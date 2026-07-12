namespace NetPdf.Layout.Elements;

/// <summary>One run of text inside a rich text block, with an optional style and hyperlink.</summary>
public sealed record TextSpan
{
    /// <summary>The span's text; may contain <c>\n</c> for forced line breaks.</summary>
    public required string Text { get; init; }

    /// <summary>Style overrides for this span; unset properties inherit from the block and ambient defaults.</summary>
    public TextStyle? Style { get; init; }

    /// <summary>When set, the span is rendered as a clickable web link.</summary>
    public string? LinkUrl { get; init; }
}

/// <summary>
/// A paragraph of text composed of spans with mixed styles. Word-wraps across span boundaries,
/// paginates by whole lines, and emits link annotations for hyperlink spans. Span styles inherit
/// from the block style, then from the ambient <c>DefaultTextStyle</c>.
/// </summary>
public sealed class RichTextElement : IElement
{
    private readonly IReadOnlyList<TextSpan> _spans;
    private readonly TextStyle? _blockStyle;

    private List<Line>? _lines;
    private double _wrapWidth = double.NaN;
    private int _lineIndex;

    /// <summary>Creates the element from spans and an optional block-level default style.</summary>
    public RichTextElement(IReadOnlyList<TextSpan> spans, TextStyle? blockStyle = null)
    {
        ArgumentNullException.ThrowIfNull(spans);
        _spans = spans;
        _blockStyle = blockStyle;
    }

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        var lines = WrapLines(canvas, availableSpace.Width);
        var remaining = lines.Count - _lineIndex;
        if (remaining <= 0)
            return SpacePlan.FullRender(Size.Zero);

        var height = 0.0;
        var width = 0.0;
        var count = 0;
        for (var i = _lineIndex; i < lines.Count; i++)
        {
            if (height + lines[i].Advance > availableSpace.Height + Epsilon)
                break;
            height += lines[i].Advance;
            width = Math.Max(width, lines[i].Width);
            count++;
        }

        if (count == 0)
            return SpacePlan.Wrap();

        var size = new Size(width, height);
        return count < remaining ? SpacePlan.PartialRender(size) : SpacePlan.FullRender(size);
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        var lines = WrapLines(canvas, availableSpace.Width);

        var y = 0.0;
        while (_lineIndex < lines.Count)
        {
            var line = lines[_lineIndex];
            if (y + line.Advance > availableSpace.Height + Epsilon)
                break;

            var x = 0.0;
            foreach (var run in line.Runs)
            {
                // Bottom-align runs so mixed sizes share an approximate baseline.
                var runY = y + (line.NaturalHeight - run.Height);
                canvas.DrawText(run.Text, run.Style, x, runY);
                if (run.LinkUrl is not null)
                    canvas.DrawLink(x, runY, run.Width, run.Height, run.LinkUrl);
                x += run.Width;
            }

            y += line.Advance;
            _lineIndex++;
        }
    }

    private const double Epsilon = 1e-6;

    private sealed record Run(string Text, TextStyle Style, string? LinkUrl, double Width, double Height);

    private sealed class Line
    {
        public List<Run> Runs { get; } = [];
        public double Width { get; set; }
        public double NaturalHeight { get; set; }
        public double Advance { get; set; }
    }

    private readonly record struct Token(string Text, TextStyle Style, string? LinkUrl, bool LeadingSpace);

    private List<Line> WrapLines(ICanvas canvas, double maxWidth)
    {
        if (_lines is not null && _wrapWidth == maxWidth)
            return _lines;

        var blockDefault = (_blockStyle ?? new TextStyle()).Merge(canvas.DefaultTextStyle);
        var lines = new List<Line>();
        var current = NewLine(canvas, blockDefault, lines);
        var pendingSpace = false;

        foreach (var span in _spans)
        {
            var style = (span.Style ?? new TextStyle()).Merge(blockDefault).Resolve();
            var paragraphs = span.Text.Split('\n');
            for (var p = 0; p < paragraphs.Length; p++)
            {
                if (p > 0)
                {
                    current = NewLine(canvas, blockDefault, lines);
                    pendingSpace = false;
                }

                var segments = paragraphs[p].Split(' ');
                for (var s = 0; s < segments.Length; s++)
                {
                    // An empty segment marks a space at the paragraph edge or a run of spaces;
                    // it turns into a leading space on the next word.
                    if (segments[s].Length == 0)
                    {
                        pendingSpace |= segments.Length > 1;
                        continue;
                    }

                    var token = new Token(segments[s], style, span.LinkUrl, s > 0 || pendingSpace);
                    current = AppendToken(canvas, blockDefault, lines, current, token, maxWidth);
                    pendingSpace = s < segments.Length - 1;
                }
            }
        }

        // Drop a trailing empty line unless the whole block is empty.
        if (lines.Count > 1 && lines[^1].Runs.Count == 0)
            lines.RemoveAt(lines.Count - 1);

        _lines = lines;
        _wrapWidth = maxWidth;
        return lines;
    }

    private Line AppendToken(ICanvas canvas, TextStyle blockDefault, List<Line> lines,
        Line current, Token token, double maxWidth)
    {
        var wordSize = canvas.MeasureText(token.Text, token.Style);
        var spaceWidth = token.LeadingSpace && current.Runs.Count > 0
            ? canvas.MeasureText(" ", token.Style).Width
            : 0;

        if (current.Runs.Count > 0 && current.Width + spaceWidth + wordSize.Width > maxWidth + Epsilon)
        {
            current = NewLine(canvas, blockDefault, lines);
            spaceWidth = 0;
        }

        var last = current.Runs.Count > 0 ? current.Runs[^1] : null;
        if (last is not null && last.Style == token.Style && last.LinkUrl == token.LinkUrl)
        {
            var text = spaceWidth > 0 ? last.Text + " " + token.Text : last.Text + token.Text;
            current.Runs[^1] = last with { Text = text, Width = last.Width + spaceWidth + wordSize.Width };
        }
        else
        {
            if (spaceWidth > 0 && last is not null)
                current.Runs[^1] = last with { Text = last.Text + " ", Width = last.Width + spaceWidth };
            current.Runs.Add(new Run(token.Text, token.Style, token.LinkUrl, wordSize.Width, wordSize.Height));
        }

        current.Width += spaceWidth + wordSize.Width;
        current.NaturalHeight = Math.Max(current.NaturalHeight, wordSize.Height);
        current.Advance = Math.Max(current.Advance, wordSize.Height * (token.Style.LineHeight ?? 1.0));
        return current;
    }

    // Empty lines still take up one line of the block's default font height.
    private static Line NewLine(ICanvas canvas, TextStyle blockDefault, List<Line> lines)
    {
        var resolved = blockDefault.Resolve();
        var height = canvas.MeasureText("Ag", resolved).Height;
        var line = new Line { NaturalHeight = height, Advance = height * (resolved.LineHeight ?? 1.0) };
        lines.Add(line);
        return line;
    }
}

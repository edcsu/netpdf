namespace NetPdf.Layout.Elements;

/// <summary>
/// A block of text that word-wraps to the available width and continues across pages
/// when it does not fit in the offered height.
/// </summary>
public sealed class TextElement : IElement
{
    private readonly string _text;
    private readonly TextStyle? _style;

    private IReadOnlyList<string>? _lines;
    private double _wrapWidth = double.NaN;
    private int _lineIndex;

    /// <summary>Creates a text element with the given content and style (defaults when omitted).</summary>
    public TextElement(string text, TextStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        _text = text;
        _style = style;
    }

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        var style = EffectiveStyle(canvas);
        var lines = WrapLines(canvas, style, availableSpace.Width);
        var remaining = lines.Count - _lineIndex;
        if (remaining <= 0)
            return SpacePlan.FullRender(Size.Zero);

        var lineHeight = LineHeight(canvas, style);
        var fitting = (int)Math.Floor(availableSpace.Height / lineHeight);
        if (fitting <= 0)
            return SpacePlan.Wrap();

        var count = Math.Min(fitting, remaining);
        var width = 0.0;
        for (var i = _lineIndex; i < _lineIndex + count; i++)
            width = Math.Max(width, canvas.MeasureText(lines[i], style).Width);

        var size = new Size(width, count * lineHeight);
        return count < remaining ? SpacePlan.PartialRender(size) : SpacePlan.FullRender(size);
    }

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        var style = EffectiveStyle(canvas);
        var lines = WrapLines(canvas, style, availableSpace.Width);
        var lineHeight = LineHeight(canvas, style);
        var fitting = (int)Math.Floor(availableSpace.Height / lineHeight);
        var count = Math.Min(fitting, lines.Count - _lineIndex);

        var rtl = canvas.Direction == ContentDirection.RightToLeft;
        var y = 0.0;
        for (var i = 0; i < count; i++)
        {
            var line = Text.BidiAlgorithm.ReorderVisual(lines[_lineIndex + i], canvas.Direction);
            // RTL paragraphs align to the right edge of the offered space.
            var x = rtl ? availableSpace.Width - canvas.MeasureText(line, style).Width : 0;
            canvas.DrawText(line, style, x, y);
            y += lineHeight;
        }

        _lineIndex += Math.Max(count, 0);
    }

    private TextStyle EffectiveStyle(ICanvas canvas) =>
        (_style ?? new TextStyle()).Merge(canvas.DefaultTextStyle).Resolve();

    private static double LineHeight(ICanvas canvas, TextStyle style) =>
        canvas.MeasureText("Ag", style).Height * (style.LineHeight ?? 1.0);

    // Greedy word-wrap; recomputed only when the offered width changes.
    private IReadOnlyList<string> WrapLines(ICanvas canvas, TextStyle style, double maxWidth)
    {
        if (_lines is not null && _wrapWidth == maxWidth)
            return _lines;

        var lines = new List<string>();
        // Contextual Arabic shaping happens before wrapping so measurements use the
        // presentation-form glyphs that are actually drawn.
        foreach (var paragraph in Text.ArabicShaper.Shape(_text).Split('\n'))
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            var line = "";
            foreach (var word in words)
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && canvas.MeasureText(candidate, style).Width > maxWidth)
                {
                    lines.Add(line);
                    line = word;
                }
                else
                {
                    line = candidate;
                }
            }
            lines.Add(line);
        }

        _lines = lines;
        _wrapWidth = maxWidth;
        return lines;
    }
}

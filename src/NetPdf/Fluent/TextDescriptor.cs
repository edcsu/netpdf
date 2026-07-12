using NetPdf.Layout;
using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>
/// Builds a rich text block from spans with individual styles. Block-level settings
/// (<see cref="DefaultStyle"/>, <see cref="LineHeight"/>, <see cref="LetterSpacing"/>)
/// apply to every span that does not override them.
/// </summary>
public sealed class TextDescriptor
{
    private readonly List<MutableSpan> _spans = [];
    private TextStyle _blockStyle = new();

    internal TextDescriptor()
    {
    }

    /// <summary>Sets the default style for all spans in this block.</summary>
    public TextDescriptor DefaultStyle(TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        _blockStyle = style.Merge(_blockStyle);
        return this;
    }

    /// <summary>Sets the block's line height as a multiplier of the natural font line height.</summary>
    public TextDescriptor LineHeight(double multiplier)
    {
        _blockStyle = _blockStyle with { LineHeight = multiplier };
        return this;
    }

    /// <summary>Sets extra spacing between characters in points for the whole block.</summary>
    public TextDescriptor LetterSpacing(double spacing)
    {
        _blockStyle = _blockStyle with { LetterSpacing = spacing };
        return this;
    }

    /// <summary>Appends a span of text; chain calls on the returned descriptor to style it.</summary>
    public TextSpanDescriptor Span(string text, TextStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        var span = new MutableSpan { Text = text, Style = style };
        _spans.Add(span);
        return new TextSpanDescriptor(span);
    }

    /// <summary>Appends a span followed by a line break.</summary>
    public TextSpanDescriptor Line(string text, TextStyle? style = null) => Span(text + "\n", style);

    /// <summary>Appends an empty line.</summary>
    public void EmptyLine() => Span("\n");

    /// <summary>
    /// Appends a clickable hyperlink span. Renders underlined in blue unless the style
    /// overrides those properties.
    /// </summary>
    public TextSpanDescriptor Hyperlink(string text, string url, TextStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(url);
        var linkDefaults = new TextStyle { Underline = true, Color = System.Drawing.Color.Blue };
        var span = new MutableSpan
        {
            Text = text,
            Style = style is null ? linkDefaults : style.Merge(linkDefaults),
            LinkUrl = url,
        };
        _spans.Add(span);
        return new TextSpanDescriptor(span);
    }

    internal RichTextElement Build() => new(
        _spans.ConvertAll(s => new TextSpan { Text = s.Text, Style = s.Style, LinkUrl = s.LinkUrl }),
        _blockStyle);

    internal sealed class MutableSpan
    {
        public required string Text { get; init; }
        public TextStyle? Style { get; set; }
        public string? LinkUrl { get; init; }
    }
}

/// <summary>Chainable style tweaks for one span created by a <see cref="TextDescriptor"/>.</summary>
public sealed class TextSpanDescriptor
{
    private readonly TextDescriptor.MutableSpan _span;

    internal TextSpanDescriptor(TextDescriptor.MutableSpan span) => _span = span;

    private TextSpanDescriptor Apply(TextStyle overrides)
    {
        _span.Style = _span.Style is null ? overrides : overrides.Merge(_span.Style);
        return this;
    }

    /// <summary>Replaces unset span style properties with those of the given style.</summary>
    public TextSpanDescriptor Style(TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        _span.Style = _span.Style is null ? style : _span.Style.Merge(style);
        return this;
    }

    /// <summary>Uses a bold face for this span.</summary>
    public TextSpanDescriptor Bold() => Apply(new TextStyle { Bold = true });

    /// <summary>Uses an italic face for this span.</summary>
    public TextSpanDescriptor Italic() => Apply(new TextStyle { Italic = true });

    /// <summary>Underlines this span.</summary>
    public TextSpanDescriptor Underline() => Apply(new TextStyle { Underline = true });

    /// <summary>Sets the font size in points for this span.</summary>
    public TextSpanDescriptor FontSize(double size) => Apply(new TextStyle { FontSize = size });

    /// <summary>Sets the font family for this span.</summary>
    public TextSpanDescriptor FontFamily(string family)
    {
        ArgumentException.ThrowIfNullOrEmpty(family);
        return Apply(new TextStyle { FontFamily = family });
    }

    /// <summary>Sets the text color for this span.</summary>
    public TextSpanDescriptor Color(System.Drawing.Color color) => Apply(new TextStyle { Color = color });
}

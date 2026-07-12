namespace NetPdf.Layout;

/// <summary>
/// Font, color and paragraph settings for text drawn through an <see cref="ICanvas"/>.
/// Unset (null) properties inherit from an enclosing default style (see
/// <c>DefaultTextStyle</c>) and finally fall back to the library defaults
/// (Arial 12 pt, black, regular, line height 1.0, no letter spacing).
/// </summary>
public sealed record TextStyle
{
    /// <summary>The font family (must be resolvable on the system). Defaults to Arial.</summary>
    public string? FontFamily { get; init; }

    /// <summary>The font size in points. Defaults to 12.</summary>
    public double? FontSize { get; init; }

    /// <summary>Whether a bold face is used.</summary>
    public bool? Bold { get; init; }

    /// <summary>Whether an italic face is used.</summary>
    public bool? Italic { get; init; }

    /// <summary>Whether the text is underlined.</summary>
    public bool? Underline { get; init; }

    /// <summary>The text color. Defaults to black.</summary>
    public System.Drawing.Color? Color { get; init; }

    /// <summary>Line height as a multiplier of the font's natural line height. Defaults to 1.0.</summary>
    public double? LineHeight { get; init; }

    /// <summary>Extra spacing between characters in points. Defaults to 0.</summary>
    public double? LetterSpacing { get; init; }

    /// <summary>Returns a style where unset properties are taken from <paramref name="fallback"/>.</summary>
    public TextStyle Merge(TextStyle fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return new TextStyle
        {
            FontFamily = FontFamily ?? fallback.FontFamily,
            FontSize = FontSize ?? fallback.FontSize,
            Bold = Bold ?? fallback.Bold,
            Italic = Italic ?? fallback.Italic,
            Underline = Underline ?? fallback.Underline,
            Color = Color ?? fallback.Color,
            LineHeight = LineHeight ?? fallback.LineHeight,
            LetterSpacing = LetterSpacing ?? fallback.LetterSpacing,
        };
    }

    /// <summary>Returns a style with every unset property replaced by its library default.</summary>
    public TextStyle Resolve() => new()
    {
        FontFamily = FontFamily ?? "Arial",
        FontSize = FontSize ?? 12,
        Bold = Bold ?? false,
        Italic = Italic ?? false,
        Underline = Underline ?? false,
        Color = Color ?? System.Drawing.Color.Black,
        LineHeight = LineHeight ?? 1.0,
        LetterSpacing = LetterSpacing ?? 0,
    };
}

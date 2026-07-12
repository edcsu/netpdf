namespace NetPdf.Layout;

/// <summary>Font and color settings for text drawn through an <see cref="ICanvas"/>.</summary>
public sealed record TextStyle
{
    /// <summary>The font family (must be resolvable on the system). Defaults to Arial.</summary>
    public string FontFamily { get; init; } = "Arial";

    /// <summary>The font size in points. Defaults to 12.</summary>
    public double FontSize { get; init; } = 12;

    /// <summary>Whether a bold face is used.</summary>
    public bool Bold { get; init; }

    /// <summary>Whether an italic face is used.</summary>
    public bool Italic { get; init; }

    /// <summary>The text color. Defaults to black.</summary>
    public System.Drawing.Color Color { get; init; } = System.Drawing.Color.Black;
}

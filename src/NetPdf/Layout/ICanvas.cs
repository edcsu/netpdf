namespace NetPdf.Layout;

/// <summary>
/// Drawing surface handed to elements during measurement and drawing. Coordinates are
/// PDF points relative to the current origin (top-left of the space offered to the element).
/// </summary>
public interface ICanvas
{
    /// <summary>Page numbering state for the page being rendered.</summary>
    PageContext PageContext { get; }

    /// <summary>Draws a single line of text with its top-left corner at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    void DrawText(string text, TextStyle style, double x, double y);

    /// <summary>Measures a single line of text; the height is the line height of the style's font.</summary>
    Size MeasureText(string text, TextStyle style);

    /// <summary>Moves the origin by the given offset.</summary>
    void Translate(double dx, double dy);

    /// <summary>Saves the current transformation state.</summary>
    void Save();

    /// <summary>Restores the most recently saved transformation state.</summary>
    void Restore();
}

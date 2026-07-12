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

    /// <summary>Returns the intrinsic size of the image in points.</summary>
    Size MeasureImage(ImageSource image);

    /// <summary>Draws the image scaled into the given rectangle.</summary>
    void DrawImage(ImageSource image, double x, double y, double width, double height);

    /// <summary>Draws a straight line between two points.</summary>
    void DrawLine(double x1, double y1, double x2, double y2, System.Drawing.Color color, double thickness);

    /// <summary>
    /// Draws a rectangle. Set <paramref name="fill"/> to fill it and/or <paramref name="stroke"/>
    /// to outline it; a <paramref name="cornerRadius"/> greater than zero rounds the corners.
    /// </summary>
    void DrawRectangle(double x, double y, double width, double height,
        System.Drawing.Color? fill, System.Drawing.Color? stroke = null,
        double strokeThickness = 1, double cornerRadius = 0);

    /// <summary>Moves the origin by the given offset.</summary>
    void Translate(double dx, double dy);

    /// <summary>Saves the current transformation state.</summary>
    void Save();

    /// <summary>Restores the most recently saved transformation state.</summary>
    void Restore();
}

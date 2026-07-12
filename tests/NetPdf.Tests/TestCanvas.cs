using NetPdf.Layout;

namespace NetPdf.Tests;

/// <summary>
/// Deterministic canvas for layout unit tests: every character is 5 pt wide, every line 10 pt tall.
/// Records draw calls with their absolute positions so tests can assert where content lands.
/// </summary>
internal sealed class TestCanvas : ICanvas
{
    private double _originX;
    private double _originY;
    private readonly Stack<(double X, double Y)> _saved = new();

    public PageContext PageContext { get; } = new();

    /// <summary>Text drawn so far, with positions in absolute coordinates.</summary>
    public List<(string Text, double X, double Y)> DrawnText { get; } = [];

    public void DrawText(string text, TextStyle style, double x, double y) =>
        DrawnText.Add((text, _originX + x, _originY + y));

    public Size MeasureText(string text, TextStyle style) => new(text.Length * 5, 10);

    /// <summary>Images drawn so far, with positions in absolute coordinates.</summary>
    public List<(ImageSource Source, double X, double Y, double Width, double Height)> DrawnImages { get; } = [];

    /// <summary>Lines drawn so far, with endpoints in absolute coordinates.</summary>
    public List<(double X1, double Y1, double X2, double Y2, System.Drawing.Color Color, double Thickness)> DrawnLines { get; } = [];

    /// <summary>Rectangles drawn so far, with positions in absolute coordinates.</summary>
    public List<(double X, double Y, double Width, double Height, System.Drawing.Color? Fill,
        System.Drawing.Color? Stroke, double StrokeThickness, double CornerRadius)> DrawnRectangles { get; } = [];

    /// <summary>Intrinsic size reported for every image.</summary>
    public Size FakeImageSize { get; set; } = new(100, 50);

    public Size MeasureImage(ImageSource image) => FakeImageSize;

    public void DrawImage(ImageSource image, double x, double y, double width, double height) =>
        DrawnImages.Add((image, _originX + x, _originY + y, width, height));

    public void DrawLine(double x1, double y1, double x2, double y2, System.Drawing.Color color, double thickness) =>
        DrawnLines.Add((_originX + x1, _originY + y1, _originX + x2, _originY + y2, color, thickness));

    public void DrawRectangle(double x, double y, double width, double height,
        System.Drawing.Color? fill, System.Drawing.Color? stroke = null,
        double strokeThickness = 1, double cornerRadius = 0) =>
        DrawnRectangles.Add((_originX + x, _originY + y, width, height, fill, stroke, strokeThickness, cornerRadius));

    public void Translate(double dx, double dy)
    {
        _originX += dx;
        _originY += dy;
    }

    public void Save() => _saved.Push((_originX, _originY));

    public void Restore() => (_originX, _originY) = _saved.Pop();
}

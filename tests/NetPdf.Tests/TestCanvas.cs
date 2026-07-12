using NetPdf.Layout;

namespace NetPdf.Tests;

/// <summary>
/// Deterministic canvas for layout unit tests: every character is 5 pt wide, every line 10 pt tall.
/// Records draw calls with their absolute positions so tests can assert where content lands.
/// Tracks a full affine transform so rotation/scaling can be asserted; with only translation
/// applied, recorded coordinates match the pre-matrix behavior exactly.
/// </summary>
internal sealed class TestCanvas : ICanvas
{
    // Row-major 2x3 affine matrix mapping local to absolute coordinates:
    // xAbs = M11*x + M21*y + Dx;  yAbs = M12*x + M22*y + Dy.
    internal readonly struct Matrix(double m11, double m12, double m21, double m22, double dx, double dy)
    {
        public double M11 { get; } = m11;
        public double M12 { get; } = m12;
        public double M21 { get; } = m21;
        public double M22 { get; } = m22;
        public double Dx { get; } = dx;
        public double Dy { get; } = dy;

        public static Matrix Identity => new(1, 0, 0, 1, 0, 0);

        public (double X, double Y) Apply(double x, double y) =>
            (M11 * x + M21 * y + Dx, M12 * x + M22 * y + Dy);

        // Prepend semantics (matches XGraphics.*Transform defaults): the new transform
        // applies in local space before the existing one.
        public Matrix Prepend(Matrix o) => new(
            o.M11 * M11 + o.M12 * M21,
            o.M11 * M12 + o.M12 * M22,
            o.M21 * M11 + o.M22 * M21,
            o.M21 * M12 + o.M22 * M22,
            o.Dx * M11 + o.Dy * M21 + Dx,
            o.Dx * M12 + o.Dy * M22 + Dy);
    }

    private Matrix _matrix = Matrix.Identity;
    private readonly Stack<Matrix> _saved = new();
    private readonly Stack<TextStyle> _defaultStyles = new([new TextStyle()]);

    public PageContext PageContext { get; } = new();

    public TextStyle DefaultTextStyle => _defaultStyles.Peek();

    /// <summary>The current local-to-absolute transform, for asserting rotation/scale state.</summary>
    internal Matrix CurrentTransform => _matrix;

    public void PushDefaultTextStyle(TextStyle style) => _defaultStyles.Push(style.Merge(DefaultTextStyle));

    public void PopDefaultTextStyle() => _defaultStyles.Pop();

    private readonly Stack<ContentDirection> _directions = new([ContentDirection.LeftToRight]);

    public ContentDirection Direction => _directions.Peek();

    public void PushDirection(ContentDirection direction) => _directions.Push(direction);

    public void PopDirection() => _directions.Pop();

    /// <summary>Link annotations added so far, with rectangles in absolute coordinates.</summary>
    public List<(double X, double Y, double Width, double Height, string Url)> Links { get; } = [];

    public void DrawLink(double x, double y, double width, double height, string url)
    {
        var (ax, ay) = _matrix.Apply(x, y);
        Links.Add((ax, ay, width, height, url));
    }

    /// <summary>Text drawn so far, with positions in absolute coordinates.</summary>
    public List<(string Text, double X, double Y)> DrawnText { get; } = [];

    public void DrawText(string text, TextStyle style, double x, double y)
    {
        var (ax, ay) = _matrix.Apply(x, y);
        DrawnText.Add((text, ax, ay));
    }

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

    public void DrawImage(ImageSource image, double x, double y, double width, double height)
    {
        var (ax, ay) = _matrix.Apply(x, y);
        DrawnImages.Add((image, ax, ay, width, height));
    }

    public void DrawLine(double x1, double y1, double x2, double y2, System.Drawing.Color color, double thickness)
    {
        var (ax1, ay1) = _matrix.Apply(x1, y1);
        var (ax2, ay2) = _matrix.Apply(x2, y2);
        DrawnLines.Add((ax1, ay1, ax2, ay2, color, thickness));
    }

    public void DrawRectangle(double x, double y, double width, double height,
        System.Drawing.Color? fill, System.Drawing.Color? stroke = null,
        double strokeThickness = 1, double cornerRadius = 0)
    {
        var (ax, ay) = _matrix.Apply(x, y);
        DrawnRectangles.Add((ax, ay, width, height, fill, stroke, strokeThickness, cornerRadius));
    }

    public void Translate(double dx, double dy) =>
        _matrix = _matrix.Prepend(new Matrix(1, 0, 0, 1, dx, dy));

    public void Rotate(double degrees)
    {
        var r = degrees * Math.PI / 180;
        var (cos, sin) = (Math.Cos(r), Math.Sin(r));
        _matrix = _matrix.Prepend(new Matrix(cos, sin, -sin, cos, 0, 0));
    }

    public void Scale(double scaleX, double scaleY) =>
        _matrix = _matrix.Prepend(new Matrix(scaleX, 0, 0, scaleY, 0, 0));

    public void Save() => _saved.Push(_matrix);

    public void Restore() => _matrix = _saved.Pop();
}

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

    /// <summary>Text drawn so far, with positions in absolute coordinates.</summary>
    public List<(string Text, double X, double Y)> DrawnText { get; } = [];

    public void DrawText(string text, TextStyle style, double x, double y) =>
        DrawnText.Add((text, _originX + x, _originY + y));

    public Size MeasureText(string text, TextStyle style) => new(text.Length * 5, 10);

    public void Translate(double dx, double dy)
    {
        _originX += dx;
        _originY += dy;
    }

    public void Save() => _saved.Push((_originX, _originY));

    public void Restore() => (_originX, _originY) = _saved.Pop();
}

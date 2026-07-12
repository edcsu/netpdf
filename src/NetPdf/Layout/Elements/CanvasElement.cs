namespace NetPdf.Layout.Elements;

/// <summary>
/// Hands the raw <see cref="ICanvas"/> to a callback for custom drawing (charts, diagrams, …).
/// The element takes the full offered space; coordinates passed to the canvas are relative to
/// the slot's top-left corner.
/// </summary>
public sealed class CanvasElement : IElement
{
    private readonly Action<ICanvas, Size> _draw;

    /// <summary>Creates the element with the drawing callback.</summary>
    public CanvasElement(Action<ICanvas, Size> draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        _draw = draw;
    }

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace) => SpacePlan.FullRender(availableSpace);

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        canvas.Save();
        _draw(canvas, availableSpace);
        canvas.Restore();
    }
}

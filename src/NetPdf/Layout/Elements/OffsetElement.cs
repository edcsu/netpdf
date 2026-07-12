namespace NetPdf.Layout.Elements;

/// <summary>Shifts the child's drawing position by a fixed offset without affecting layout.</summary>
public sealed class OffsetElement : ContainerElement
{
    /// <summary>The horizontal shift in points (may be negative).</summary>
    public double OffsetX { get; init; }

    /// <summary>The vertical shift in points (may be negative).</summary>
    public double OffsetY { get; init; }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        canvas.Translate(OffsetX, OffsetY);
        Child.Draw(canvas, availableSpace);
        canvas.Translate(-OffsetX, -OffsetY);
    }
}

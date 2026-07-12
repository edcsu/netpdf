namespace NetPdf.Layout.Elements;

/// <summary>
/// Rotates the child's drawing by an arbitrary angle around the slot's top-left corner.
/// The rotation is visual only: it does not change the measured size, so rotated content
/// may extend outside the slot. Combine with <see cref="OffsetElement"/> to reposition.
/// </summary>
public sealed class RotateElement : ContainerElement
{
    /// <summary>Clockwise rotation angle in degrees.</summary>
    public double Degrees { get; set; }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        canvas.Save();
        canvas.Rotate(Degrees);
        Child.Draw(canvas, availableSpace);
        canvas.Restore();
    }
}

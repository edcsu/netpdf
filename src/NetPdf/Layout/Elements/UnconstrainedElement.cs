namespace NetPdf.Layout.Elements;

/// <summary>
/// Measures and draws the child with effectively unlimited space while occupying no space
/// itself, letting content overflow its slot in the parent layout.
/// </summary>
public sealed class UnconstrainedElement : ContainerElement
{
    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        Child.Measure(canvas, Size.Max);
        return SpacePlan.FullRender(Size.Zero);
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace) => Child.Draw(canvas, Size.Max);
}

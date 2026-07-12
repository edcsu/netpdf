namespace NetPdf.Layout.Elements;

/// <summary>
/// Renders its child once; after the child has fully rendered, the element occupies no space
/// on subsequent pages. Useful inside repeated slots such as decorations.
/// </summary>
public sealed class ShowOnceElement : ContainerElement
{
    private bool _done;

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace) =>
        _done ? SpacePlan.FullRender(Size.Zero) : Child.Measure(canvas, availableSpace);

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        if (_done)
            return;

        var plan = Child.Measure(canvas, availableSpace);
        Child.Draw(canvas, availableSpace);
        if (plan.Type == SpacePlanType.FullRender)
            _done = true;
    }
}

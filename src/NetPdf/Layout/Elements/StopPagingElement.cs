namespace NetPdf.Layout.Elements;

/// <summary>
/// Renders only the part of its child that fits on the current page and discards the rest,
/// so the child never continues onto another page.
/// </summary>
public sealed class StopPagingElement : ContainerElement
{
    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        var plan = Child.Measure(canvas, availableSpace);
        return plan.Type switch
        {
            SpacePlanType.PartialRender => SpacePlan.FullRender(plan.Size),
            SpacePlanType.Wrap => SpacePlan.FullRender(Size.Zero),
            _ => plan,
        };
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        // Nothing fits at all: draw nothing rather than overflow.
        if (Child.Measure(canvas, availableSpace).Type == SpacePlanType.Wrap)
            return;
        Child.Draw(canvas, availableSpace);
    }
}

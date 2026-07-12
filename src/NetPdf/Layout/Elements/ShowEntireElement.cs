namespace NetPdf.Layout.Elements;

/// <summary>
/// Prevents its child from being split across pages: when the child would only partially fit,
/// the whole block is deferred to the next page instead. Content taller than a full page
/// cannot be rendered and fails with a layout error.
/// </summary>
public sealed class ShowEntireElement : ContainerElement
{
    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        var plan = Child.Measure(canvas, availableSpace);
        return plan.Type == SpacePlanType.PartialRender ? SpacePlan.Wrap() : plan;
    }
}

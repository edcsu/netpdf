namespace NetPdf.Layout.Elements;

/// <summary>
/// Starts its child on the next page unless at least <see cref="MinHeight"/> points of height
/// are available; with enough space the child may still split across pages as usual.
/// </summary>
public sealed class EnsureSpaceElement : ContainerElement
{
    /// <summary>The minimum height in points required to start rendering on this page.</summary>
    public double MinHeight { get; init; } = 150;

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        var plan = Child.Measure(canvas, availableSpace);
        if (plan.Type == SpacePlanType.PartialRender && availableSpace.Height < MinHeight)
            return SpacePlan.Wrap();
        return plan;
    }
}

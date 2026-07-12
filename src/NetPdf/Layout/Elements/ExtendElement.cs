namespace NetPdf.Layout.Elements;

/// <summary>Reports the full offered space on the extended axes regardless of the child's natural size.</summary>
public sealed class ExtendElement : ContainerElement
{
    /// <summary>Whether the element takes the full offered width. Defaults to true.</summary>
    public bool ExtendHorizontal { get; init; } = true;

    /// <summary>Whether the element takes the full offered height. Defaults to true.</summary>
    public bool ExtendVertical { get; init; } = true;

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        var plan = Child.Measure(canvas, availableSpace);
        if (plan.Type == SpacePlanType.Wrap)
            return SpacePlan.Wrap();

        var size = new Size(
            ExtendHorizontal ? availableSpace.Width : plan.Size.Width,
            ExtendVertical ? availableSpace.Height : plan.Size.Height);
        return plan.Type == SpacePlanType.PartialRender
            ? SpacePlan.PartialRender(size)
            : SpacePlan.FullRender(size);
    }
}

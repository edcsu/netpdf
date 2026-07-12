namespace NetPdf.Layout.Elements;

/// <summary>
/// Applies minimum and maximum width/height constraints to the child. An exact dimension is
/// expressed by setting the minimum and maximum to the same value.
/// </summary>
public sealed class ConstrainedElement : ContainerElement
{
    /// <summary>The minimum width in points, or null for no constraint.</summary>
    public double? MinWidth { get; init; }

    /// <summary>The maximum width in points, or null for no constraint.</summary>
    public double? MaxWidth { get; init; }

    /// <summary>The minimum height in points, or null for no constraint.</summary>
    public double? MinHeight { get; init; }

    /// <summary>The maximum height in points, or null for no constraint.</summary>
    public double? MaxHeight { get; init; }

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        if (availableSpace.Width < MinWidth || availableSpace.Height < MinHeight)
            return SpacePlan.Wrap();

        var plan = Child.Measure(canvas, Constrain(availableSpace));
        if (plan.Type == SpacePlanType.Wrap)
            return SpacePlan.Wrap();

        var size = new Size(
            Math.Max(plan.Size.Width, MinWidth ?? 0),
            Math.Max(plan.Size.Height, MinHeight ?? 0));
        return plan.Type == SpacePlanType.PartialRender
            ? SpacePlan.PartialRender(size)
            : SpacePlan.FullRender(size);
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace) =>
        Child.Draw(canvas, Constrain(availableSpace));

    private Size Constrain(Size available) => new(
        Math.Min(available.Width, MaxWidth ?? available.Width),
        Math.Min(available.Height, MaxHeight ?? available.Height));
}

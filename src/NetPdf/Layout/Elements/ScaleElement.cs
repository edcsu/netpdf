namespace NetPdf.Layout.Elements;

/// <summary>
/// Scales the child's content. The child is measured with proportionally more (or less)
/// space and the reported size is scaled back, so scaling affects layout. Negative factors
/// mirror the content within the slot (flip).
/// </summary>
public sealed class ScaleElement : ContainerElement
{
    /// <summary>Horizontal scale factor; negative mirrors horizontally. Must not be zero.</summary>
    public double ScaleX { get; set; } = 1;

    /// <summary>Vertical scale factor; negative mirrors vertically. Must not be zero.</summary>
    public double ScaleY { get; set; } = 1;

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        ValidateFactors();
        var plan = Child.Measure(canvas, ChildSpace(availableSpace));
        return plan.Type switch
        {
            SpacePlanType.Wrap => SpacePlan.Wrap(),
            SpacePlanType.PartialRender => SpacePlan.PartialRender(ScaleSize(plan.Size)),
            _ => SpacePlan.FullRender(ScaleSize(plan.Size)),
        };
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ValidateFactors();
        canvas.Save();
        canvas.Translate(ScaleX < 0 ? availableSpace.Width : 0, ScaleY < 0 ? availableSpace.Height : 0);
        canvas.Scale(ScaleX, ScaleY);
        Child.Draw(canvas, ChildSpace(availableSpace));
        canvas.Restore();
    }

    private void ValidateFactors()
    {
        if (ScaleX == 0 || ScaleY == 0)
            throw new InvalidOperationException("Scale factors must not be zero.");
    }

    private Size ChildSpace(Size space) =>
        new(space.Width / Math.Abs(ScaleX), space.Height / Math.Abs(ScaleY));

    private Size ScaleSize(Size size) =>
        new(size.Width * Math.Abs(ScaleX), size.Height * Math.Abs(ScaleY));
}

namespace NetPdf.Layout.Elements;

/// <summary>How an <see cref="AspectRatioElement"/> fills the offered space.</summary>
public enum AspectRatioOption
{
    /// <summary>Use the largest size with the given ratio that fits the offered space.</summary>
    FitArea,
    /// <summary>Take the full offered width; the height follows from the ratio.</summary>
    FitWidth,
    /// <summary>Take the full offered height; the width follows from the ratio.</summary>
    FitHeight,
}

/// <summary>
/// Sizes the child to a fixed width-to-height ratio. The element always occupies the computed
/// size in full, wrapping to the next page if the offered space cannot hold it.
/// </summary>
public sealed class AspectRatioElement : ContainerElement
{
    private readonly double _ratio;

    /// <summary>Creates the element with the given width-to-height ratio (must be positive).</summary>
    public AspectRatioElement(double ratio)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ratio);
        _ratio = ratio;
    }

    /// <summary>How the computed size fills the offered space. Defaults to <see cref="AspectRatioOption.FitArea"/>.</summary>
    public AspectRatioOption Option { get; init; } = AspectRatioOption.FitArea;

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        var target = TargetSize(availableSpace);
        if (target.Width > availableSpace.Width || target.Height > availableSpace.Height)
            return SpacePlan.Wrap();

        var plan = Child.Measure(canvas, target);
        return plan.Type == SpacePlanType.Wrap ? SpacePlan.Wrap() : SpacePlan.FullRender(target);
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace) =>
        Child.Draw(canvas, TargetSize(availableSpace));

    private Size TargetSize(Size available)
    {
        var fromWidth = new Size(available.Width, available.Width / _ratio);
        var fromHeight = new Size(available.Height * _ratio, available.Height);
        return Option switch
        {
            AspectRatioOption.FitWidth => fromWidth,
            AspectRatioOption.FitHeight => fromHeight,
            _ => fromWidth.Height <= available.Height ? fromWidth : fromHeight,
        };
    }
}

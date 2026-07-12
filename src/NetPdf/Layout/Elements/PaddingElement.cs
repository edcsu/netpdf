namespace NetPdf.Layout.Elements;

/// <summary>Insets the child by the given padding on each side.</summary>
public sealed class PaddingElement : ContainerElement
{
    /// <summary>Padding on the left edge in points.</summary>
    public double Left { get; init; }

    /// <summary>Padding on the top edge in points.</summary>
    public double Top { get; init; }

    /// <summary>Padding on the right edge in points.</summary>
    public double Right { get; init; }

    /// <summary>Padding on the bottom edge in points.</summary>
    public double Bottom { get; init; }

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        var innerWidth = availableSpace.Width - Left - Right;
        var innerHeight = availableSpace.Height - Top - Bottom;
        if (innerWidth <= 0 || innerHeight <= 0)
            return SpacePlan.Wrap();

        var plan = Child.Measure(canvas, new Size(innerWidth, innerHeight));
        if (plan.Type == SpacePlanType.Wrap)
            return SpacePlan.Wrap();

        var size = new Size(plan.Size.Width + Left + Right, plan.Size.Height + Top + Bottom);
        return plan.Type == SpacePlanType.PartialRender
            ? SpacePlan.PartialRender(size)
            : SpacePlan.FullRender(size);
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        var inner = new Size(availableSpace.Width - Left - Right, availableSpace.Height - Top - Bottom);
        canvas.Translate(Left, Top);
        Child.Draw(canvas, inner);
        canvas.Translate(-Left, -Top);
    }
}

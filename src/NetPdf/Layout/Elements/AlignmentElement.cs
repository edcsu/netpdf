namespace NetPdf.Layout.Elements;

/// <summary>
/// Positions the child within the offered space. Vertical alignment is applied only when the
/// child fully renders; partially rendered content stays top-aligned so it flows naturally
/// across pages.
/// </summary>
public sealed class AlignmentElement : ContainerElement
{
    /// <summary>The horizontal placement of the child. Defaults to left.</summary>
    public HorizontalAlignment Horizontal { get; init; } = HorizontalAlignment.Left;

    /// <summary>The vertical placement of the child. Defaults to top.</summary>
    public VerticalAlignment Vertical { get; init; } = VerticalAlignment.Top;

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        var plan = Child.Measure(canvas, availableSpace);
        if (plan.Type == SpacePlanType.Wrap)
            return;

        var dx = (availableSpace.Width - plan.Size.Width) * HorizontalFraction();
        var dy = plan.Type == SpacePlanType.FullRender
            ? (availableSpace.Height - plan.Size.Height) * VerticalFraction()
            : 0;

        canvas.Translate(dx, dy);
        Child.Draw(canvas, availableSpace);
        canvas.Translate(-dx, -dy);
    }

    private double HorizontalFraction() => Horizontal switch
    {
        HorizontalAlignment.Center => 0.5,
        HorizontalAlignment.Right => 1,
        _ => 0,
    };

    private double VerticalFraction() => Vertical switch
    {
        VerticalAlignment.Middle => 0.5,
        VerticalAlignment.Bottom => 1,
        _ => 0,
    };
}

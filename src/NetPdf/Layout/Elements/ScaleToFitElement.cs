namespace NetPdf.Layout.Elements;

/// <summary>
/// Shrinks the child uniformly until it fully fits in the offered space, using a binary
/// search over the scale factor. Never paginates: the content always renders entirely on
/// the current page at the found scale.
/// </summary>
public sealed class ScaleToFitElement : ContainerElement
{
    private const int SearchIterations = 16;
    private const double Epsilon = 1e-6;

    private double _scale = 1;

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        _scale = FindScale(canvas, availableSpace);
        var plan = Child.Measure(canvas, ChildSpace(availableSpace, _scale));
        if (plan.Type == SpacePlanType.Wrap)
            return SpacePlan.Wrap();
        return SpacePlan.FullRender(new Size(plan.Size.Width * _scale, plan.Size.Height * _scale));
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        canvas.Save();
        canvas.Scale(_scale, _scale);
        Child.Draw(canvas, ChildSpace(availableSpace, _scale));
        canvas.Restore();
    }

    private double FindScale(ICanvas canvas, Size space)
    {
        if (Fits(canvas, space, 1))
            return 1;

        double low = 0, high = 1;
        for (var i = 0; i < SearchIterations; i++)
        {
            var mid = (low + high) / 2;
            if (Fits(canvas, space, mid))
                low = mid;
            else
                high = mid;
        }

        return low;
    }

    private bool Fits(ICanvas canvas, Size space, double scale)
    {
        if (scale <= 0)
            return false;
        var childSpace = ChildSpace(space, scale);
        var plan = Child.Measure(canvas, childSpace);
        // FullRender alone is not enough: unbreakable content (e.g. a single long word)
        // reports FullRender while overflowing the offered width.
        return plan.Type == SpacePlanType.FullRender
            && plan.Size.Width <= childSpace.Width + Epsilon
            && plan.Size.Height <= childSpace.Height + Epsilon;
    }

    private static Size ChildSpace(Size space, double scale) =>
        scale <= 0 ? space : new Size(space.Width / scale, space.Height / scale);
}

namespace NetPdf.Layout.Elements;

/// <summary>
/// Occupies no space the first time it is reached (typically the first page it appears on)
/// and renders its child normally afterwards.
/// </summary>
public sealed class SkipOnceElement : ContainerElement
{
    private bool _skipped;

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace) =>
        _skipped ? Child.Measure(canvas, availableSpace) : SpacePlan.FullRender(Size.Zero);

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        if (_skipped)
        {
            Child.Draw(canvas, availableSpace);
            return;
        }

        _skipped = true;
    }
}

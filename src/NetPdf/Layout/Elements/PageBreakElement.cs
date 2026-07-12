namespace NetPdf.Layout.Elements;

/// <summary>
/// Forces following content to start on the next page by consuming the remaining height
/// the first time it is rendered; it occupies no space afterwards.
/// </summary>
public sealed class PageBreakElement : IElement
{
    private bool _broken;

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace) =>
        _broken
            ? SpacePlan.FullRender(Size.Zero)
            : SpacePlan.PartialRender(new Size(0, availableSpace.Height));

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace) => _broken = true;
}

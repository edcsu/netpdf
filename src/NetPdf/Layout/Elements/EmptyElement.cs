namespace NetPdf.Layout.Elements;

/// <summary>An element that occupies no space and draws nothing.</summary>
public sealed class EmptyElement : IElement
{
    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace) => SpacePlan.FullRender(Size.Zero);

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
    }
}

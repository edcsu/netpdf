namespace NetPdf.Layout.Elements;

/// <summary>
/// Applies a content direction to its subtree: text defaults to right alignment and
/// reorders bidirectionally, and rows/inlined items mirror when the direction is
/// right-to-left.
/// </summary>
public sealed class ContentDirectionElement : ContainerElement
{
    /// <summary>The direction applied while measuring and drawing the child.</summary>
    public ContentDirection Direction { get; set; } = ContentDirection.RightToLeft;

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        canvas.PushDirection(Direction);
        try
        {
            return Child.Measure(canvas, availableSpace);
        }
        finally
        {
            canvas.PopDirection();
        }
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        canvas.PushDirection(Direction);
        try
        {
            Child.Draw(canvas, availableSpace);
        }
        finally
        {
            canvas.PopDirection();
        }
    }
}

namespace NetPdf.Layout.Elements;

/// <summary>
/// Applies a default text style to all text inside its subtree. Text elements merge their own
/// style into the ambient default, so only properties left unset on the inner text inherit.
/// Nested defaults themselves inherit from the enclosing default.
/// </summary>
public sealed class DefaultTextStyleElement : ContainerElement
{
    /// <summary>The style pushed as the ambient default while the subtree is measured and drawn.</summary>
    public required TextStyle Style { get; init; }

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace)
    {
        canvas.PushDefaultTextStyle(Style);
        try
        {
            return Child.Measure(canvas, availableSpace);
        }
        finally
        {
            canvas.PopDefaultTextStyle();
        }
    }

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        canvas.PushDefaultTextStyle(Style);
        try
        {
            Child.Draw(canvas, availableSpace);
        }
        finally
        {
            canvas.PopDefaultTextStyle();
        }
    }
}

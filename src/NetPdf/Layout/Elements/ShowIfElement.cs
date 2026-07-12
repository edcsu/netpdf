namespace NetPdf.Layout.Elements;

/// <summary>Renders its child only when <see cref="Condition"/> is true; otherwise occupies no space.</summary>
public sealed class ShowIfElement : ContainerElement
{
    /// <summary>Whether the child is rendered.</summary>
    public bool Condition { get; init; }

    /// <inheritdoc />
    public override SpacePlan Measure(ICanvas canvas, Size availableSpace) =>
        Condition ? Child.Measure(canvas, availableSpace) : SpacePlan.FullRender(Size.Zero);

    /// <inheritdoc />
    public override void Draw(ICanvas canvas, Size availableSpace)
    {
        if (Condition)
            Child.Draw(canvas, availableSpace);
    }
}

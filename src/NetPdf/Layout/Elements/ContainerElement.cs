namespace NetPdf.Layout.Elements;

/// <summary>
/// Base class for elements that wrap a single child, delegating measurement and drawing to it.
/// Derived classes override <see cref="Measure"/> and/or <see cref="Draw"/> to adjust the space
/// offered to the child or the position it is drawn at.
/// </summary>
public abstract class ContainerElement : IElement
{
    /// <summary>The wrapped child element. Defaults to an <see cref="EmptyElement"/>.</summary>
    public IElement Child { get; set; } = new EmptyElement();

    /// <inheritdoc />
    public virtual SpacePlan Measure(ICanvas canvas, Size availableSpace) => Child.Measure(canvas, availableSpace);

    /// <inheritdoc />
    public virtual void Draw(ICanvas canvas, Size availableSpace) => Child.Draw(canvas, availableSpace);
}

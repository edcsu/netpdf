namespace NetPdf.Layout;

/// <summary>
/// A node in the layout element tree, rendered with a two-pass protocol: the render loop calls
/// <see cref="Measure"/> to learn how much of the element fits in the offered space, then
/// <see cref="Draw"/> with the same space to paint it.
/// </summary>
/// <remarks>
/// Elements are stateful during a render pass: when <see cref="Measure"/> returns
/// <see cref="SpacePlanType.PartialRender"/>, <see cref="Draw"/> must paint the fitting part and
/// advance an internal cursor, after which the element is measured and drawn again on the next page
/// for the remainder. An element instance is single-use per render.
/// </remarks>
public interface IElement
{
    /// <summary>Determines how much of the element's remaining content fits in <paramref name="availableSpace"/>.</summary>
    SpacePlan Measure(ICanvas canvas, Size availableSpace);

    /// <summary>
    /// Draws the part of the element that fits in <paramref name="availableSpace"/>. Must be preceded by a
    /// <see cref="Measure"/> call with the same space.
    /// </summary>
    void Draw(ICanvas canvas, Size availableSpace);
}

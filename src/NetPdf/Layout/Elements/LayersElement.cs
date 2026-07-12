namespace NetPdf.Layout.Elements;

/// <summary>
/// Draws several elements on top of each other at the same origin. Pagination follows the
/// primary layer only; the other layers are drawn once per page and should be sized to fit
/// a single page.
/// </summary>
public sealed class LayersElement : IElement
{
    /// <summary>The stacked layers, drawn in order (first is the bottom).</summary>
    public IList<IElement> Layers { get; init; } = [];

    /// <summary>Index of the layer that drives measurement and pagination. Defaults to the first.</summary>
    public int PrimaryLayerIndex { get; init; }

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace) =>
        Layers.Count == 0
            ? SpacePlan.FullRender(Size.Zero)
            : Layers[PrimaryLayerIndex].Measure(canvas, availableSpace);

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        for (var i = 0; i < Layers.Count; i++)
        {
            if (i != PrimaryLayerIndex)
                Layers[i].Measure(canvas, availableSpace);
            canvas.Save();
            Layers[i].Draw(canvas, availableSpace);
            canvas.Restore();
        }
    }
}

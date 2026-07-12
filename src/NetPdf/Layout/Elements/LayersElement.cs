namespace NetPdf.Layout.Elements;

/// <summary>
/// Draws several elements on top of each other at the same origin. Pagination follows the
/// primary layer only; the other layers are drawn once per page and should be sized to fit
/// a single page.
/// </summary>
public sealed class LayersElement : IElement
{
    private readonly Dictionary<int, int> _zIndices = [];

    /// <summary>The stacked layers, drawn in order (first is the bottom).</summary>
    public IList<IElement> Layers { get; init; } = [];

    /// <summary>Index of the layer that drives measurement and pagination. Defaults to the first.</summary>
    public int PrimaryLayerIndex { get; set; }

    /// <summary>
    /// Assigns a z-index to the layer at <paramref name="layerIndex"/>. Layers draw in ascending
    /// z-index (default 0); layers sharing a z-index keep their insertion order. The z-index only
    /// orders siblings within this <see cref="LayersElement"/>.
    /// </summary>
    public void SetZIndex(int layerIndex, int zIndex) => _zIndices[layerIndex] = zIndex;

    /// <inheritdoc />
    public SpacePlan Measure(ICanvas canvas, Size availableSpace) =>
        Layers.Count == 0
            ? SpacePlan.FullRender(Size.Zero)
            : Layers[PrimaryLayerIndex].Measure(canvas, availableSpace);

    /// <inheritdoc />
    public void Draw(ICanvas canvas, Size availableSpace)
    {
        foreach (var i in DrawOrder())
        {
            if (i != PrimaryLayerIndex)
                Layers[i].Measure(canvas, availableSpace);
            canvas.Save();
            Layers[i].Draw(canvas, availableSpace);
            canvas.Restore();
        }
    }

    private IEnumerable<int> DrawOrder() =>
        Enumerable.Range(0, Layers.Count)
            .OrderBy(i => _zIndices.GetValueOrDefault(i)); // OrderBy is stable: ties keep insertion order
}

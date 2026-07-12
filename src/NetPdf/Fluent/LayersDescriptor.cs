using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>Configures elements stacked on top of each other at the same origin.</summary>
public sealed class LayersDescriptor
{
    private readonly LayersElement _layers;

    internal LayersDescriptor(LayersElement layers) => _layers = layers;

    /// <summary>
    /// Adds a layer drawn once per page. Layers draw in ascending <paramref name="zIndex"/>
    /// (default 0); layers sharing a z-index keep their insertion order (first added is the bottom).
    /// </summary>
    public ContainerDescriptor Layer(int zIndex = 0)
    {
        var index = _layers.Layers.Count;
        _layers.SetZIndex(index, zIndex);
        return new(element =>
        {
            // Placeholder is appended immediately so nested Layer() calls keep stable indices.
            while (_layers.Layers.Count <= index)
                _layers.Layers.Add(new EmptyElement());
            _layers.Layers[index] = element;
        });
    }

    /// <summary>Adds the layer that drives measurement and pagination.</summary>
    public ContainerDescriptor PrimaryLayer(int zIndex = 0)
    {
        _layers.PrimaryLayerIndex = _layers.Layers.Count;
        return Layer(zIndex);
    }
}

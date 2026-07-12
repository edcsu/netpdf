using NetPdf.Layout.Elements;

namespace NetPdf.Fluent;

/// <summary>Configures elements stacked on top of each other at the same origin.</summary>
public sealed class LayersDescriptor
{
    private readonly LayersElement _layers;

    internal LayersDescriptor(LayersElement layers) => _layers = layers;

    /// <summary>Adds a layer drawn once per page (first added is the bottom).</summary>
    public ContainerDescriptor Layer() => new(element => _layers.Layers.Add(element));

    /// <summary>Adds the layer that drives measurement and pagination.</summary>
    public ContainerDescriptor PrimaryLayer()
    {
        _layers.PrimaryLayerIndex = _layers.Layers.Count;
        return Layer();
    }
}

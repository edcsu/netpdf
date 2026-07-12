using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class ZIndexTests
{
    [Fact]
    public void Layers_WithoutZIndex_DrawInInsertionOrder()
    {
        var canvas = new TestCanvas();
        var layers = new LayersElement
        {
            Layers = { new TextElement("bottom"), new TextElement("top") },
        };

        layers.Draw(canvas, new Size(200, 100));

        Assert.Equal(["bottom", "top"], canvas.DrawnText.Select(d => d.Text));
    }

    [Fact]
    public void Layers_ZIndexReordersDrawing()
    {
        var canvas = new TestCanvas();
        var layers = new LayersElement
        {
            Layers = { new TextElement("a"), new TextElement("b"), new TextElement("c") },
        };
        layers.SetZIndex(0, 5);  // "a" drawn last (topmost)
        layers.SetZIndex(2, -1); // "c" drawn first (bottom)

        layers.Draw(canvas, new Size(200, 100));

        Assert.Equal(["c", "b", "a"], canvas.DrawnText.Select(d => d.Text));
    }

    [Fact]
    public void Layers_EqualZIndex_KeepsInsertionOrder()
    {
        var canvas = new TestCanvas();
        var layers = new LayersElement
        {
            Layers = { new TextElement("first"), new TextElement("second") },
        };
        layers.SetZIndex(0, 1);
        layers.SetZIndex(1, 1);

        layers.Draw(canvas, new Size(200, 100));

        Assert.Equal(["first", "second"], canvas.DrawnText.Select(d => d.Text));
    }

    [Fact]
    public void Fluent_LayerZIndex_ControlsDrawOrder()
    {
        var canvas = new TestCanvas();
        LayersElement layers = null!;
        var container = new NetPdf.Fluent.ContainerDescriptor(e => layers = (LayersElement)e);
        container.Layers(l =>
        {
            l.PrimaryLayer().Text("content");
            l.Layer(zIndex: -1).Text("watermark"); // added later but drawn underneath
        });

        layers.Draw(canvas, new Size(400, 100));

        Assert.Equal(["watermark", "content"], canvas.DrawnText.Select(d => d.Text));
    }
}

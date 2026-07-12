using NetPdf.Layout;
using Xunit;

namespace NetPdf.Tests;

public class TransformCanvasTests
{
    [Fact]
    public void Translate_OffsetsDrawnText()
    {
        var canvas = new TestCanvas();
        canvas.Translate(10, 20);
        canvas.DrawText("hi", new TextStyle(), 1, 2);

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(11, x, 6);
        Assert.Equal(22, y, 6);
    }

    [Fact]
    public void Rotate_90Degrees_MapsXAxisToYAxis()
    {
        var canvas = new TestCanvas();
        canvas.Rotate(90);
        canvas.DrawText("hi", new TextStyle(), 10, 0);

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(0, x, 6);
        Assert.Equal(10, y, 6);
    }

    [Fact]
    public void Rotate_AroundTranslatedOrigin()
    {
        var canvas = new TestCanvas();
        canvas.Translate(100, 100);
        canvas.Rotate(180);
        canvas.DrawText("hi", new TextStyle(), 10, 20);

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(90, x, 6);
        Assert.Equal(80, y, 6);
    }

    [Fact]
    public void Scale_ScalesPositions()
    {
        var canvas = new TestCanvas();
        canvas.Scale(2, 3);
        canvas.DrawText("hi", new TextStyle(), 10, 10);

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(20, x, 6);
        Assert.Equal(30, y, 6);
    }

    [Fact]
    public void NegativeScale_MirrorsAxis()
    {
        var canvas = new TestCanvas();
        canvas.Translate(50, 0);
        canvas.Scale(-1, 1);
        canvas.DrawText("hi", new TextStyle(), 10, 5);

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(40, x, 6);
        Assert.Equal(5, y, 6);
    }

    [Fact]
    public void SaveRestore_RevertsTransforms()
    {
        var canvas = new TestCanvas();
        canvas.Save();
        canvas.Translate(10, 10);
        canvas.Rotate(45);
        canvas.Scale(2, 2);
        canvas.Restore();
        canvas.DrawText("hi", new TextStyle(), 3, 4);

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(3, x, 6);
        Assert.Equal(4, y, 6);
    }

    [Fact]
    public void Lines_TransformBothEndpoints()
    {
        var canvas = new TestCanvas();
        canvas.Rotate(90);
        canvas.DrawLine(0, 0, 10, 0, System.Drawing.Color.Black, 1);

        var line = Assert.Single(canvas.DrawnLines);
        Assert.Equal(0, line.X1, 6);
        Assert.Equal(0, line.Y1, 6);
        Assert.Equal(0, line.X2, 6);
        Assert.Equal(10, line.Y2, 6);
    }
}

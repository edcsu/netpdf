using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class ShadowTests
{
    private static readonly Size Space = new(200, 100);

    [Fact]
    public void Shadow_DoesNotAffectMeasuredSize()
    {
        var element = new ShadowElement { Child = new TextElement("abcd") };
        var plan = element.Measure(new TestCanvas(), Space);

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(20, plan.Size.Width);
        Assert.Equal(10, plan.Size.Height);
    }

    [Fact]
    public void SolidShadow_DrawsOneOffsetRectangleBehindContent()
    {
        var canvas = new TestCanvas();
        var element = new ShadowElement
        {
            Style = new ShadowStyle { OffsetX = 3, OffsetY = 4, CornerRadius = 2 },
            Child = new TextElement("abcd"),
        };

        element.Draw(canvas, Space);

        var rect = Assert.Single(canvas.DrawnRectangles);
        Assert.Equal(3, rect.X);
        Assert.Equal(4, rect.Y);
        Assert.Equal(20, rect.Width);  // matches the child's size
        Assert.Equal(10, rect.Height);
        Assert.Equal(2, rect.CornerRadius);
        Assert.Equal(64, rect.Fill!.Value.A); // default 25% alpha black
        Assert.Single(canvas.DrawnText); // content drawn on top
    }

    [Fact]
    public void BlurredShadow_DrawsSteppedTranslucentRectangles()
    {
        var canvas = new TestCanvas();
        var element = new ShadowElement
        {
            Style = new ShadowStyle { Blur = 6 },
            Child = new TextElement("abcd"),
        };

        element.Draw(canvas, Space);

        Assert.Equal(3, canvas.DrawnRectangles.Count);
        // Rectangles expand outward with the spread.
        var widths = canvas.DrawnRectangles.Select(r => r.Width).ToList();
        Assert.Equal(widths.OrderBy(w => w), widths);
        Assert.All(canvas.DrawnRectangles, r => Assert.True(r.Fill!.Value.A < 64));
    }

    [Fact]
    public void Shadow_WrappingChild_DrawsNothing()
    {
        var canvas = new TestCanvas();
        var element = new ShadowElement { Child = new TextElement("abcd") };

        element.Draw(canvas, new Size(200, 5)); // too short for even one 10pt line

        Assert.Empty(canvas.DrawnRectangles);
        Assert.Empty(canvas.DrawnText);
    }
}

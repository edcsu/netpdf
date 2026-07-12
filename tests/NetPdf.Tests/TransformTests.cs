using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class TransformTests
{
    private static readonly Size Space = new(200, 100);

    [Fact]
    public void Rotate_DoesNotAffectMeasuredSize()
    {
        var element = new RotateElement { Degrees = 45, Child = new TextElement("abcd") };
        var canvas = new TestCanvas();

        var plan = element.Measure(canvas, Space);

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(20, plan.Size.Width); // 4 chars * 5pt, unrotated
        Assert.Equal(10, plan.Size.Height);
    }

    [Fact]
    public void Rotate_DrawsAtRotatedPosition()
    {
        var element = new RotateElement { Degrees = 90, Child = new TextElement("hi") };
        var canvas = new TestCanvas();
        canvas.Translate(100, 0);

        element.Draw(canvas, Space);

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(100, x, 6); // text at local (0,0) stays at the rotation origin
        Assert.Equal(0, y, 6);
    }

    [Fact]
    public void Scale_DoublesMeasuredSize()
    {
        var element = new ScaleElement { ScaleX = 2, ScaleY = 2, Child = new TextElement("abcd") };
        var canvas = new TestCanvas();

        var plan = element.Measure(canvas, Space);

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(40, plan.Size.Width);
        Assert.Equal(20, plan.Size.Height);
    }

    [Fact]
    public void Scale_OffersChildProportionallyMoreSpace()
    {
        // At 0.5 scale the child sees double the space: a 60-char line (300pt) fits 400pt.
        var element = new ScaleElement
        {
            ScaleX = 0.5,
            ScaleY = 0.5,
            Child = new TextElement(new string('x', 60)),
        };
        var canvas = new TestCanvas();

        var plan = element.Measure(canvas, Space);

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(150, plan.Size.Width); // 300pt * 0.5
        Assert.Equal(5, plan.Size.Height);
    }

    [Fact]
    public void Scale_ZeroFactor_Throws()
    {
        var element = new ScaleElement { ScaleX = 0, Child = new TextElement("x") };
        Assert.Throws<InvalidOperationException>(() => element.Measure(new TestCanvas(), Space));
    }

    [Fact]
    public void FlipHorizontal_MirrorsDrawPosition()
    {
        var element = new ScaleElement { ScaleX = -1, ScaleY = 1, Child = new TextElement("hi") };
        var canvas = new TestCanvas();

        element.Draw(canvas, Space);

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(200, x, 6); // local (0,0) maps to the right edge of the slot
        Assert.Equal(0, y, 6);
    }

    [Fact]
    public void FlipVertical_MirrorsDrawPosition()
    {
        var element = new ScaleElement { ScaleX = 1, ScaleY = -1, Child = new TextElement("hi") };
        var canvas = new TestCanvas();

        element.Draw(canvas, Space);

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(0, x, 6);
        Assert.Equal(100, y, 6); // local (0,0) maps to the bottom edge of the slot
    }

    [Fact]
    public void ScaleToFit_KeepsFullScaleWhenContentFits()
    {
        var element = new ScaleToFitElement { Child = new TextElement("hi") };
        var canvas = new TestCanvas();

        var plan = element.Measure(canvas, Space);

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(10, plan.Size.Width);
        Assert.Equal(10, plan.Size.Height);
    }

    [Fact]
    public void ScaleToFit_ShrinksOversizedContent()
    {
        // A single unbreakable 100-char word is 500pt wide; it must shrink to the 200pt slot.
        var element = new ScaleToFitElement { Child = new TextElement(new string('x', 100)) };
        var canvas = new TestCanvas();
        var tight = new Size(200, 15); // full-scale would need 3 lines = 30pt

        var plan = element.Measure(canvas, tight);

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.True(plan.Size.Width <= tight.Width + 1e-6);
        Assert.True(plan.Size.Height <= tight.Height + 1e-6);
        Assert.True(plan.Size.Height > 0);
    }

    [Fact]
    public void ScaleToFit_DrawUsesFoundScale()
    {
        var element = new ScaleToFitElement { Child = new TextElement(new string('x', 100)) };
        var canvas = new TestCanvas();
        var tight = new Size(200, 15);

        element.Measure(canvas, tight);
        element.Draw(canvas, tight);

        Assert.NotEmpty(canvas.DrawnText);
        // All draw positions stay within the slot.
        Assert.All(canvas.DrawnText, d =>
        {
            Assert.InRange(d.X, 0, tight.Width);
            Assert.InRange(d.Y, 0, tight.Height);
        });
    }
}

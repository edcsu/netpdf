using NetPdf.Layout;
using Color = System.Drawing.Color;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class DrawingElementTests
{
    private static ImageSource Source => ImageSource.FromBytes([1, 2, 3]);

    [Fact]
    public void Image_scales_to_available_width_preserving_ratio()
    {
        var canvas = new TestCanvas { FakeImageSize = new Size(200, 100) };
        var element = new ImageElement(Source);

        var plan = element.Measure(canvas, new Size(100, 500));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(100, plan.Size.Width);
        Assert.Equal(50, plan.Size.Height);
    }

    [Fact]
    public void Image_smaller_than_available_width_keeps_intrinsic_size()
    {
        var canvas = new TestCanvas { FakeImageSize = new Size(40, 20) };
        var element = new ImageElement(Source);

        var plan = element.Measure(canvas, new Size(100, 500));

        Assert.Equal(40, plan.Size.Width);
        Assert.Equal(20, plan.Size.Height);
    }

    [Fact]
    public void Image_wraps_when_scaled_height_exceeds_available_height()
    {
        var canvas = new TestCanvas { FakeImageSize = new Size(100, 100) };
        var element = new ImageElement(Source);

        var plan = element.Measure(canvas, new Size(100, 60));

        Assert.Equal(SpacePlanType.Wrap, plan.Type);
    }

    [Fact]
    public void Image_with_zero_intrinsic_size_renders_empty()
    {
        var canvas = new TestCanvas { FakeImageSize = Size.Zero };
        var element = new ImageElement(Source);

        var plan = element.Measure(canvas, new Size(100, 100));
        element.Draw(canvas, new Size(100, 100));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(0, plan.Size.Width);
        Assert.Empty(canvas.DrawnImages);
    }

    [Fact]
    public void Image_draw_records_scaled_rectangle()
    {
        var canvas = new TestCanvas { FakeImageSize = new Size(200, 100) };
        var element = new ImageElement(Source);

        element.Measure(canvas, new Size(100, 500));
        element.Draw(canvas, new Size(100, 500));

        var drawn = Assert.Single(canvas.DrawnImages);
        Assert.Equal(100, drawn.Width);
        Assert.Equal(50, drawn.Height);
    }

    [Fact]
    public void Horizontal_line_takes_full_width_and_thickness_height()
    {
        var canvas = new TestCanvas();
        var element = new LineElement { Thickness = 2 };

        var plan = element.Measure(canvas, new Size(300, 100));
        element.Draw(canvas, new Size(300, 100));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(300, plan.Size.Width);
        Assert.Equal(2, plan.Size.Height);
        var line = Assert.Single(canvas.DrawnLines);
        Assert.Equal((0, 1, 300, 1), (line.X1, line.Y1, line.X2, line.Y2));
        Assert.Equal(2, line.Thickness);
    }

    [Fact]
    public void Vertical_line_takes_full_height_and_thickness_width()
    {
        var canvas = new TestCanvas();
        var element = new LineElement { Orientation = LineOrientation.Vertical, Thickness = 4 };

        var plan = element.Measure(canvas, new Size(100, 200));
        element.Draw(canvas, new Size(100, 200));

        Assert.Equal(4, plan.Size.Width);
        Assert.Equal(200, plan.Size.Height);
        var line = Assert.Single(canvas.DrawnLines);
        Assert.Equal((2, 0, 2, 200), (line.X1, line.Y1, line.X2, line.Y2));
    }

    [Fact]
    public void Line_wraps_when_cross_axis_space_is_insufficient()
    {
        var canvas = new TestCanvas();
        var element = new LineElement { Thickness = 5 };

        Assert.Equal(SpacePlanType.Wrap, element.Measure(canvas, new Size(300, 3)).Type);
    }

    [Fact]
    public void Background_measurement_passes_through_to_child()
    {
        var canvas = new TestCanvas();
        var text = new TextElement("Hi");
        var background = new BackgroundElement { Color = Color.Yellow, Child = new TextElement("Hi") };
        var space = new Size(200, 100);

        Assert.Equal(text.Measure(canvas, space).Size.Height, background.Measure(canvas, space).Size.Height);
        Assert.Equal(text.Measure(canvas, space).Size.Width, background.Measure(canvas, space).Size.Width);
    }

    [Fact]
    public void Background_paints_rectangle_at_child_size_before_child()
    {
        var canvas = new TestCanvas();
        var background = new BackgroundElement { Color = Color.Yellow, Child = new TextElement("Hi") };
        var space = new Size(200, 100);

        background.Measure(canvas, space);
        background.Draw(canvas, space);

        var rect = Assert.Single(canvas.DrawnRectangles);
        Assert.Equal(Color.Yellow, rect.Fill);
        Assert.Equal(10, rect.Width);  // "Hi" = 2 chars * 5 pt
        Assert.Equal(10, rect.Height); // one 10 pt line
        Assert.Single(canvas.DrawnText);
    }

    [Fact]
    public void Background_records_corner_radius()
    {
        var canvas = new TestCanvas();
        var background = new BackgroundElement { Color = Color.Red, CornerRadius = 4, Child = new TextElement("Hi") };

        background.Measure(canvas, new Size(200, 100));
        background.Draw(canvas, new Size(200, 100));

        Assert.Equal(4, Assert.Single(canvas.DrawnRectangles).CornerRadius);
    }

    [Fact]
    public void Uniform_border_strokes_one_rectangle_inset_by_half_thickness()
    {
        var canvas = new TestCanvas();
        var border = new BorderElement { Left = 2, Top = 2, Right = 2, Bottom = 2, Child = new TextElement("Hi") };

        border.Measure(canvas, new Size(200, 100));
        border.Draw(canvas, new Size(200, 100));

        var rect = Assert.Single(canvas.DrawnRectangles);
        Assert.Null(rect.Fill);
        Assert.Equal(Color.Black, rect.Stroke);
        Assert.Equal((1.0, 1.0, 8.0, 8.0), (rect.X, rect.Y, rect.Width, rect.Height));
        Assert.Equal(2, rect.StrokeThickness);
    }

    [Fact]
    public void Per_side_border_strokes_individual_lines()
    {
        var canvas = new TestCanvas();
        var border = new BorderElement { Left = 2, Bottom = 4, Child = new TextElement("Hi") };

        border.Measure(canvas, new Size(200, 100));
        border.Draw(canvas, new Size(200, 100));

        Assert.Empty(canvas.DrawnRectangles);
        Assert.Equal(2, canvas.DrawnLines.Count);
        var left = canvas.DrawnLines[0];
        Assert.Equal((1.0, 0.0, 1.0, 10.0), (left.X1, left.Y1, left.X2, left.Y2));
        var bottom = canvas.DrawnLines[1];
        Assert.Equal((0.0, 8.0, 10.0, 8.0), (bottom.X1, bottom.Y1, bottom.X2, bottom.Y2));
    }

    [Fact]
    public void Zero_thickness_border_draws_nothing()
    {
        var canvas = new TestCanvas();
        var border = new BorderElement { Child = new TextElement("Hi") };

        border.Measure(canvas, new Size(200, 100));
        border.Draw(canvas, new Size(200, 100));

        Assert.Empty(canvas.DrawnRectangles);
        Assert.Empty(canvas.DrawnLines);
        Assert.Single(canvas.DrawnText);
    }
}

using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class ContainerElementTests
{
    [Fact]
    public void Padding_shrinks_space_offered_to_child_and_grows_reported_size()
    {
        var canvas = new TestCanvas();
        var child = new SpyElement(SpacePlan.FullRender(new Size(20, 10)));
        var padding = new PaddingElement { Left = 5, Top = 6, Right = 7, Bottom = 8, Child = child };

        var plan = padding.Measure(canvas, new Size(100, 100));

        Assert.Equal(new Size(88, 86).Width, child.LastOfferedSpace.Width);
        Assert.Equal(86, child.LastOfferedSpace.Height);
        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(32, plan.Size.Width);
        Assert.Equal(24, plan.Size.Height);
    }

    [Fact]
    public void Padding_with_no_inner_space_wraps()
    {
        var canvas = new TestCanvas();
        var padding = new PaddingElement { Left = 60, Right = 60, Child = new TextElement("x") };
        var plan = padding.Measure(canvas, new Size(100, 100));
        Assert.Equal(SpacePlanType.Wrap, plan.Type);
    }

    [Fact]
    public void Padding_translates_child_draw_position()
    {
        var canvas = new TestCanvas();
        var padding = new PaddingElement { Left = 10, Top = 20, Child = new TextElement("hi") };
        padding.Measure(canvas, new Size(100, 100));
        padding.Draw(canvas, new Size(100, 100));

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(10, x);
        Assert.Equal(20, y);
    }

    [Fact]
    public void Constrained_clamps_available_space_to_maximum()
    {
        var canvas = new TestCanvas();
        var child = new SpyElement(SpacePlan.FullRender(new Size(30, 10)));
        var constrained = new ConstrainedElement { MaxWidth = 40, MaxHeight = 25, Child = child };

        constrained.Measure(canvas, new Size(100, 100));

        Assert.Equal(40, child.LastOfferedSpace.Width);
        Assert.Equal(25, child.LastOfferedSpace.Height);
    }

    [Fact]
    public void Constrained_raises_reported_size_to_minimum()
    {
        var canvas = new TestCanvas();
        var child = new SpyElement(SpacePlan.FullRender(new Size(10, 5)));
        var constrained = new ConstrainedElement { MinWidth = 50, MinHeight = 30, Child = child };

        var plan = constrained.Measure(canvas, new Size(100, 100));

        Assert.Equal(50, plan.Size.Width);
        Assert.Equal(30, plan.Size.Height);
    }

    [Fact]
    public void Constrained_wraps_when_minimum_exceeds_available_space()
    {
        var canvas = new TestCanvas();
        var constrained = new ConstrainedElement { MinHeight = 200, Child = new EmptyElement() };
        var plan = constrained.Measure(canvas, new Size(100, 100));
        Assert.Equal(SpacePlanType.Wrap, plan.Type);
    }

    [Fact]
    public void Alignment_centers_child_horizontally_and_vertically()
    {
        var canvas = new TestCanvas();
        // "hi" measures 10 x 10 on the test canvas.
        var alignment = new AlignmentElement
        {
            Horizontal = HorizontalAlignment.Center,
            Vertical = VerticalAlignment.Middle,
            Child = new TextElement("hi"),
        };
        alignment.Measure(canvas, new Size(110, 110));
        alignment.Draw(canvas, new Size(110, 110));

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(50, x);
        Assert.Equal(50, y);
    }

    [Fact]
    public void Alignment_right_and_bottom_places_child_at_far_edges()
    {
        var canvas = new TestCanvas();
        var alignment = new AlignmentElement
        {
            Horizontal = HorizontalAlignment.Right,
            Vertical = VerticalAlignment.Bottom,
            Child = new TextElement("hi"),
        };
        alignment.Measure(canvas, new Size(110, 110));
        alignment.Draw(canvas, new Size(110, 110));

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(100, x);
        Assert.Equal(100, y);
    }

    [Fact]
    public void Aspect_ratio_fit_width_computes_height_from_width()
    {
        var canvas = new TestCanvas();
        var element = new AspectRatioElement(2) { Option = AspectRatioOption.FitWidth };
        var plan = element.Measure(canvas, new Size(100, 100));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(100, plan.Size.Width);
        Assert.Equal(50, plan.Size.Height);
    }

    [Fact]
    public void Aspect_ratio_fit_area_picks_the_largest_fitting_size()
    {
        var canvas = new TestCanvas();
        var element = new AspectRatioElement(2);
        var plan = element.Measure(canvas, new Size(100, 40));

        Assert.Equal(80, plan.Size.Width);
        Assert.Equal(40, plan.Size.Height);
    }

    [Fact]
    public void Aspect_ratio_wraps_when_fit_width_exceeds_available_height()
    {
        var canvas = new TestCanvas();
        var element = new AspectRatioElement(1) { Option = AspectRatioOption.FitWidth };
        var plan = element.Measure(canvas, new Size(100, 40));
        Assert.Equal(SpacePlanType.Wrap, plan.Type);
    }

    [Fact]
    public void Extend_reports_the_full_available_space()
    {
        var canvas = new TestCanvas();
        var extend = new ExtendElement { Child = new TextElement("hi") };
        var plan = extend.Measure(canvas, new Size(100, 80));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(100, plan.Size.Width);
        Assert.Equal(80, plan.Size.Height);
    }

    [Fact]
    public void Extend_horizontal_only_keeps_the_child_height()
    {
        var canvas = new TestCanvas();
        var extend = new ExtendElement { ExtendVertical = false, Child = new TextElement("hi") };
        var plan = extend.Measure(canvas, new Size(100, 80));

        Assert.Equal(100, plan.Size.Width);
        Assert.Equal(10, plan.Size.Height);
    }

    [Fact]
    public void Shrink_reports_the_child_natural_size()
    {
        var canvas = new TestCanvas();
        var shrink = new ShrinkElement { Child = new TextElement("hi") };
        var plan = shrink.Measure(canvas, new Size(100, 80));

        Assert.Equal(10, plan.Size.Width);
        Assert.Equal(10, plan.Size.Height);
    }

    [Fact]
    public void Unconstrained_occupies_no_space_but_measures_child_with_unlimited_space()
    {
        var canvas = new TestCanvas();
        var child = new SpyElement(SpacePlan.FullRender(new Size(500, 500)));
        var unconstrained = new UnconstrainedElement { Child = child };

        var plan = unconstrained.Measure(canvas, new Size(10, 10));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(0, plan.Size.Width);
        Assert.Equal(Size.Max.Width, child.LastOfferedSpace.Width);
    }

    [Fact]
    public void Offset_translates_child_draw_position()
    {
        var canvas = new TestCanvas();
        var offset = new OffsetElement { OffsetX = 15, OffsetY = -5, Child = new TextElement("hi") };
        offset.Measure(canvas, new Size(100, 100));
        offset.Draw(canvas, new Size(100, 100));

        var (_, x, y) = Assert.Single(canvas.DrawnText);
        Assert.Equal(15, x);
        Assert.Equal(-5, y);
    }

    [Fact]
    public void Container_element_defaults_to_an_empty_child()
    {
        var canvas = new TestCanvas();
        var plan = new ShrinkElement().Measure(canvas, new Size(100, 100));
        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(0, plan.Size.Width);
    }

    private sealed class SpyElement : IElement
    {
        private readonly SpacePlan _plan;

        public SpyElement(SpacePlan plan) => _plan = plan;

        public Size LastOfferedSpace { get; private set; }

        public SpacePlan Measure(ICanvas canvas, Size availableSpace)
        {
            LastOfferedSpace = availableSpace;
            return _plan;
        }

        public void Draw(ICanvas canvas, Size availableSpace) { }
    }
}

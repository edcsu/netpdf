using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class DefaultTextStyleTests
{
    [Fact]
    public void Merge_prefers_own_properties_over_fallback()
    {
        var own = new TextStyle { FontSize = 20 };
        var fallback = new TextStyle { FontSize = 8, Bold = true };

        var merged = own.Merge(fallback);

        Assert.Equal(20, merged.FontSize);
        Assert.True(merged.Bold);
        Assert.Null(merged.FontFamily);
    }

    [Fact]
    public void Resolve_fills_library_defaults()
    {
        var resolved = new TextStyle().Resolve();

        Assert.Equal("Arial", resolved.FontFamily);
        Assert.Equal(12, resolved.FontSize);
        Assert.False(resolved.Bold);
        Assert.Equal(System.Drawing.Color.Black, resolved.Color);
        Assert.Equal(1.0, resolved.LineHeight);
        Assert.Equal(0, resolved.LetterSpacing);
    }

    [Fact]
    public void Nested_default_styles_inherit_from_enclosing_default()
    {
        var canvas = new TestCanvas();
        var inner = new StyleProbe();
        var tree = new DefaultTextStyleElement
        {
            Style = new TextStyle { Bold = true, FontSize = 8 },
            Child = new DefaultTextStyleElement
            {
                Style = new TextStyle { FontSize = 16 },
                Child = inner,
            },
        };

        tree.Measure(canvas, new Size(100, 100));

        Assert.Equal(16, inner.Seen!.FontSize);
        Assert.True(inner.Seen.Bold);
    }

    [Fact]
    public void Default_style_is_popped_after_subtree()
    {
        var canvas = new TestCanvas();
        var element = new DefaultTextStyleElement { Style = new TextStyle { Bold = true } };

        element.Measure(canvas, new Size(100, 100));

        Assert.Null(canvas.DefaultTextStyle.Bold);
    }

    private sealed class StyleProbe : IElement
    {
        public TextStyle? Seen { get; private set; }

        public SpacePlan Measure(ICanvas canvas, Size availableSpace)
        {
            Seen = canvas.DefaultTextStyle;
            return SpacePlan.FullRender(Size.Zero);
        }

        public void Draw(ICanvas canvas, Size availableSpace) => Seen = canvas.DefaultTextStyle;
    }
}

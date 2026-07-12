using NetPdf.Fluent;
using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class RichTextTests
{
    // TestCanvas metrics: every character 5 pt wide, every line 10 pt tall.

    private static RichTextElement Element(params TextSpan[] spans) => new(spans);

    [Fact]
    public void Adjacent_spans_with_same_style_merge_into_one_run()
    {
        var canvas = new TestCanvas();
        var element = Element(
            new TextSpan { Text = "Hello " },
            new TextSpan { Text = "world" });

        element.Draw(canvas, new Size(200, 100));

        var drawn = Assert.Single(canvas.DrawnText);
        Assert.Equal("Hello world", drawn.Text);
    }

    [Fact]
    public void Styled_spans_draw_side_by_side()
    {
        var canvas = new TestCanvas();
        var element = Element(
            new TextSpan { Text = "ab " },
            new TextSpan { Text = "cd", Style = new TextStyle { Bold = true } });

        element.Draw(canvas, new Size(200, 100));

        Assert.Equal(2, canvas.DrawnText.Count);
        Assert.Equal(("ab ", 0.0), (canvas.DrawnText[0].Text, canvas.DrawnText[0].X));
        // "ab " is 3 chars = 15 pt wide, so the bold run starts at x = 15.
        Assert.Equal(("cd", 15.0), (canvas.DrawnText[1].Text, canvas.DrawnText[1].X));
    }

    [Fact]
    public void Words_wrap_across_span_boundaries()
    {
        var canvas = new TestCanvas();
        // "aaaa bbbb" = 45 pt; max width 30 pt fits "aaaa " (25) but not "aaaa bbbb".
        var element = Element(
            new TextSpan { Text = "aaaa " },
            new TextSpan { Text = "bbbb", Style = new TextStyle { Bold = true } });

        var plan = element.Measure(canvas, new Size(30, 100));
        element.Draw(canvas, new Size(30, 100));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(2, canvas.DrawnText.Count);
        Assert.Equal(0, canvas.DrawnText[0].Y);
        Assert.Equal(10, canvas.DrawnText[1].Y);
    }

    [Fact]
    public void Measure_wraps_when_no_line_fits()
    {
        var canvas = new TestCanvas();
        var element = Element(new TextSpan { Text = "hello" });

        Assert.Equal(SpacePlanType.Wrap, element.Measure(canvas, new Size(100, 5)).Type);
    }

    [Fact]
    public void Paginates_by_whole_lines()
    {
        var canvas = new TestCanvas();
        // Three forced lines, 10 pt each; page height 20 pt fits two lines.
        var element = Element(new TextSpan { Text = "one\ntwo\nthree" });

        var first = element.Measure(canvas, new Size(100, 20));
        Assert.Equal(SpacePlanType.PartialRender, first.Type);
        Assert.Equal(20, first.Size.Height);
        element.Draw(canvas, new Size(100, 20));

        var second = element.Measure(canvas, new Size(100, 20));
        Assert.Equal(SpacePlanType.FullRender, second.Type);
        element.Draw(canvas, new Size(100, 20));

        Assert.Equal(["one", "two", "three"], canvas.DrawnText.Select(t => t.Text));
    }

    [Fact]
    public void Line_height_multiplier_scales_line_advance()
    {
        var canvas = new TestCanvas();
        var element = new RichTextElement(
            [new TextSpan { Text = "a\nb" }],
            new TextStyle { LineHeight = 2 });

        var plan = element.Measure(canvas, new Size(100, 100));
        element.Draw(canvas, new Size(100, 100));

        Assert.Equal(40, plan.Size.Height);
        Assert.Equal(20, canvas.DrawnText[1].Y);
    }

    [Fact]
    public void Hyperlink_span_emits_link_annotation()
    {
        var canvas = new TestCanvas();
        var element = Element(new TextSpan { Text = "click", LinkUrl = "https://example.com" });

        element.Draw(canvas, new Size(200, 100));

        var link = Assert.Single(canvas.Links);
        Assert.Equal("https://example.com", link.Url);
        Assert.Equal(25, link.Width);
    }

    [Fact]
    public void Block_style_inherits_into_spans_and_span_overrides_win()
    {
        var canvas = new TestCanvas();
        var recorded = new RecordingCanvas();
        var element = new RichTextElement(
            [
                new TextSpan { Text = "plain\n" },
                new TextSpan { Text = "big", Style = new TextStyle { FontSize = 20 } },
            ],
            new TextStyle { FontSize = 8, Bold = true });

        element.Draw(recorded, new Size(400, 100));

        Assert.Equal(8, recorded.Styles[0].FontSize);
        Assert.Equal(20, recorded.Styles[1].FontSize);
        Assert.All(recorded.Styles, s => Assert.True(s.Bold));
        _ = canvas;
    }

    [Fact]
    public void Fluent_text_descriptor_builds_spans_and_links()
    {
        var canvas = new TestCanvas();
        IElement? element = null;
        var descriptor = new ContainerDescriptor(e => element = e);
        descriptor.Text(t =>
        {
            t.Span("Visit ");
            t.Hyperlink("our site", "https://example.com");
            t.Span(" today").Bold();
        });

        element!.Draw(canvas, new Size(400, 100));

        Assert.Single(canvas.Links);
        Assert.Contains(canvas.DrawnText, d => d.Text.Contains("Visit"));
    }

    private sealed class RecordingCanvas : ICanvas
    {
        private readonly Stack<TextStyle> _defaults = new([new TextStyle()]);
        public List<TextStyle> Styles { get; } = [];
        public PageContext PageContext { get; } = new();
        public TextStyle DefaultTextStyle => _defaults.Peek();
        public void PushDefaultTextStyle(TextStyle style) => _defaults.Push(style.Merge(DefaultTextStyle));
        public void PopDefaultTextStyle() => _defaults.Pop();
        public void DrawText(string text, TextStyle style, double x, double y) => Styles.Add(style);
        public Size MeasureText(string text, TextStyle style) => new(text.Length * 5, 10);
        public Size MeasureImage(ImageSource image) => new(100, 50);
        public void DrawImage(ImageSource image, double x, double y, double width, double height) { }
        public void DrawLine(double x1, double y1, double x2, double y2, System.Drawing.Color color, double thickness) { }
        public void DrawRectangle(double x, double y, double width, double height,
            System.Drawing.Color? fill, System.Drawing.Color? stroke = null,
            double strokeThickness = 1, double cornerRadius = 0) { }
        public void DrawLink(double x, double y, double width, double height, string url) { }
        public void Translate(double dx, double dy) { }
        public void Save() { }
        public void Restore() { }
    }
}

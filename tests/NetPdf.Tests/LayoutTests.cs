using NetPdf.Layout;
using Xunit;
using NetPdf.Layout.Elements;

namespace NetPdf.Tests;

public class LayoutTests
{
    [Fact]
    public void Space_plan_factories_report_expected_type_and_size()
    {
        var wrap = SpacePlan.Wrap();
        Assert.Equal(SpacePlanType.Wrap, wrap.Type);
        Assert.Equal(0, wrap.Size.Height);

        var partial = SpacePlan.PartialRender(new Size(10, 20));
        Assert.Equal(SpacePlanType.PartialRender, partial.Type);
        Assert.Equal(20, partial.Size.Height);

        var full = SpacePlan.FullRender(new Size(10, 20));
        Assert.Equal(SpacePlanType.FullRender, full.Type);
        Assert.Equal(10, full.Size.Width);
    }

    [Fact]
    public void Size_rejects_negative_dimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Size(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Size(0, -1));
    }

    [Fact]
    public void Empty_element_renders_single_page()
    {
        var bytes = PdfFile.Create().AddContent(new EmptyElement()).ToBytes();
        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
    }

    [Fact]
    public void Short_text_renders_on_one_page()
    {
        var bytes = PdfFile.Create()
            .AddContent(new TextElement("Hello layout engine"))
            .ToBytes();
        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
        Assert.Contains("Hello layout engine", pdf.ExtractText());
    }

    [Fact]
    public void Long_text_paginates_across_multiple_pages()
    {
        var text = string.Join("\n", Enumerable.Range(1, 200).Select(i => $"Line number {i}"));
        var bytes = PdfFile.Create().AddContent(new TextElement(text)).ToBytes();
        using var pdf = PdfFile.Open(bytes);

        Assert.True(pdf.PageCount > 1);
        var all = pdf.ExtractText();
        Assert.Contains("Line number 1", all);
        Assert.Contains("Line number 200", all);
        Assert.Contains("Line number 1", pdf.ExtractText(0));
        Assert.Contains("Line number 200", pdf.ExtractText(pdf.PageCount - 1));
    }

    [Fact]
    public void Text_element_returns_wrap_when_no_line_fits()
    {
        var canvas = new FakeCanvas();
        var element = new TextElement("some words to wrap");
        var plan = element.Measure(canvas, new Size(100, 2)); // shorter than one line
        Assert.Equal(SpacePlanType.Wrap, plan.Type);
    }

    [Fact]
    public void Text_element_wraps_words_to_the_available_width()
    {
        var canvas = new FakeCanvas(); // 5 pt per character, 10 pt line height
        var element = new TextElement("aaaa bbbb cccc");
        // 45 pt fits "aaaa bbbb" (9 chars); the rest wraps to a second line.
        var plan = element.Measure(canvas, new Size(45, 100));
        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(20, plan.Size.Height);
    }

    [Fact]
    public void Content_too_large_for_empty_page_throws_layout_exception()
    {
        var builder = PdfFile.Create();
        Assert.Throws<LayoutException>(() => builder.AddContent(new AlwaysWrapElement()));
    }

    [Fact]
    public void Renderer_throws_on_element_that_never_progresses()
    {
        var builder = PdfFile.Create();
        Assert.Throws<LayoutException>(() => builder.AddContent(new NeverProgressingElement()));
    }

    [Fact]
    public void Add_content_validates_arguments()
    {
        var builder = PdfFile.Create();
        Assert.Throws<ArgumentNullException>(() => builder.AddContent(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddContent(new EmptyElement(), 0, 842, 50));
    }

    private sealed class AlwaysWrapElement : IElement
    {
        public SpacePlan Measure(ICanvas canvas, Size availableSpace) => SpacePlan.Wrap();
        public void Draw(ICanvas canvas, Size availableSpace) { }
    }

    private sealed class NeverProgressingElement : IElement
    {
        public SpacePlan Measure(ICanvas canvas, Size availableSpace) => SpacePlan.PartialRender(Size.Zero);
        public void Draw(ICanvas canvas, Size availableSpace) { }
    }

    /// <summary>Deterministic canvas: every character is 5 pt wide, every line 10 pt tall.</summary>
    private sealed class FakeCanvas : ICanvas
    {
        public void DrawText(string text, TextStyle style, double x, double y) { }
        public Size MeasureText(string text, TextStyle style) => new(text.Length * 5, 10);
        public void Translate(double dx, double dy) { }
        public void Save() { }
        public void Restore() { }
    }
}

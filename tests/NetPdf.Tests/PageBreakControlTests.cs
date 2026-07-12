using NetPdf.Fluent;
using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class PageBreakControlTests
{
    // TestCanvas metrics: every character 5 pt wide, every line 10 pt tall.

    private static TextElement Lines(int count) =>
        new(string.Join('\n', Enumerable.Range(1, count).Select(i => $"line{i}")));

    [Fact]
    public void ShowEntire_wraps_when_child_would_split()
    {
        var canvas = new TestCanvas();
        var element = new ShowEntireElement { Child = Lines(5) };

        // 5 lines need 50 pt; only 30 available -> defer entirely.
        Assert.Equal(SpacePlanType.Wrap, element.Measure(canvas, new Size(100, 30)).Type);
        Assert.Equal(SpacePlanType.FullRender, element.Measure(canvas, new Size(100, 50)).Type);
    }

    [Fact]
    public void EnsureSpace_allows_split_only_above_min_height()
    {
        var canvas = new TestCanvas();
        var element = new EnsureSpaceElement { MinHeight = 40, Child = Lines(10) };

        // 30 pt available (< 40): a partial render is rejected.
        Assert.Equal(SpacePlanType.Wrap, element.Measure(canvas, new Size(100, 30)).Type);
        // 40 pt available (>= 40): partial render passes through.
        Assert.Equal(SpacePlanType.PartialRender, element.Measure(canvas, new Size(100, 40)).Type);
    }

    [Fact]
    public void ShowOnce_becomes_empty_after_first_full_render()
    {
        var canvas = new TestCanvas();
        var element = new ShowOnceElement { Child = Lines(2) };
        var space = new Size(100, 100);

        Assert.Equal(SpacePlanType.FullRender, element.Measure(canvas, space).Type);
        element.Draw(canvas, space);
        Assert.Equal(2, canvas.DrawnText.Count);

        var second = element.Measure(canvas, space);
        Assert.Equal(SpacePlanType.FullRender, second.Type);
        Assert.Equal(0, second.Size.Height);
        element.Draw(canvas, space);
        Assert.Equal(2, canvas.DrawnText.Count);
    }

    [Fact]
    public void SkipOnce_hides_first_cycle_then_renders()
    {
        var canvas = new TestCanvas();
        var element = new SkipOnceElement { Child = Lines(1) };
        var space = new Size(100, 100);

        var first = element.Measure(canvas, space);
        Assert.Equal((SpacePlanType.FullRender, 0.0), (first.Type, first.Size.Height));
        element.Draw(canvas, space);
        Assert.Empty(canvas.DrawnText);

        element.Measure(canvas, space);
        element.Draw(canvas, space);
        Assert.Single(canvas.DrawnText);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void ShowIf_renders_only_when_condition_holds(bool condition, int expectedDraws)
    {
        var canvas = new TestCanvas();
        var element = new ShowIfElement { Condition = condition, Child = Lines(1) };
        var space = new Size(100, 100);

        element.Measure(canvas, space);
        element.Draw(canvas, space);

        Assert.Equal(expectedDraws, canvas.DrawnText.Count);
    }

    [Fact]
    public void StopPaging_truncates_overflowing_child()
    {
        var canvas = new TestCanvas();
        var element = new StopPagingElement { Child = Lines(10) };
        var space = new Size(100, 30);

        var plan = element.Measure(canvas, space);
        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(30, plan.Size.Height);

        element.Draw(canvas, space);
        Assert.Equal(3, canvas.DrawnText.Count);
    }

    [Fact]
    public void PageBreak_consumes_page_once_then_is_empty()
    {
        var canvas = new TestCanvas();
        var element = new PageBreakElement();
        var space = new Size(100, 200);

        var first = element.Measure(canvas, space);
        Assert.Equal((SpacePlanType.PartialRender, 200.0), (first.Type, first.Size.Height));
        element.Draw(canvas, space);

        var second = element.Measure(canvas, space);
        Assert.Equal((SpacePlanType.FullRender, 0.0), (second.Type, second.Size.Height));
    }

    [Fact]
    public void Repeat_recreates_content_every_cycle()
    {
        var canvas = new TestCanvas();
        var element = new RepeatElement(() => Lines(1));
        var space = new Size(100, 100);

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(SpacePlanType.PartialRender, element.Measure(canvas, space).Type);
            element.Draw(canvas, space);
        }

        Assert.Equal(3, canvas.DrawnText.Count);
    }

    [Fact]
    public void PageBreak_adds_a_page_in_rendered_document()
    {
        var bytes = Document.Create(doc => doc.Page(page => page.Content(content => content.Column(column =>
        {
            column.Item().Text("first");
            column.Item().PageBreak();
            column.Item().Text("second");
        })))).ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(2, pdf.PageCount);
        Assert.Contains("first", pdf.ExtractText(0));
        Assert.Contains("second", pdf.ExtractText(1));
    }

    [Fact]
    public void StopPaging_caps_long_text_to_one_page()
    {
        var longText = string.Join(" ", Enumerable.Repeat("wordy content flows", 2000));
        var bytes = Document.Create(doc => doc.Page(page =>
            page.Content(content => content.StopPaging().Text(longText)))).ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
    }

    [Fact]
    public void ShowEntire_pushes_block_to_next_page()
    {
        var filler = string.Join("\n", Enumerable.Repeat("filler line", 40));
        var block = string.Join("\n", Enumerable.Repeat("kept together", 30));
        var bytes = Document.Create(doc => doc.Page(page => page.Content(content => content.Column(column =>
        {
            column.Item().Text(filler);
            column.Item().ShowEntire().Text(block);
        })))).ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(2, pdf.PageCount);
        Assert.DoesNotContain("kept together", pdf.ExtractText(0));
        Assert.Contains("kept together", pdf.ExtractText(1));
    }
}

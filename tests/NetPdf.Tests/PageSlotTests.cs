using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class PageSlotTests
{
    private static byte[] Render(PageLayout layout, PageContext? context = null)
    {
        var builder = PdfFile.Create();
        builder.AddPageLayout(layout, context ?? new PageContext());
        return builder.ToBytes();
    }

    private static IElement LongContent(int lines) =>
        new TextElement(string.Join("\n", Enumerable.Range(1, lines).Select(i => $"Body line {i}")));

    [Fact]
    public void Header_repeats_on_every_page()
    {
        var layout = new PageLayout
        {
            Header = () => new TextElement("REPORT HEADER"),
            Content = () => LongContent(200),
        };
        using var pdf = PdfFile.Open(Render(layout));

        Assert.True(pdf.PageCount > 1);
        for (var i = 0; i < pdf.PageCount; i++)
            Assert.Contains("REPORT HEADER", pdf.ExtractText(i));
    }

    [Fact]
    public void Footer_repeats_on_every_page()
    {
        var layout = new PageLayout
        {
            Footer = () => new TextElement("PAGE FOOTER"),
            Content = () => LongContent(200),
        };
        using var pdf = PdfFile.Open(Render(layout));

        Assert.True(pdf.PageCount > 1);
        for (var i = 0; i < pdf.PageCount; i++)
            Assert.Contains("PAGE FOOTER", pdf.ExtractText(i));
    }

    [Fact]
    public void Page_number_renders_current_page_per_page()
    {
        var layout = new PageLayout
        {
            Footer = () => new PageNumberText("Page {number}"),
            Content = () => LongContent(200),
        };
        using var pdf = PdfFile.Open(Render(layout));

        Assert.True(pdf.PageCount > 1);
        Assert.Contains("Page 1", pdf.ExtractText(0));
        Assert.Contains($"Page {pdf.PageCount}", pdf.ExtractText(pdf.PageCount - 1));
    }

    [Fact]
    public void Page_number_total_renders_placeholder_when_unknown()
    {
        var layout = new PageLayout
        {
            Footer = () => new PageNumberText("Page {number} of {total}"),
            Content = () => new TextElement("short"),
        };
        using var pdf = PdfFile.Open(Render(layout));
        Assert.Contains("Page 1 of ?", pdf.ExtractText(0));
    }

    [Fact]
    public void Page_number_total_renders_when_context_knows_it()
    {
        var context = new PageContext { TotalPages = 7 };
        var layout = new PageLayout
        {
            Footer = () => new PageNumberText("Page {number} of {total}"),
            Content = () => new TextElement("short"),
        };
        using var pdf = PdfFile.Open(Render(layout, context));
        Assert.Contains("Page 1 of 7", pdf.ExtractText(0));
    }

    [Fact]
    public void Header_too_large_for_the_page_throws_layout_exception()
    {
        var layout = new PageLayout
        {
            Header = () => LongContent(200),
            Content = () => new TextElement("short"),
        };
        Assert.Throws<LayoutException>(() => Render(layout));
    }

    [Fact]
    public void Header_and_footer_filling_the_page_throws_layout_exception()
    {
        var layout = new PageLayout
        {
            PageHeight = 130, // 30 pt of content space after 50 pt margins
            Header = () => new TextElement("h"),
            Footer = () => new TextElement("f"),
            Content = () => new TextElement("body"),
        };
        Assert.Throws<LayoutException>(() => Render(layout));
    }
}

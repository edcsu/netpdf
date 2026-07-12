using NetPdf.Fluent;
using NetPdf.Layout;
using Xunit;

namespace NetPdf.Tests;

public class FluentApiTests
{
    [Fact]
    public void Document_create_produces_a_pdf_with_text()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page
                    .Content(content => content.Text("Hello fluent world"))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
        Assert.Contains("Hello fluent world", pdf.ExtractText());
    }

    [Fact]
    public void Nested_containers_compose()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page
                    .Size(PageSizes.A4)
                    .Margin(40)
                    .Content(content => content
                        .Padding(10)
                        .MaxWidth(300)
                        .Column(column =>
                        {
                            column.Spacing(6);
                            column.Item().Text("First item");
                            column.Item().AlignCenter().Text("Centered item");
                            column.Item().Row(row =>
                            {
                                row.ConstantItem(80).Text("Left cell");
                                row.RelativeItem().Text("Right cell");
                            });
                        }))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        var text = pdf.ExtractText();
        Assert.Contains("First item", text);
        Assert.Contains("Centered item", text);
        Assert.Contains("Left cell", text);
        Assert.Contains("Right cell", text);
    }

    [Fact]
    public void Header_footer_and_total_page_numbers_render_on_every_page()
    {
        var body = string.Join("\n", Enumerable.Range(1, 200).Select(i => $"Report line {i}"));
        var bytes = Document.Create(doc => doc
                .Page(page => page
                    .Header(header => header.Text("ACME QUARTERLY"))
                    .Content(content => content.Text(body))
                    .Footer(footer => footer.AlignCenter().PageNumber("Page {number} of {total}"))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.True(pdf.PageCount > 1);
        for (var i = 0; i < pdf.PageCount; i++)
        {
            var text = pdf.ExtractText(i);
            Assert.Contains("ACME QUARTERLY", text);
            Assert.Contains($"Page {i + 1} of {pdf.PageCount}", text);
        }
    }

    [Fact]
    public void Multiple_page_sections_render_sequentially()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page.Content(c => c.Text("Section one")))
                .Page(page => page
                    .Size(PageSizes.A5)
                    .Content(c => c.Text("Section two"))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(2, pdf.PageCount);
        Assert.Contains("Section one", pdf.ExtractText(0));
        Assert.Contains("Section two", pdf.ExtractText(1));
    }

    [Fact]
    public void Page_numbers_continue_across_page_sections()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page
                    .Content(c => c.Text("one"))
                    .Footer(f => f.PageNumber("Page {number} of {total}")))
                .Page(page => page
                    .Content(c => c.Text("two"))
                    .Footer(f => f.PageNumber("Page {number} of {total}"))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Contains("Page 1 of 2", pdf.ExtractText(0));
        Assert.Contains("Page 2 of 2", pdf.ExtractText(1));
    }

    [Fact]
    public void Empty_page_section_renders_a_blank_page()
    {
        var bytes = Document.Create(doc => doc.Page(_ => { })).ToBytes();
        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
    }

    [Fact]
    public void Custom_element_escape_hatch_is_supported()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page.Content(c => c.Element(new Layout.Elements.TextElement("custom element")))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Contains("custom element", pdf.ExtractText());
    }

    // 1x1 transparent PNG.
    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public void Background_border_line_and_image_render_to_a_valid_pdf()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page
                    .Content(content => content.Column(column =>
                    {
                        column.Item()
                            .Background(System.Drawing.Color.LightYellow, cornerRadius: 4)
                            .Border(1.5, System.Drawing.Color.DarkGray)
                            .Padding(8)
                            .Text("Decorated block");
                        column.Item().LineHorizontal(2, System.Drawing.Color.Red);
                        column.Item().Width(50).Image(ImageSource.FromBytes(OnePixelPng));
                    }))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
        Assert.Contains("Decorated block", pdf.ExtractText());
    }

    [Fact]
    public void Background_and_border_around_paginating_text_span_pages()
    {
        var body = string.Join("\n", Enumerable.Range(1, 300).Select(i => $"Row {i}"));
        var bytes = Document.Create(doc => doc
                .Page(page => page
                    .Content(content => content
                        .Background(System.Drawing.Color.WhiteSmoke)
                        .Border(1)
                        .Text(body))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.True(pdf.PageCount > 1);
        Assert.Contains("Row 1", pdf.ExtractText(0));
        Assert.Contains("Row 300", pdf.ExtractText(pdf.PageCount - 1));
    }
}

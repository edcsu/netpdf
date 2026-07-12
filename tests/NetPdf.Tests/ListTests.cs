using NetPdf.Fluent;
using NetPdf.Layout;
using Xunit;

namespace NetPdf.Tests;

public class ListTests
{
    [Fact]
    public void Unordered_list_renders_bullets_and_content()
    {
        var canvas = new TestCanvas();
        NetPdf.Layout.IElement? element = null;
        var container = new ContainerDescriptor(e => element = e);
        container.List(list =>
        {
            list.Item().Text("first");
            list.Item().Text("second");
        });

        element!.Draw(canvas, new Size(200, 100));

        Assert.Equal(2, canvas.DrawnText.Count(t => t.Text == "•"));
        // Content starts after the 18 pt marker column.
        Assert.All(canvas.DrawnText.Where(t => t.Text != "•"), t => Assert.Equal(18, t.X));
    }

    [Fact]
    public void Ordered_list_numbers_items_sequentially()
    {
        var canvas = new TestCanvas();
        NetPdf.Layout.IElement? element = null;
        var container = new ContainerDescriptor(e => element = e);
        container.List(list =>
        {
            list.Ordered();
            list.Item().Text("one");
            list.Item().Text("two");
            list.Item().Text("three");
        });

        element!.Draw(canvas, new Size(200, 100));

        Assert.Contains(canvas.DrawnText, t => t.Text == "1.");
        Assert.Contains(canvas.DrawnText, t => t.Text == "2.");
        Assert.Contains(canvas.DrawnText, t => t.Text == "3.");
    }

    [Fact]
    public void List_roundtrips_through_rendered_document()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page.Content(content => content.List(list =>
                {
                    list.Ordered("{0})").Spacing(4);
                    list.Item().Text("apples");
                    list.Item().Text("bananas");
                }))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        var text = pdf.ExtractText();
        Assert.Contains("1)", text);
        Assert.Contains("apples", text);
        Assert.Contains("2)", text);
        Assert.Contains("bananas", text);
    }

    [Fact]
    public void Long_list_paginates_across_pages()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page.Content(content => content.List(list =>
                {
                    list.Ordered();
                    for (var i = 1; i <= 120; i++)
                        list.Item().Text($"list entry {i}");
                }))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.True(pdf.PageCount >= 2);
        Assert.Contains("list entry 1", pdf.ExtractText(0));
        Assert.Contains("list entry 120", pdf.ExtractText(pdf.PageCount - 1));
    }
}

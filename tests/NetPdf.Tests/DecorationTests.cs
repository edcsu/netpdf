using NetPdf.Fluent;
using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class DecorationTests
{
    // TestCanvas metrics: every character 5 pt wide, every line 10 pt tall.

    [Fact]
    public void Before_and_after_surround_content()
    {
        var canvas = new TestCanvas();
        var element = new DecorationElement
        {
            Before = () => new TextElement("before"),
            After = () => new TextElement("after"),
            Content = new TextElement("body"),
        };

        var plan = element.Measure(canvas, new Size(200, 100));
        element.Draw(canvas, new Size(200, 100));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(30, plan.Size.Height);
        Assert.Equal(["before", "body", "after"], canvas.DrawnText.Select(t => t.Text));
        Assert.Equal([0.0, 10.0, 20.0], canvas.DrawnText.Select(t => t.Y));
    }

    [Fact]
    public void Slots_repeat_on_every_page_of_paginating_content()
    {
        var canvas = new TestCanvas();
        var element = new DecorationElement
        {
            Before = () => new TextElement("head"),
            Content = new TextElement("a\nb\nc\nd"),
        };
        // 30 pt per page: 10 for the slot, 20 for two content lines -> two pages.
        var space = new Size(200, 30);

        Assert.Equal(SpacePlanType.PartialRender, element.Measure(canvas, space).Type);
        element.Draw(canvas, space);
        Assert.Equal(SpacePlanType.FullRender, element.Measure(canvas, space).Type);
        element.Draw(canvas, space);

        Assert.Equal(2, canvas.DrawnText.Count(t => t.Text == "head"));
        Assert.Equal(4, canvas.DrawnText.Count(t => t.Text is "a" or "b" or "c" or "d"));
    }

    [Fact]
    public void Wraps_when_slots_leave_no_room_for_content()
    {
        var canvas = new TestCanvas();
        var element = new DecorationElement
        {
            Before = () => new TextElement("before"),
            After = () => new TextElement("after"),
            Content = new TextElement("body"),
        };

        Assert.Equal(SpacePlanType.Wrap, element.Measure(canvas, new Size(200, 20)).Type);
    }

    [Fact]
    public void Fluent_decoration_renders_slots_across_pages()
    {
        var body = string.Join("\n", Enumerable.Range(1, 150).Select(i => $"row {i}"));
        var bytes = Document.Create(doc => doc
                .Page(page => page.Content(content => content.Decoration(decoration =>
                {
                    decoration.Before(before => before.Text("SECTION"));
                    decoration.Content(c => c.Text(body));
                }))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.True(pdf.PageCount >= 2);
        for (var i = 0; i < pdf.PageCount; i++)
            Assert.Contains("SECTION", pdf.ExtractText(i));
    }
}

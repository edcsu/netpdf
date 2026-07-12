using NetPdf.Fluent;
using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class InlinedTests
{
    // TestCanvas metrics: every character 5 pt wide, every line 10 pt tall.

    private static InlinedElement Element(params IElement[] items) => new() { Items = [.. items] };

    [Fact]
    public void Items_pack_into_rows_and_wrap_on_width()
    {
        var canvas = new TestCanvas();
        // Each item "xxxx" is 20 pt wide; width 50 fits two per row.
        var element = Element(
            new TextElement("xxxx"), new TextElement("xxxx"), new TextElement("xxxx"));

        var plan = element.Measure(canvas, new Size(50, 100));
        element.Draw(canvas, new Size(50, 100));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(20, plan.Size.Height);
        Assert.Equal([(0.0, 0.0), (20.0, 0.0), (0.0, 10.0)],
            canvas.DrawnText.Select(t => (t.X, t.Y)));
    }

    [Fact]
    public void Horizontal_spacing_separates_items()
    {
        var canvas = new TestCanvas();
        var element = Element(new TextElement("aa"), new TextElement("bb"));
        element.HorizontalSpacing = 5;

        element.Draw(canvas, new Size(100, 100));

        Assert.Equal([0.0, 15.0], canvas.DrawnText.Select(t => t.X));
    }

    [Fact]
    public void Rows_paginate_when_height_runs_out()
    {
        var canvas = new TestCanvas();
        // Three rows of one item each (width forces wrapping); height fits two rows.
        var element = Element(
            new TextElement("wide item"), new TextElement("wide item"), new TextElement("wide item"));

        var space = new Size(50, 20);
        var first = element.Measure(canvas, space);
        Assert.Equal(SpacePlanType.PartialRender, first.Type);
        element.Draw(canvas, space);

        var second = element.Measure(canvas, space);
        Assert.Equal(SpacePlanType.FullRender, second.Type);
        element.Draw(canvas, space);
        Assert.Equal(3, canvas.DrawnText.Count); // one 45 pt row per item: two on page 1, one on page 2
    }

    [Fact]
    public void Center_alignment_offsets_rows()
    {
        var canvas = new TestCanvas();
        var element = Element(new TextElement("aa")); // 10 pt wide row in 100 pt slot
        element.Alignment = HorizontalAlignment.Center;

        element.Draw(canvas, new Size(100, 100));

        Assert.Equal(45, Assert.Single(canvas.DrawnText).X);
    }

    [Fact]
    public void Bottom_alignment_offsets_short_items()
    {
        var canvas = new TestCanvas();
        // A 2-line item sets the row height to 20; the 1-line item bottom-aligns at y = 10.
        var element = Element(new TextElement("tall\nitem"), new TextElement("low"));
        element.VerticalAlignment = VerticalAlignment.Bottom;

        element.Draw(canvas, new Size(100, 100));

        var low = canvas.DrawnText.Single(t => t.Text == "low");
        Assert.Equal(10, low.Y);
    }

    [Fact]
    public void Fluent_inlined_renders_items()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page.Content(content => content.Inlined(inlined =>
                {
                    inlined.Spacing(4).AlignCenter();
                    inlined.Item().Text("alpha");
                    inlined.Item().Text("beta");
                    inlined.Item().Text("gamma");
                }))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        var text = pdf.ExtractText();
        Assert.Contains("alpha", text);
        Assert.Contains("beta", text);
        Assert.Contains("gamma", text);
    }
}

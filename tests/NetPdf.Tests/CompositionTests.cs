using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class CompositionTests
{
    [Fact]
    public void Column_stacks_children_and_sums_heights()
    {
        var canvas = new TestCanvas();
        var column = new ColumnElement { Items = { new TextElement("aaa"), new TextElement("bb") } };
        var plan = column.Measure(canvas, new Size(100, 100));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(20, plan.Size.Height);
        Assert.Equal(15, plan.Size.Width); // widest child

        column.Draw(canvas, new Size(100, 100));
        Assert.Equal(0, canvas.DrawnText[0].Y);
        Assert.Equal(10, canvas.DrawnText[1].Y);
    }

    [Fact]
    public void Column_applies_spacing_between_items()
    {
        var canvas = new TestCanvas();
        var column = new ColumnElement
        {
            Spacing = 5,
            Items = { new TextElement("a"), new TextElement("b") },
        };
        var plan = column.Measure(canvas, new Size(100, 100));
        Assert.Equal(25, plan.Size.Height);

        column.Draw(canvas, new Size(100, 100));
        Assert.Equal(15, canvas.DrawnText[1].Y);
    }

    [Fact]
    public void Column_defers_item_that_does_not_fit_to_the_next_page()
    {
        var canvas = new TestCanvas();
        var column = new ColumnElement
        {
            Items = { new TextElement("first"), new TextElement("second") },
        };
        // Only one 10 pt line fits.
        var plan = column.Measure(canvas, new Size(100, 15));
        Assert.Equal(SpacePlanType.PartialRender, plan.Type);
        Assert.Equal(10, plan.Size.Height);

        column.Draw(canvas, new Size(100, 15));
        var (text, _, _) = Assert.Single(canvas.DrawnText);
        Assert.Equal("first", text);

        // Next page: the remaining item renders.
        var next = column.Measure(canvas, new Size(100, 15));
        Assert.Equal(SpacePlanType.FullRender, next.Type);
        column.Draw(canvas, new Size(100, 15));
        Assert.Equal("second", canvas.DrawnText[1].Text);
    }

    [Fact]
    public void Column_paginates_items_across_pages_in_a_real_document()
    {
        var column = new ColumnElement
        {
            Spacing = 4,
            Items = { },
        };
        foreach (var i in Enumerable.Range(1, 120))
            column.Items.Add(new TextElement($"Item number {i}"));

        var bytes = PdfFile.Create().AddContent(column).ToBytes();
        using var pdf = PdfFile.Open(bytes);

        Assert.True(pdf.PageCount > 1);
        Assert.Contains("Item number 1", pdf.ExtractText(0));
        Assert.Contains("Item number 120", pdf.ExtractText(pdf.PageCount - 1));
    }

    [Fact]
    public void Row_splits_width_between_relative_items_by_weight()
    {
        var canvas = new TestCanvas();
        var left = new SpyElement();
        var right = new SpyElement();
        var row = new RowElement
        {
            Items =
            {
                new RowItem { Element = left, Size = 1 },
                new RowItem { Element = right, Size = 3 },
            },
        };
        row.Measure(canvas, new Size(100, 50));

        Assert.Equal(25, left.LastOfferedSpace.Width);
        Assert.Equal(75, right.LastOfferedSpace.Width);
    }

    [Fact]
    public void Row_constant_item_gets_exact_width()
    {
        var canvas = new TestCanvas();
        var fixedItem = new SpyElement();
        var flexItem = new SpyElement();
        var row = new RowElement
        {
            Spacing = 10,
            Items =
            {
                new RowItem { Element = fixedItem, Type = RowItemType.Constant, Size = 30 },
                new RowItem { Element = flexItem },
            },
        };
        row.Measure(canvas, new Size(100, 50));

        Assert.Equal(30, fixedItem.LastOfferedSpace.Width);
        Assert.Equal(60, flexItem.LastOfferedSpace.Width); // 100 - 30 constant - 10 spacing
    }

    [Fact]
    public void Row_draws_items_side_by_side()
    {
        var canvas = new TestCanvas();
        var row = new RowElement
        {
            Items =
            {
                new RowItem { Element = new TextElement("left") },
                new RowItem { Element = new TextElement("right") },
            },
        };
        row.Measure(canvas, new Size(100, 50));
        row.Draw(canvas, new Size(100, 50));

        Assert.Equal(0, canvas.DrawnText[0].X);
        Assert.Equal(50, canvas.DrawnText[1].X);
        Assert.Equal(0, canvas.DrawnText[1].Y);
    }

    [Fact]
    public void Row_height_is_the_tallest_item()
    {
        var canvas = new TestCanvas();
        var row = new RowElement
        {
            Items =
            {
                new RowItem { Element = new TextElement("short") },
                new RowItem { Element = new TextElement("one two three four five six seven") },
            },
        };
        // Second item wraps to several 10 pt lines in a 50 pt cell.
        var plan = row.Measure(canvas, new Size(100, 100));
        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.True(plan.Size.Height > 10);
    }

    [Fact]
    public void Row_finished_item_is_not_redrawn_on_the_next_page()
    {
        var canvas = new TestCanvas();
        var longText = string.Join(" ", Enumerable.Range(1, 30).Select(i => $"w{i}"));
        var row = new RowElement
        {
            Items =
            {
                new RowItem { Element = new TextElement("done") },
                new RowItem { Element = new TextElement(longText) },
            },
        };

        // First page: 20 pt tall, so the long item renders partially.
        var plan = row.Measure(canvas, new Size(100, 20));
        Assert.Equal(SpacePlanType.PartialRender, plan.Type);
        row.Draw(canvas, new Size(100, 20));
        Assert.Contains(canvas.DrawnText, d => d.Text == "done");

        // Second page: the finished left item must not appear again.
        canvas.DrawnText.Clear();
        row.Measure(canvas, new Size(100, 200));
        row.Draw(canvas, new Size(100, 200));
        Assert.DoesNotContain(canvas.DrawnText, d => d.Text == "done");
        Assert.NotEmpty(canvas.DrawnText);
    }

    [Fact]
    public void Layers_draws_all_layers_and_measures_by_the_primary()
    {
        var canvas = new TestCanvas();
        var layers = new LayersElement
        {
            Layers = { new TextElement("background"), new TextElement("foreground content") },
            PrimaryLayerIndex = 1,
        };
        var plan = layers.Measure(canvas, new Size(200, 100));
        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal("foreground content".Length * 5, plan.Size.Width);

        layers.Draw(canvas, new Size(200, 100));
        Assert.Equal(2, canvas.DrawnText.Count);
        Assert.Equal(0, canvas.DrawnText[0].X); // both drawn at the same origin
        Assert.Equal(0, canvas.DrawnText[1].X);
    }

    private sealed class SpyElement : IElement
    {
        public Size LastOfferedSpace { get; private set; }

        public SpacePlan Measure(ICanvas canvas, Size availableSpace)
        {
            LastOfferedSpace = availableSpace;
            return SpacePlan.FullRender(new Size(0, 10));
        }

        public void Draw(ICanvas canvas, Size availableSpace) { }
    }
}

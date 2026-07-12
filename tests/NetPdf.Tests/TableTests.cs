using NetPdf.Fluent;
using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class TableTests
{
    private static TableElement BuildTable(Action<TableDescriptor> configure)
    {
        TableElement table = null!;
        new ContainerDescriptor(e => table = (TableElement)e).Table(configure);
        return table;
    }

    [Fact]
    public void EmptyTable_MeasuresToZero()
    {
        var table = new TableElement();
        var plan = table.Measure(new TestCanvas(), new Size(200, 100));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(0, plan.Size.Width);
        Assert.Equal(0, plan.Size.Height);
    }

    [Fact]
    public void ColumnWidths_ConstantThenRelativeByWeight()
    {
        var canvas = new TestCanvas();
        var table = BuildTable(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.ConstantColumn(50);
                c.RelativeColumn(2);
                c.RelativeColumn();
            });
            t.Cell().Text("a");
            t.Cell().Text("b");
            t.Cell().Text("c");
        });

        table.Draw(canvas, new Size(200, 100));

        // Constant 50, remainder 150 split 2:1 -> 100 and 50; cells at x = 0, 50, 150.
        Assert.Equal([0.0, 50.0, 150.0], canvas.DrawnText.OrderBy(d => d.X).Select(d => d.X));
    }

    [Fact]
    public void AutoPlacement_FillsRowsLeftToRight()
    {
        var canvas = new TestCanvas();
        var table = BuildTable(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
            });
            t.Cell().Text("r1c1");
            t.Cell().Text("r1c2");
            t.Cell().Text("r2c1");
        });

        table.Draw(canvas, new Size(200, 100));

        var byText = canvas.DrawnText.ToDictionary(d => d.Text, d => (d.X, d.Y));
        Assert.Equal((0, 0), byText["r1c1"]);
        Assert.Equal((100, 0), byText["r1c2"]);
        Assert.Equal((0, 10), byText["r2c1"]);
    }

    [Fact]
    public void ExplicitPlacement_PositionsCellAndAutoCellsSkipIt()
    {
        var canvas = new TestCanvas();
        var table = BuildTable(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
            });
            t.Cell().Row(1).Column(2).Text("explicit");
            t.Cell().Text("auto"); // must land at row 1, column 1 — the free slot
        });

        table.Draw(canvas, new Size(200, 100));

        var byText = canvas.DrawnText.ToDictionary(d => d.Text, d => (d.X, d.Y));
        Assert.Equal((100, 0), byText["explicit"]);
        Assert.Equal((0, 0), byText["auto"]);
    }

    [Fact]
    public void ColumnSpan_WidensTheCell()
    {
        var canvas = new TestCanvas();
        var table = BuildTable(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
            });
            // 30 chars = 150pt: fits the 200pt spanned width in one line, not a 100pt column.
            t.Cell().ColumnSpan(2).Text(new string('x', 30));
        });

        var plan = table.Measure(canvas, new Size(200, 100));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(10, plan.Size.Height); // single line
    }

    [Fact]
    public void RowSpan_StretchesLastSpannedRowWhenNeeded()
    {
        var canvas = new TestCanvas();
        var table = BuildTable(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.ConstantColumn(100);
                c.ConstantColumn(100);
            });
            // Spanning cell needs 3 lines (30pt); the two spanned rows hold 10pt each -> last row stretches to 20pt.
            t.Cell().RowSpan(2).Text("word word word word word word word word word word");
            t.Cell().Text("r1");
            t.Cell().Row(2).Column(2).Text("r2");
        });

        var plan = table.Measure(canvas, new Size(200, 100));

        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(30, plan.Size.Height);

        table.Draw(canvas, new Size(200, 100));
        var r2 = canvas.DrawnText.Single(d => d.Text == "r2");
        Assert.Equal(10, r2.Y); // second row starts after the first row's 10pt
    }

    [Fact]
    public void Body_PaginatesBetweenRows()
    {
        var canvas = new TestCanvas();
        var table = BuildTable(t =>
        {
            t.ColumnsDefinition(c => c.RelativeColumn());
            for (var i = 1; i <= 5; i++)
                t.Cell().Text($"row{i}");
        });

        // 25pt fits two 10pt rows per page.
        var space = new Size(200, 25);
        var plan1 = table.Measure(canvas, space);
        Assert.Equal(SpacePlanType.PartialRender, plan1.Type);
        Assert.Equal(20, plan1.Size.Height);
        table.Draw(canvas, space);
        Assert.Equal(["row1", "row2"], canvas.DrawnText.Select(d => d.Text));

        canvas.DrawnText.Clear();
        table.Draw(canvas, space);
        Assert.Equal(["row3", "row4"], canvas.DrawnText.Select(d => d.Text));

        canvas.DrawnText.Clear();
        var plan3 = table.Measure(canvas, space);
        Assert.Equal(SpacePlanType.FullRender, plan3.Type);
        table.Draw(canvas, space);
        Assert.Equal(["row5"], canvas.DrawnText.Select(d => d.Text));
    }

    [Fact]
    public void RowSpan_NeverStraddlesPageBreak()
    {
        var canvas = new TestCanvas();
        var table = BuildTable(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.ConstantColumn(100);
                c.ConstantColumn(100);
            });
            t.Cell().Text("top"); // row 1 alone, 10pt
            t.Cell().Row(2).Column(1).RowSpan(2).Text("word word word word word word word word word word");
            t.Cell().Row(2).Column(2).Text("a");
            t.Cell().Row(3).Column(2).Text("b");
        });

        // 25pt: row 1 (10pt) fits; the spanned band of rows 2-3 (30pt) must defer entirely.
        var space = new Size(200, 25);
        table.Draw(canvas, space);
        Assert.Equal(["top"], canvas.DrawnText.Select(d => d.Text));

        canvas.DrawnText.Clear();
        table.Draw(canvas, new Size(200, 100));
        // Whole band renders together on the next page: 3 wrapped lines of the span + "a" + "b".
        Assert.Equal(5, canvas.DrawnText.Count);
        Assert.Contains(canvas.DrawnText, d => d.Text == "a" && d.Y == 0);
    }

    [Fact]
    public void BandTallerThanEmptyPage_ReportsWrap()
    {
        var canvas = new TestCanvas();
        var table = BuildTable(t =>
        {
            t.ColumnsDefinition(c => c.RelativeColumn());
            t.Cell().Text("word word word word word"); // wraps to multiple 10pt lines at 40pt width
        });

        var plan = table.Measure(canvas, new Size(40, 15));
        Assert.Equal(SpacePlanType.Wrap, plan.Type);
    }

    [Fact]
    public void OverlappingExplicitCells_Throw()
    {
        var table = BuildTable(t =>
        {
            t.ColumnsDefinition(c => c.RelativeColumn());
            t.Cell().Row(1).Column(1).Text("a");
            t.Cell().Row(1).Column(1).Text("b");
        });

        Assert.Throws<LayoutException>(() => table.Measure(new TestCanvas(), new Size(200, 100)));
    }

    [Fact]
    public void HeaderAndFooter_RepeatOnEveryPage()
    {
        var canvas = new TestCanvas();
        var table = BuildTable(t =>
        {
            t.ColumnsDefinition(c => c.RelativeColumn());
            t.Header(h => h.Cell().Text("HEAD"));
            t.Footer(f => f.Cell().Text("FOOT"));
            for (var i = 1; i <= 3; i++)
                t.Cell().Text($"row{i}");
        });

        // 45pt page: header 10 + footer 10 leaves 25 -> two body rows per page.
        var space = new Size(200, 45);
        var plan1 = table.Measure(canvas, space);
        Assert.Equal(SpacePlanType.PartialRender, plan1.Type);
        table.Draw(canvas, space);
        Assert.Equal(["HEAD", "row1", "row2", "FOOT"], canvas.DrawnText.Select(d => d.Text));
        Assert.Equal(0, canvas.DrawnText.Single(d => d.Text == "HEAD").Y);
        Assert.Equal(30, canvas.DrawnText.Single(d => d.Text == "FOOT").Y); // right below the body

        canvas.DrawnText.Clear();
        var plan2 = table.Measure(canvas, space);
        Assert.Equal(SpacePlanType.FullRender, plan2.Type);
        table.Draw(canvas, space);
        Assert.Equal(["HEAD", "row3", "FOOT"], canvas.DrawnText.Select(d => d.Text));
    }

    [Fact]
    public void Table_RendersInRealDocument()
    {
        var bytes = Document.Create(doc => doc.Page(page => page.Content(c => c.Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.ConstantColumn(120);
                cd.RelativeColumn();
            });
            t.Header(h =>
            {
                h.Cell().Text("Name");
                h.Cell().Text("Description");
            });
            for (var i = 1; i <= 80; i++)
            {
                t.Cell().Text($"Item {i}");
                t.Cell().Text($"Description of item {i} with a bit of extra text to wrap.");
            }
        })))).ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.True(pdf.PageCount >= 2); // 80 rows overflow one page
        Assert.Contains("Item 1", pdf.ExtractText(0));
        Assert.Contains("Name", pdf.ExtractText(1)); // header repeats on page 2
        Assert.Contains("Item 80", pdf.ExtractText(pdf.PageCount - 1));
    }
}

using NetPdf.Fluent;
using Xunit;
using NetPdf.Layout;
using NetPdf.Layout.Elements;

namespace NetPdf.Tests;

public class DebugAidTests
{
    [Fact]
    public void DebugArea_DrawsChildOutlineAndLabel()
    {
        var canvas = new TestCanvas();
        var element = new DebugAreaElement
        {
            Label = "box",
            Color = System.Drawing.Color.Red,
            Child = new TextElement("hello"),
        };

        element.Draw(canvas, new Size(200, 100));

        var rect = Assert.Single(canvas.DrawnRectangles);
        Assert.Null(rect.Fill);
        Assert.Equal(System.Drawing.Color.Red, rect.Stroke);
        Assert.Equal(25, rect.Width); // "hello" = 5 chars * 5pt in TestCanvas
        Assert.Equal(10, rect.Height);
        Assert.Contains(canvas.DrawnText, t => t.Text == "hello");
        Assert.Contains(canvas.DrawnText, t => t.Text == "box");
    }

    [Fact]
    public void DebugArea_WithoutLabel_DrawsOnlyOutline()
    {
        var canvas = new TestCanvas();
        var element = new DebugAreaElement { Child = new TextElement("hi") };

        element.Draw(canvas, new Size(200, 100));

        Assert.Single(canvas.DrawnRectangles);
        Assert.Single(canvas.DrawnText); // only the child's text, no label
    }

    [Fact]
    public void DebugArea_PicksStablePaletteColor_WhenNoColorGiven()
    {
        var canvas = new TestCanvas();
        var element = new DebugAreaElement { Label = "a", Child = new TextElement("x") };
        element.Draw(canvas, new Size(100, 100));

        Assert.NotNull(canvas.DrawnRectangles[0].Stroke);
    }

    [Fact]
    public void Fluent_Debug_ProducesValidPdf()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page
                    .Content(c => c.Debug("content-box").Text("Debug me"))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
        Assert.Contains("Debug me", pdf.ExtractText());
    }

    [Fact]
    public void Fluent_DebugOverlay_OutlinesAllSlots()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page
                    .DebugOverlay()
                    .Header(h => h.Text("Head"))
                    .Content(c => c.Text("Body"))
                    .Footer(f => f.Text("Foot"))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        var text = pdf.ExtractText();
        Assert.Contains("Body", text);
        Assert.Contains("header", text); // slot labels are drawn as text
        Assert.Contains("content", text);
        Assert.Contains("footer", text);

        var png = pdf.RenderPage(0);
        Assert.True(png.Length > 0);
    }
}

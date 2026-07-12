using NetPdf.Fluent;
using NetPdf.Layout;
using NetPdf.Layout.Elements;
using NetPdf.Layout.Text;
using Xunit;

namespace NetPdf.Tests;

public class BidiTests
{
    [Fact]
    public void LtrText_IsUnchanged()
    {
        Assert.Equal("hello world", BidiAlgorithm.ReorderVisual("hello world", ContentDirection.LeftToRight));
    }

    [Fact]
    public void PureRtlRun_IsReversed()
    {
        // Hebrew "shalom" in logical order comes out reversed for LTR glyph drawing.
        Assert.Equal("םולש", BidiAlgorithm.ReorderVisual("שלום", ContentDirection.RightToLeft));
    }

    [Fact]
    public void MixedText_KeepsLatinRunsForward()
    {
        // Logical: "abc אבג" with RTL base: Hebrew (reversed) first visually, then "abc".
        var visual = BidiAlgorithm.ReorderVisual("abc אבג", ContentDirection.RightToLeft);
        Assert.Equal("גבא abc", visual);
    }

    [Fact]
    public void NumbersInRtlText_StayLeftToRight()
    {
        var visual = BidiAlgorithm.ReorderVisual("אב 123", ContentDirection.RightToLeft);
        Assert.Equal("123 בא", visual);
    }

    [Fact]
    public void HasRtl_DetectsHebrewAndArabic()
    {
        Assert.True(BidiAlgorithm.HasRtl("שלום"));
        Assert.True(BidiAlgorithm.HasRtl("سلام"));
        Assert.False(BidiAlgorithm.HasRtl("hello 123"));
    }
}

public class ArabicShapingTests
{
    [Fact]
    public void NonArabicText_PassesThrough()
    {
        Assert.Equal("hello", ArabicShaper.Shape("hello"));
    }

    [Fact]
    public void Salam_ShapesToContextualForms()
    {
        // سلام: seen initial, lam-alef ligature final, meem final.
        var shaped = ArabicShaper.Shape("سلام");
        Assert.Equal("ﺳﻼﻡ", shaped);
    }

    [Fact]
    public void IsolatedLetter_UsesIsolatedForm()
    {
        Assert.Equal("ﺏ", ArabicShaper.Shape("ب"));
    }

    [Fact]
    public void RightJoiningLetter_BreaksTheJoin()
    {
        // دار: dal isolated (nothing joins to it from before), alef final? No —
        // dal is right-joining and nothing precedes it: isolated. Alef after dal:
        // dal does not join forward, so alef is isolated; reh joins to nothing: isolated.
        var shaped = ArabicShaper.Shape("دار");
        Assert.Equal("ﺩﺍﺭ", shaped);
    }

    [Fact]
    public void LamAlef_FormsLigature()
    {
        Assert.Equal("ﻻ", ArabicShaper.Shape("لا"));
    }
}

public class RtlLayoutTests
{
    [Fact]
    public void RtlText_AlignsRight()
    {
        var canvas = new TestCanvas();
        var element = new ContentDirectionElement
        {
            Direction = ContentDirection.RightToLeft,
            Child = new TextElement("שלום"),
        };

        element.Draw(canvas, new Size(200, 100));

        var text = Assert.Single(canvas.DrawnText);
        // 4 chars * 5pt wide in TestCanvas → right-aligned at x = 200 - 20.
        Assert.Equal(180, text.X);
        Assert.Equal("םולש", text.Text);
    }

    [Fact]
    public void RtlRow_MirrorsItemOrder()
    {
        var canvas = new TestCanvas();
        var row = new RowElement
        {
            Items =
            {
                new RowItem { Element = new TextElement("first") },
                new RowItem { Element = new TextElement("second") },
            },
        };
        var element = new ContentDirectionElement
        {
            Direction = ContentDirection.RightToLeft,
            Child = row,
        };

        element.Draw(canvas, new Size(200, 100));

        var first = canvas.DrawnText.Single(t => t.Text == "first");
        var second = canvas.DrawnText.Single(t => t.Text == "second");
        Assert.True(first.X > second.X, "in RTL the first item must land to the right of the second");
    }

    [Fact]
    public void LtrRow_KeepsItemOrder()
    {
        var canvas = new TestCanvas();
        var row = new RowElement
        {
            Items =
            {
                new RowItem { Element = new TextElement("first") },
                new RowItem { Element = new TextElement("second") },
            },
        };

        row.Draw(canvas, new Size(200, 100));

        var first = canvas.DrawnText.Single(t => t.Text == "first");
        var second = canvas.DrawnText.Single(t => t.Text == "second");
        Assert.True(first.X < second.X);
    }

    [Fact]
    public void Fluent_ContentFromRightToLeft_ProducesValidPdf()
    {
        var bytes = Document.Create(doc => doc
                .Page(page => page
                    .ContentFromRightToLeft()
                    .Content(c => c.Text("שלום עולם"))))
            .ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
        Assert.True(pdf.RenderPage(0).Length > 0);
    }
}

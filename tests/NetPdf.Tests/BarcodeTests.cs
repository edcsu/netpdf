using NetPdf.Creation;
using NetPdf.Fluent;
using NetPdf.Layout;
using NetPdf.Layout.Elements;
using Xunit;

namespace NetPdf.Tests;

public class BarcodeTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47];

    [Fact]
    public void QrCode_ProducesValidPngImageSource()
    {
        var source = BarcodeGenerator.GenerateQrCode("https://example.com");
        Assert.Equal(PngSignature, source.Data.Take(4));
    }

    [Fact]
    public void Barcode_Code128_ProducesValidPngImageSource()
    {
        var source = BarcodeGenerator.Generate("NETPDF-12345", BarcodeFormat.Code128, 256, 100);
        Assert.Equal(PngSignature, source.Data.Take(4));
    }

    [Fact]
    public void QrCode_IsDeterministic()
    {
        var a = BarcodeGenerator.GenerateQrCode("hello");
        var b = BarcodeGenerator.GenerateQrCode("hello");
        Assert.Equal(a.Data, b.Data);
    }

    [Fact]
    public void EmptyContent_Throws()
    {
        Assert.Throws<ArgumentException>(() => BarcodeGenerator.GenerateQrCode(""));
    }

    [Fact]
    public void Fluent_QrCode_PlacesAnImageElement()
    {
        IElement element = null!;
        new ContainerDescriptor(e => element = e).QrCode("hello");
        Assert.IsType<ImageElement>(element);
    }

    [Fact]
    public void CanvasElement_TakesFullSpaceAndDrawsCallback()
    {
        var canvas = new TestCanvas();
        var element = new CanvasElement((c, size) =>
            c.DrawLine(0, 0, size.Width, size.Height, System.Drawing.Color.Red, 2));

        var plan = element.Measure(canvas, new Size(120, 60));
        Assert.Equal(SpacePlanType.FullRender, plan.Type);
        Assert.Equal(120, plan.Size.Width);

        element.Draw(canvas, new Size(120, 60));
        var line = Assert.Single(canvas.DrawnLines);
        Assert.Equal(120, line.X2);
        Assert.Equal(60, line.Y2);
    }

    [Fact]
    public void QrCode_RendersInRealDocument()
    {
        var bytes = Document.Create(doc => doc.Page(page => page.Content(c => c.Column(col =>
        {
            col.Item().Text("Scan me:");
            col.Item().Width(120).QrCode("https://example.com");
        })))).ToBytes();

        using var pdf = PdfFile.Open(bytes);
        Assert.Equal(1, pdf.PageCount);
        Assert.NotEmpty(pdf.GetImages(0)); // the QR bitmap is embedded
    }
}

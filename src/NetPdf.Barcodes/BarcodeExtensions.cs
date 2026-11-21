using NetPdf.Creation;

// Same namespace as ContainerDescriptor, so callers need no extra "using".
namespace NetPdf.Fluent;

/// <summary>QR code and barcode placement for <see cref="ContainerDescriptor"/>, provided by NetPdfKit.Barcodes.</summary>
public static class ContainerDescriptorBarcodeExtensions
{
    /// <summary>Places a QR code in the slot, scaled to the available width like an image.</summary>
    public static void QrCode(this ContainerDescriptor container, string content, int pixelSize = 256)
    {
        ArgumentNullException.ThrowIfNull(container);
        container.Image(BarcodeGenerator.GenerateQrCode(content, pixelSize));
    }

    /// <summary>Places a barcode in the slot, scaled to the available width like an image.</summary>
    public static void Barcode(this ContainerDescriptor container, string content, BarcodeFormat format,
        int pixelWidth = 256, int pixelHeight = 256)
    {
        ArgumentNullException.ThrowIfNull(container);
        container.Image(BarcodeGenerator.Generate(content, format, pixelWidth, pixelHeight));
    }
}

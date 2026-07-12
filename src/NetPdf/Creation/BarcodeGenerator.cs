using NetPdf.Layout;

namespace NetPdf.Creation;

/// <summary>Barcode symbologies supported by <see cref="BarcodeGenerator"/>.</summary>
public enum BarcodeFormat
{
    /// <summary>QR Code 2D barcode.</summary>
    QrCode,
    /// <summary>Code 128 1D barcode.</summary>
    Code128,
    /// <summary>Code 39 1D barcode.</summary>
    Code39,
    /// <summary>EAN-8 1D barcode.</summary>
    Ean8,
    /// <summary>EAN-13 1D barcode.</summary>
    Ean13,
    /// <summary>UPC-A 1D barcode.</summary>
    UpcA,
    /// <summary>Interleaved 2 of 5 1D barcode.</summary>
    Itf,
    /// <summary>PDF417 2D barcode.</summary>
    Pdf417,
    /// <summary>Data Matrix 2D barcode.</summary>
    DataMatrix,
    /// <summary>Aztec 2D barcode.</summary>
    Aztec,
}

/// <summary>Renders barcodes and QR codes into <see cref="ImageSource"/> bitmaps via ZXing.</summary>
public static class BarcodeGenerator
{
    /// <summary>
    /// Encodes <paramref name="content"/> as a barcode bitmap of roughly the requested pixel
    /// size. The result is a plain image: reuse the instance to embed it only once, and size
    /// it on the page with the usual layout containers.
    /// </summary>
    public static ImageSource Generate(string content, BarcodeFormat format,
        int pixelWidth = 256, int pixelHeight = 256)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelWidth, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(pixelHeight, 16);

        var matrix = new ZXing.MultiFormatWriter().encode(content, Map(format), pixelWidth, pixelHeight);
        var pixels = new byte[matrix.Height, matrix.Width];
        for (var y = 0; y < matrix.Height; y++)
            for (var x = 0; x < matrix.Width; x++)
                pixels[y, x] = matrix[x, y] ? (byte)0 : (byte)255;

        return ImageSource.FromBytes(PngWriter.WriteGrayscale(pixels));
    }

    /// <summary>Encodes <paramref name="content"/> as a square QR code bitmap.</summary>
    public static ImageSource GenerateQrCode(string content, int pixelSize = 256) =>
        Generate(content, BarcodeFormat.QrCode, pixelSize, pixelSize);

    private static ZXing.BarcodeFormat Map(BarcodeFormat format) => format switch
    {
        BarcodeFormat.QrCode => ZXing.BarcodeFormat.QR_CODE,
        BarcodeFormat.Code128 => ZXing.BarcodeFormat.CODE_128,
        BarcodeFormat.Code39 => ZXing.BarcodeFormat.CODE_39,
        BarcodeFormat.Ean8 => ZXing.BarcodeFormat.EAN_8,
        BarcodeFormat.Ean13 => ZXing.BarcodeFormat.EAN_13,
        BarcodeFormat.UpcA => ZXing.BarcodeFormat.UPC_A,
        BarcodeFormat.Itf => ZXing.BarcodeFormat.ITF,
        BarcodeFormat.Pdf417 => ZXing.BarcodeFormat.PDF_417,
        BarcodeFormat.DataMatrix => ZXing.BarcodeFormat.DATA_MATRIX,
        BarcodeFormat.Aztec => ZXing.BarcodeFormat.AZTEC,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };
}

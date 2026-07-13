# Barcodes, QR codes, SVG, and canvas

QR codes, barcodes (via [ZXing.Net](https://github.com/micjahn/ZXing.Net)), and SVG (rasterized via [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia)) render through the same image pipeline as regular images. `Canvas` exposes raw drawing for charts and custom graphics.

```csharp
content.Column(column =>
{
    column.Item().Width(120).QrCode("https://example.com");
    column.Item().Width(240).Barcode("NETPDF-12345", BarcodeFormat.Code128);
    column.Item().Width(80).Svg("<svg …>…</svg>");             // rasterized at 2× by default
    column.Item().Height(100).Canvas((canvas, size) =>
        canvas.DrawLine(0, size.Height, size.Width, 0, Color.SteelBlue, 2));
});
```

Notes:

- SVG content is rasterized at 2× the layout size by default for crisp output.
- The `Canvas` callback receives the allocated size, so drawings scale with the layout slot.

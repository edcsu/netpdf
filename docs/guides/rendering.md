# Rendering pages to images

Render pages to PNG bytes via [PDFtoImage](https://github.com/sungaila/PDFtoImage) (PDFium):

```csharp
using var doc = PdfFile.Open("report.pdf");
byte[] png = doc.RenderPage(0, dpi: 150);
File.WriteAllBytes("page1.png", png);

var allPages = doc.RenderAllPages(dpi: 96);
```

- Page indexes are 0-based.
- `dpi` controls output resolution; 96 is screen resolution, 150–300 suits print previews and thumbnails at higher fidelity.
- Rendering is also handy for visual testing of [fluent layouts](fluent-layout.md) together with `DebugOverlay()`.

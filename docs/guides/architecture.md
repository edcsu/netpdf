# Architecture

NetPdf wraps battle-tested, permissively-licensed libraries behind a single coherent surface:

| Capability | Powered by | License |
|---|---|---|
| Create, merge, split, rotate, encrypt | [PDFsharp](https://github.com/empira/PDFsharp) | MIT |
| Text & image extraction | [PdfPig](https://github.com/UglyToad/PdfPig) | Apache-2.0 |
| Render pages to PNG | [PDFtoImage](https://github.com/sungaila/PDFtoImage) (PDFium) | MIT |
| Barcodes & QR codes | [ZXing.Net](https://github.com/micjahn/ZXing.Net) | Apache-2.0 |
| SVG rasterization | [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) (SkiaSharp) | MIT |

## The three entry points

- **`PdfFile`** (static) — the front door. `Create()` returns a `PdfBuilder` for coordinate-based drawing; `Open(path/stream/bytes, password)` returns a `PdfDocument`; `Merge(...)` combines files.
- **`PdfDocument`** — the unified document model. Reading (`ExtractText`, `GetImages`, `Metadata`), manipulation (`ExtractPages`, `Split`, `Overlay`, `Protect`, `Sign`, …), and rendering (`RenderPage`). It is **immutable**: every manipulation returns a new `PdfDocument`, and it implements `IDisposable`.
- **`NetPdf.Fluent.Document`** — the auto-layout engine. `Document.Create(doc => ...)` measures and paginates content with a two-pass `Measure`/`Draw` element protocol; custom elements implement `IElement`.

## Design notes

- Page indexes are 0-based everywhere; coordinates are PDF points (1/72 inch) from the top-left corner.
- Fonts are resolved from the operating system's font directories on all platforms.
- Operation ordering matters: manipulations → `Linearize()` → XMP / `AsPdfA()` → `Sign()` last. See [Managing PDFs](managing.md#operation-ordering).

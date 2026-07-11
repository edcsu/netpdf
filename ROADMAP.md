# Roadmap

Where NetPdf is headed. Phases are ordered by dependency: each builds on the previous. Effort tags: **S** (days), **M** (weeks), **L** (a month or more).

## Where NetPdf stands today

Already covered:

- Merge, split, extract, reorder, delete, and rotate pages (`PdfManipulator`)
- Metadata read/write (Title, Author, Subject, Keywords)
- Password protection (RC4-128) and opening protected files
- Text and image extraction (PdfPig)
- Page-to-PNG rendering (PDFtoImage)
- Absolute-coordinate drawing: text (with word wrap, font styles, OS font resolution), images, lines, rectangles

The biggest gap is a **layout engine**: NetPdf draws at explicit x/y with no measurement, reflow, or automatic pagination. Most planned features depend on one, so it anchors Phase 2.

## Phase 1 — Quick wins within the current architecture

Independent items that fit the existing wrapper design. Each bullet maps cleanly to one GitHub issue.

- [x] **AES-256 encryption + explicit decryption API** (S) — PDFsharp supports AES; replaces the deliberately weak RC4-128 default noted in the README
- [x] **Hyperlinks & bookmarks/outlines** (S) — link annotations and document outline via PDFsharp
- [x] **Overlays / underlays** (S) — stamp a page or PDF onto another (watermarks, letterheads) via form XObjects
- [x] **File attachments** (S) — embedded files
- [x] **More drawing primitives** (S) — ellipse, polygon, bézier, rounded rectangle; text alignment, underline/strikethrough, line spacing in `TextOptions`
- [x] **XMP metadata read/write** (M) — raw packet + Info-derived generator; written as an incremental update (PDFsharp regenerates `/Metadata` on save), so apply XMP last

## Phase 2 — Layout engine foundation

The big investment; everything in Phases 3–4 depends on it.

- [ ] **Element tree core** (L) — `IElement` with a two-pass `Measure(availableSpace) → SpacePlan` / `Draw(canvas)` protocol and a render loop that paginates automatically
- [ ] **Sizing & position containers** (M) — Width, Height, Padding, Alignment, AspectRatio, Extend, Shrink, Unconstrained, Offset
- [ ] **Composition elements** (M) — Column, Row (constant/relative sizing), Layers
- [ ] **Page slots** (M) — Header / Content / Footer repeated per page; page numbers via deferred text
- [ ] **Fluent document API** (M) — `Document.Create(c => c.Page(p => …))` alongside the existing absolute-coordinate `PageBuilder` (no breaking changes)

Backend decision: render through PDFsharp `XGraphics` initially, reusing `SystemFontResolver`; revisit SkiaSharp only if text shaping or SVG demands it.

## Phase 3 — Content flow & rich text

- [ ] **Page-break controls** (M) — PageBreak, ShowEntire, EnsureSpace, ShowOnce, SkipOnce, ShowIf, Repeat, StopPaging
- [ ] **Rich text** (L) — span-based text with mixed styles, style inheritance / DefaultTextStyle, paragraph style (line height, letter spacing), hyperlink spans
- [ ] **Decoration, Inlined, List** (M)

## Phase 4 — Table and advanced visuals

- [ ] **Table** (L) — column definitions, cell spans, header/footer rows repeating across pages
- [ ] **Styled containers** (M) — borders, backgrounds, rounded corners, shadows
- [ ] **Transforms** (M) — Rotate, Scale, ScaleToFit, Flip, Z-Index
- [ ] **SVG rendering** (M) — plus shared images and image size optimization
- [ ] **Barcodes / QR codes** (S) — via an optional integration (e.g. ZXing); charts documented as a canvas extension point rather than in-core

## Phase 5 — Compliance & long tail

- [ ] **Accessibility (tagged PDF) and PDF/A conformance** (L)
- [ ] **Digital signatures** (M)
- [ ] **Right-to-left content direction & advanced text shaping** (L)
- [ ] **Document linearization** (M)
- [ ] **Debug aids** (S) — DebugArea-style overlays and layout diagnostics; a live previewer app is out of scope — `RenderPage` to PNG is the supported inspection path

## Out of scope

- A companion/previewer application (use page rendering instead)
- Form fields (AcroForms) — may be reconsidered after Phase 4

# Fluent layout

The fluent layout API measures and paginates content automatically — no coordinates needed. Headers and footers repeat on every page, and `{number}`/`{total}` resolve to page numbers.

```csharp
using NetPdf.Fluent;

Document.Create(doc => doc
    .Page(page => page
        .Size(PageSizes.A4)
        .Margin(50)
        .Header(h => h.Text("ACME Quarterly Report"))
        .Content(c => c
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(8);
                column.Item().Text("Body text wraps and flows across pages automatically.");
                column.Item().Row(row =>
                {
                    row.ConstantItem(120).Text("Label");
                    row.RelativeItem().Text("Value that takes the remaining width");
                });
            }))
        .Footer(f => f.AlignCenter().PageNumber("Page {number} of {total}"))))
    .Save("report.pdf");
```

## Containers

Containers compose with chainable calls: `Padding`, `Width`/`Height` (plus min/max variants), `AlignCenter`/`AlignMiddle`/…, `AspectRatio`, `Extend`, `Shrink`, `Unconstrained`, and `Offset`. Custom `IElement` implementations plug in via `.Element(...)`.

## Content flow

Flow is controlled per block:

- `ShowEntire` keeps a block on one page
- `EnsureSpace(pt)` requires a minimum height before starting
- `PageBreak()` forces a new page
- `StopPaging` truncates to the current page
- `ShowOnce` / `SkipOnce` / `ShowIf(bool)` / `Repeat(...)` control repeated slots
- `DefaultTextStyle(style)` cascades text styling to everything inside

## Rich text and lists

Rich text mixes styles, hyperlinks, and paragraph settings inside one flowing block, and `List`/`Inlined`/`Decoration` cover common structures:

```csharp
content.Column(column =>
{
    column.Item().Text(text =>
    {
        text.LineHeight(1.4);
        text.Span("NetPdf ").Bold();
        text.Span("builds rich paragraphs — ");
        text.Hyperlink("learn more", "https://example.com");
        text.Span(".");
    });
    column.Item().List(list =>
    {
        list.Ordered().Spacing(4);
        list.Item().Text("First point");
        list.Item().Text("Second point");
    });
});
```

## Tables

Tables have fixed column widths, cell spans, and header/footer rows that repeat on every page; a spanned cell never splits across a page break:

```csharp
content.Table(table =>
{
    table.ColumnsDefinition(columns =>
    {
        columns.ConstantColumn(120);
        columns.RelativeColumn();
    });
    table.Header(header =>
    {
        header.Cell().Text("Name");
        header.Cell().Text("Description");
    });
    table.Cell().Text("Item 1");
    table.Cell().Text("First item");
    table.Cell().Row(2).Column(1).RowSpan(2).Text("Spans two rows");
});
```

## Visual styling and transforms

Visual styling and transforms chain onto any container: `Background(color, cornerRadius)`, `Border(...)`, `Shadow(...)` (blur is approximated — PDF has no native blur), `Rotate(degrees)`, `Scale(x, y)`, `FlipHorizontal`/`FlipVertical`/`FlipOver`, and `ScaleToFit()` which shrinks content until it fits its slot. Layers accept a `zIndex` to control stacking order.

## Debugging layouts

Outline any element, or all page slots, then [render to PNG](rendering.md) to inspect:

```csharp
Document.Create(doc => doc
    .Page(page => page
        .DebugOverlay()                       // outlines header/content/footer
        .Content(c => c.Debug("body").Text("…"))));
```

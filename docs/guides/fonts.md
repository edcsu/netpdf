# Fonts

NetPdf looks for a font in this order:

1. **Fonts you register** with `NetPdfFonts`.
2. **Fonts installed on the machine.** It scans the usual font folders on Windows, macOS and Linux. If the family you asked for is missing, it tries Arial, Helvetica, Liberation Sans, DejaVu Sans and Verdana.
3. **Bundled Liberation Sans.** It ships inside the package, so this step always works.

Because of step 3, PDFs are built even in a container with no fonts installed. You don't need to install a font package on Linux.

The default font is Arial, 12 pt. On a machine without Arial, text falls back to Liberation Sans. It looks almost the same, because Liberation Sans has the same character widths as Arial.

## Use your own font

Register it once at startup, before you build any PDF:

```csharp
using NetPdf;

NetPdfFonts.RegisterFile("Brand Sans", "fonts/BrandSans-Regular.ttf");
NetPdfFonts.RegisterFile("Brand Sans", "fonts/BrandSans-Bold.ttf", bold: true);
NetPdfFonts.RegisterFile("Brand Sans", "fonts/BrandSans-Italic.ttf", italic: true);
```

Then use the family name in a text span:

```csharp
t.Span("Hello").FontFamily("Brand Sans");
t.Span("Important").FontFamily("Brand Sans").Bold();
```

If you load fonts from somewhere else (an embedded resource, a database), pass the bytes:

```csharp
NetPdfFonts.Register("Brand Sans", bytes, bold: false, italic: false);
```

## Good to know

- Use `.ttf` or `.otf` files.
- If a style is missing (say, you only registered the regular face), bold and italic text uses the regular face.
- Check the font's license before you ship it inside your app.
- The bundled Liberation Sans is licensed under the SIL Open Font License. The license file is included in the package.
- Rendering Arabic needs a font with presentation-form glyphs. See [Compliance](compliance.md).

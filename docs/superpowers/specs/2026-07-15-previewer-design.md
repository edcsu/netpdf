# NetPdf Live Previewer — Design

Date: 2026-07-15. Status: approved.

## Purpose

A QuestPDF-Companion-style live hot-reload previewer for NetPdf. A developer edits
document code in their IDE, runs their app under `dotnet watch run`, and a desktop
window re-renders the generated PDF on every rebuild. Revises the previous ROADMAP
"out of scope" decision.

## Components

### NetPdf.Previewer (client NuGet, `NetPdfKit.Previewer`)

- `DocumentBuilder.ShowInPreviewer(int port = 12500)` extension method.
- Generates the PDF with `ToBytes()` and POSTs it to `http://localhost:{port}/document`.
- If the previewer app is not reachable, attempts to auto-launch the
  `netpdf-previewer` global tool; if not installed, throws with an
  install hint (`dotnet tool install -g NetPdfKit.Previewer.App`).
- If document generation throws (e.g. layout overflow), the exception text is
  POSTed to `/error` so the failure appears in the previewer window; the
  exception is rethrown.
- Multi-targets net8.0/net9.0/net10.0 like the library.

### NetPdf.Previewer.App (Avalonia desktop app, dotnet tool `netpdf-previewer`)

- Hosts a localhost `HttpListener`: `GET /ping` → `netpdf-previewer`,
  `POST /document` (raw PDF bytes), `POST /error` (UTF-8 text). Clients send
  `X-NetPdf-Previewer-Version: 1`.
- Renders pages to PNG via the library's existing PDFium path
  (`PdfFile.RenderPage(pageIndex, dpi)`).
- UI (v1): continuous-scroll page view with zoom in/out and fit-to-width;
  thumbnail sidebar for page jumps; status bar with connection state and
  last-update timestamp; error panel for pushed generation exceptions;
  "Save PDF…" button writing the last received bytes.
- Packaged as a dotnet global tool (`PackAsTool`, command `netpdf-previewer`),
  single TFM net10.0.

## Protocol

Plain HTTP on localhost with raw bytes in the body — no serialization framework,
trivially debuggable. Version header reserved for future compatibility.

## Testing

Unit tests in the existing test project for the client (payloads, ping, retry/launch
behavior against a stub listener), the server receive pipeline, and the view model
render pipeline. The Avalonia shell is verified manually end-to-end.

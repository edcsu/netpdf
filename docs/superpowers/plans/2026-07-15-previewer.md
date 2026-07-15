# NetPdf Live Previewer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

## Context

NetPdf's ROADMAP lists "a previewer application" as out of scope, but the user wants one: a QuestPDF-Companion-style **live hot-reload previewer**. A developer edits document code, runs their app under `dotnet watch run`, and each rebuild pushes the freshly generated PDF to a desktop window that re-renders it. Approved design:

- **Push model**: `DocumentBuilder.ShowInPreviewer(port = 12500)` in a new client package posts PDF bytes over localhost HTTP; generation errors are pushed too so they show in the window.
- **Avalonia** desktop app (macOS/Windows/Linux), packaged as a **dotnet global tool** `netpdf-previewer`; client auto-launches it when not reachable.
- **V1 UI**: continuous-scroll page view with zoom/fit-to-width, thumbnail sidebar, connection/error status, Save PDF button.
- Rendering reuses NetPdf's existing PDFium path (`PdfFile.RenderPage(pageIndex, dpi)` at `src/NetPdf/PdfFile.cs:266`).

**Goal:** Ship `NetPdf.Previewer` (client NuGet) + `NetPdf.Previewer.App` (Avalonia dotnet tool) providing live PDF preview with hot reload.

**Architecture:** Client extension method generates the PDF via existing `DocumentBuilder.ToBytes()` and POSTs it to a tiny `HttpListener` embedded in the Avalonia app; the app renders pages with `PdfFile.RenderPage` and shows them in an MVVM UI. Plain HTTP on localhost, raw PDF bytes in the body — no serialization framework.

**Tech Stack:** .NET (net8.0 for the app; net8/9/10 for the client like the library), Avalonia 11 + CommunityToolkit.Mvvm, `System.Net.HttpListener`, xUnit (existing test project).

## Global Constraints

- Repo conventions: `Directory.Build.props` sets `TargetFrameworks` net8.0;net9.0;net10.0 — the Avalonia app must override to single `net10.0` (exe projects can't multitarget usefully as a tool with Avalonia); client package keeps all three.
- Library package id is `NetPdfKit`; new packages: `NetPdfKit.Previewer` (client) and `NetPdfKit.Previewer.App` (tool, `ToolCommandName=netpdf-previewer`).
- No breaking changes to existing public API.
- Default port **12500**; endpoints `GET /ping`, `POST /document` (body = PDF bytes), `POST /error` (body = UTF-8 text). Header `X-NetPdf-Previewer-Version: 1` on all client requests.
- Follow existing code style (file-scoped namespaces, XML doc comments on public members, sealed classes).

## File Structure

```
src/NetPdf.Previewer/
  NetPdf.Previewer.csproj          # client NuGet, refs NetPdf project
  PreviewerClient.cs               # internal: HTTP push, ping, app launch/retry
  PreviewerExtensions.cs           # public ShowInPreviewer() on DocumentBuilder
src/NetPdf.Previewer.App/
  NetPdf.Previewer.App.csproj      # Avalonia exe, PackAsTool, refs NetPdf project
  Program.cs                       # Avalonia bootstrap, --port arg
  App.axaml / App.axaml.cs
  PreviewServer.cs                 # HttpListener host raising DocumentReceived/ErrorReceived
  ViewModels/MainViewModel.cs      # pages, thumbnails, zoom, status, save
  Views/MainWindow.axaml(.cs)      # UI layout
tests/NetPdf.Tests/Previewer/
  PreviewerClientTests.cs          # client against stub HttpListener
  PreviewServerTests.cs            # server receive pipeline
```

Solution: add both projects to `NetPdf.sln`. Docs: README previewer section; ROADMAP out-of-scope note updated.

---

### Task 1: PreviewServer (app-side HTTP listener)

**Files:**
- Create: `src/NetPdf.Previewer.App/NetPdf.Previewer.App.csproj` (project only; Avalonia UI comes in Task 4)
- Create: `src/NetPdf.Previewer.App/PreviewServer.cs`
- Test: `tests/NetPdf.Tests/Previewer/PreviewServerTests.cs`
- Modify: `NetPdf.sln` (add project), `tests/NetPdf.Tests/NetPdf.Tests.csproj` (add project reference)

**Interfaces:**
- Produces: `sealed class PreviewServer(int port) : IDisposable` with `void Start()`, `event Action<byte[]>? DocumentReceived`, `event Action<string>? ErrorReceived`, `int Port { get; }`. Handles `GET /ping` → 200 `"netpdf-previewer"`, `POST /document` → 200 after raising `DocumentReceived(bodyBytes)`, `POST /error` → 200 after raising `ErrorReceived(bodyText)`, anything else → 404.

- [ ] Create the csproj: `<OutputType>Exe</OutputType>`, `<TargetFramework>net10.0</TargetFramework>` (overrides Directory.Build.props plural), `<IsPackable>false</IsPackable>` for now, ProjectReference to `../NetPdf/NetPdf.csproj`. Add to solution with `dotnet sln add`.
- [ ] Write failing tests in `PreviewServerTests.cs`: start server on a free port (port 0 → pick via `TcpListener` trick or a fixed high test port), then with `HttpClient`: (a) `GET /ping` returns 200 and body `netpdf-previewer`; (b) `POST /document` with byte payload raises `DocumentReceived` with identical bytes; (c) `POST /error` with text raises `ErrorReceived`; (d) `GET /bogus` returns 404.
- [ ] Run `dotnet test --filter PreviewServerTests` — expect compile failure/red.
- [ ] Implement `PreviewServer` using `HttpListener` (`http://127.0.0.1:{port}/`), background `Task` accept loop, copy request body to `MemoryStream`, raise events, always respond 200/404, swallow `ObjectDisposedException`/`HttpListenerException` on dispose.
- [ ] Run tests — green.
- [ ] Commit `feat(previewer): add PreviewServer HTTP listener`.

### Task 2: Previewer client package

**Files:**
- Create: `src/NetPdf.Previewer/NetPdf.Previewer.csproj` (PackageId `NetPdfKit.Previewer`, ProjectReference to NetPdf, inherits multi-TFMs)
- Create: `src/NetPdf.Previewer/PreviewerClient.cs`
- Create: `src/NetPdf.Previewer/PreviewerExtensions.cs`
- Test: `tests/NetPdf.Tests/Previewer/PreviewerClientTests.cs`
- Modify: `NetPdf.sln`, `tests/NetPdf.Tests/NetPdf.Tests.csproj`

**Interfaces:**
- Consumes: `DocumentBuilder.ToBytes()`; the Task 1 endpoint contract.
- Produces: `public static class PreviewerExtensions { public static void ShowInPreviewer(this DocumentBuilder builder, int port = 12500) }`; `internal sealed class PreviewerClient(int port)` with `bool Ping()`, `void SendDocument(byte[] pdf)`, `void SendError(string message)`, `bool TryLaunchApp()` (starts `netpdf-previewer --port {port}` via `Process.Start`, returns false if the tool isn't installed), and `void EnsureRunning()` (ping → launch → poll ping up to ~10 s → throw `InvalidOperationException` with the `dotnet tool install -g NetPdfKit.Previewer` hint).
- Behavior of `ShowInPreviewer`: `EnsureRunning()`; then try `builder.ToBytes()` — on success `SendDocument`, on exception `SendError(ex.ToString())` and rethrow.

- [ ] Create csproj + add to solution and test project references.
- [ ] Write failing tests: run a stub `HttpListener` in the test recording method/path/headers/body, then assert (a) `SendDocument` POSTs bytes to `/document` with `X-NetPdf-Previewer-Version: 1`; (b) `SendError` POSTs text to `/error`; (c) `Ping()` true when stub answers `netpdf-previewer`, false when nothing listens; (d) `ShowInPreviewer` on a document that throws during compose sends `/error` and rethrows; (e) `EnsureRunning` with no listener and failed launch throws with install hint in message.
- [ ] Red run, then implement with a shared static `HttpClient` (short timeout ~2 s for ping, longer for document).
- [ ] Green run; commit `feat(previewer): add NetPdfKit.Previewer client with ShowInPreviewer`.

### Task 3: MainViewModel (render pipeline + UI state)

**Files:**
- Create: `src/NetPdf.Previewer.App/ViewModels/MainViewModel.cs`
- Test: `tests/NetPdf.Tests/Previewer/MainViewModelTests.cs`

**Interfaces:**
- Consumes: `PdfFile.FromBytes/ctor` + `PageCount` + `RenderPage(int pageIndex, int dpi)` (src/NetPdf/PdfFile.cs), `PreviewServer` events.
- Produces: `public sealed partial class MainViewModel : ObservableObject` (CommunityToolkit.Mvvm) with `ObservableCollection<PageImage> Pages` (`PageImage`: page number + PNG bytes for main view and thumbnail), `double Zoom` (+ `ZoomIn/ZoomOut/FitToWidth(viewportWidth)` commands), `string Status`, `string? ErrorText`, `bool HasDocument`, `void LoadDocument(byte[] pdf)` (renders all pages at `(int)(96 * Zoom)` dpi, thumbnails at 24 dpi, clears error, updates status with timestamp), `void ShowError(string message)`, `SaveCommand(string path)` writing the last received bytes.

- [ ] Add `CommunityToolkit.Mvvm` package to the app project.
- [ ] Failing tests: `LoadDocument` with a small PDF generated in-test via `Document.Create(...).ToBytes()` populates `Pages` with `PageCount` entries and non-empty PNGs; `ShowError` sets `ErrorText` and keeps previous pages; `Save` writes bytes identical to input; zoom commands clamp within 0.25–4.0 and trigger re-render.
- [ ] Red, implement, green.
- [ ] Commit `feat(previewer): add MainViewModel render pipeline`.

### Task 4: Avalonia UI + tool packaging

**Files:**
- Create: `src/NetPdf.Previewer.App/Program.cs`, `App.axaml(.cs)`, `Views/MainWindow.axaml(.cs)`
- Modify: `src/NetPdf.Previewer.App/NetPdf.Previewer.App.csproj` (Avalonia packages, `PackAsTool=true`, `ToolCommandName=netpdf-previewer`, `PackageId=NetPdfKit.Previewer.App`, `IsPackable=true`)

**Interfaces:**
- Consumes: `PreviewServer`, `MainViewModel`.

- [ ] Add Avalonia 11 packages (`Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`).
- [ ] `Program.cs`: parse `--port` (default 12500), classic Avalonia `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`.
- [ ] `MainWindow.axaml`: DockPanel — top toolbar (zoom −/+, %, Fit Width, Save PDF… via `StorageProvider.SaveFilePickerAsync`), left `ListBox` of thumbnails (click scrolls main view via selected index binding), center `ScrollViewer` with vertical `ItemsControl` of page images, bottom status bar (`Status` + `ErrorText` in red when set). Wire `PreviewServer.DocumentReceived/ErrorReceived` to the VM through `Dispatcher.UIThread.Post`.
- [ ] Manual verification (see Verification section) — window opens, shows "Waiting for document…".
- [ ] Set tool-packaging properties; `dotnet pack src/NetPdf.Previewer.App` succeeds.
- [ ] Commit `feat(previewer): Avalonia previewer app packaged as netpdf-previewer tool`.

### Task 5: Docs + roadmap

**Files:**
- Modify: `README.md` (new "Live previewer" section: install tool, `ShowInPreviewer()` + `dotnet watch` workflow, code sample), `ROADMAP.md` (move previewer out of "Out of scope"; note it shipped)

- [ ] Write docs, commit `docs: document live previewer`.

## Verification

1. `dotnet build NetPdf.sln` and `dotnet test` — all green on all TFMs.
2. End-to-end: `dotnet run --project src/NetPdf.Previewer.App` in one terminal; in another, a scratch console app (in scratchpad) with `Document.Create(...).ShowInPreviewer()` under `dotnet watch run`; edit the document text, save, confirm the window re-renders. Test zoom, thumbnails, Save PDF, and an intentional exception showing in the error panel.
3. `dotnet pack` both new projects; `dotnet tool install -g --add-source ./artifacts NetPdfKit.Previewer.App` and confirm `netpdf-previewer` launches and auto-launch from the client works.

## Post-approval housekeeping

On execution start (outside plan mode): write the approved design spec to `docs/superpowers/specs/2026-07-15-previewer-design.md`, copy this plan to `docs/superpowers/plans/2026-07-15-previewer.md`, and commit both. Work on a feature branch `feature/previewer` off `develop` (repo uses develop→main flow).

# Contributing to NetPdf

Thanks for your interest in contributing! This guide covers everything you need to build, test, and submit changes.

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) — the library multi-targets `net8.0`, `net9.0`, and `net10.0`, so the newest SDK builds all three.
- On Linux, install fonts used by the tests: `sudo apt-get install fonts-liberation fonts-dejavu-core`.

## Build and test

```sh
dotnet build -c Release
dotnet test -c Release
```

CI runs the same commands on Ubuntu, Windows, and macOS — your PR must be green on all three.

## Conventions

- **Warnings are errors** (`TreatWarningsAsErrors`) and nullable reference types are enabled solution-wide.
- **XML doc comments are required on all public APIs** — the build fails without them (`GenerateDocumentationFile` + warnings-as-errors).
- **Tests mirror features**: one `*Tests.cs` file per feature area in `tests/NetPdf.Tests` (e.g. `TableTests.cs`, `SignatureTests.cs`). Add or extend the matching file for your change.
- Manipulation APIs on `PdfDocument` are immutable — new methods should return a new document, never mutate the receiver.

## Documentation

Guides live in `docs/guides/` and the API reference is generated from XML comments by [DocFX](https://dotnet.github.io/docfx/). Preview locally:

```sh
dotnet tool update -g docfx
docfx docs/docfx.json --serve
```

Then open http://localhost:8080. If you change public API behavior, update the matching guide page and README sample.

## Pull request flow

1. Fork and branch from `main`.
2. Make your change with tests and doc updates.
3. Ensure `dotnet build` and `dotnet test` pass locally.
4. Open a PR — CI must pass on all three operating systems before review.

## Releasing

Maintainers: see [RELEASING.md](RELEASING.md).

# Releasing NetPdfKit

Releases are fully automated: pushing a version tag builds, packs, publishes to NuGet, and creates a GitHub Release. No API keys are stored — publishing uses [NuGet trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) via GitHub OIDC.

## Packages

One tag publishes the whole library family at the same version, in this order: `NetPdfKit.Core`, then `NetPdfKit.Svg`, `NetPdfKit.Barcodes`, `NetPdfKit.Rendering`, then the `NetPdfKit` meta-package that depends on them all. The Previewer packages go out through `release.yml`. A new package id needs a trusted-publishing policy on nuget.org before its first push.

## Versioning

- The package version comes from the git tag: tag `v1.2.3` publishes package version `1.2.3` (CI passes `-p:Version=${GITHUB_REF_NAME#v}` to `dotnet pack`).
- The `1.0.0` in `src/NetPdf.Packaging.props` is only a local-build fallback.
- Follow [SemVer](https://semver.org/): patch for fixes, minor for backwards-compatible features, major for breaking changes.

## Release steps

1. Ensure `main` is green (CI matrix on Ubuntu/Windows/macOS).
2. Update `ROADMAP.md` and any docs if the release changes them.
3. Tag and push:

   ```sh
   git tag v1.2.3
   git push origin v1.2.3
   ```

That's it. Two workflows react to the tag, in parallel:

**`.github/workflows/ci.yml`** — publishes the NuGet package:

1. Re-runs the full build/test matrix.
2. Packs `src/NetPdf` with the tag-derived version (produces `.nupkg` + `.snupkg` symbols).
3. Authenticates to nuget.org via OIDC (`NuGet/login@v1`, user `edcsu`) and pushes with `--skip-duplicate`.

**`.github/workflows/release.yml`** — assembles the GitHub Release:

1. Packs `NetPdfKit` (for attachment).
2. Builds the DocFX docs site and zips it (`docs-site.zip`).
3. Publishes the previewer app as self-contained, single-file binaries for `win-x64`, `linux-x64`, `osx-arm64`, and `osx-x64` (no .NET runtime required to run them), zipped per platform.
4. Creates a single GitHub Release with auto-generated notes and all of the above attached: the `.nupkg`/`.snupkg`, `docs-site.zip`, and the four `netpdf-previewer-<rid>.zip` files.

Users can grab a previewer binary from the Release, unzip it, and run the `netpdf-previewer` executable directly — or still install it as a global tool with `dotnet tool install -g NetPdfKit.Previewer.App`.

> Previewer binaries are also built (as downloadable workflow artifacts) on every PR and `main` push that touches the previewer or `src/`, via `.github/workflows/previewer-assets.yml`. That workflow never touches NuGet — publishing to nuget.org happens **only** on a `v*` tag push, or on a manual **Release** run with `dry_run` off and `publish_release` on.

## If something goes wrong

- **Tests fail on the tag build**: nothing was published. Fix on `main`, delete the tag (`git tag -d v1.2.3 && git push origin :refs/tags/v1.2.3`), and re-tag.
- **Push partially succeeded / job needs a re-run**: just re-run the workflow — `--skip-duplicate` makes the NuGet push idempotent.
- **A bad version reached nuget.org**: packages cannot be deleted, only [unlisted](https://learn.microsoft.com/nuget/nuget-org/policies/deleting-packages). Unlist it on nuget.org and ship a fixed patch version.

## Documentation site

The docs site (DocFX → GitHub Pages) deploys automatically via `.github/workflows/docs.yml` on pushes to `main` that touch `docs/`, `src/`, or `README.md`.

One-time setup: in the repo, go to **Settings → Pages** and set **Source** to **GitHub Actions**. The site is served at https://edcsu.github.io/netpdf/. You can also trigger a deploy manually from the Actions tab (`workflow_dispatch`).

## Releasing from the Release workflow (manual run)

You can also run **Release** by hand: set `dry_run` off, `publish_release` on, and `tag` to something like `v1.2.3`. It builds everything, creates the GitHub Release and the tag, and pushes the package to NuGet (the `nuget` job).

- A tag made this way does not start the tag-triggered CI run (GitHub does not run workflows from tags created with the built-in token). That is why `release.yml` pushes to NuGet itself in this case.
- The `tag` must start with `v`. Otherwise the `nuget` job stops instead of pushing a placeholder version.
- NuGet trusted publishing is tied to a workflow file. On nuget.org, add a trusted publishing policy for `release.yml` (owner `edcsu`, repo `netpdf`), next to the existing one for `ci.yml`. Without it, the NuGet login step fails.

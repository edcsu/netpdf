# Releasing NetPdfKit

Releases are fully automated: pushing a version tag builds, packs, publishes to NuGet, and creates a GitHub Release. No API keys are stored — publishing uses [NuGet trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) via GitHub OIDC.

## Versioning

- The package version comes from the git tag: tag `v1.2.3` publishes package version `1.2.3` (CI passes `-p:Version=${GITHUB_REF_NAME#v}` to `dotnet pack`).
- The `0.1.0` in `src/NetPdf/NetPdf.csproj` is only a local-build fallback.
- Follow [SemVer](https://semver.org/): patch for fixes, minor for backwards-compatible features, major for breaking changes.

## Release steps

1. Ensure `main` is green (CI matrix on Ubuntu/Windows/macOS).
2. Update `ROADMAP.md` and any docs if the release changes them.
3. Tag and push:

   ```sh
   git tag v1.2.3
   git push origin v1.2.3
   ```

That's it. The `publish` job in `.github/workflows/ci.yml` then:

1. Re-runs the full build/test matrix.
2. Packs `src/NetPdf` with the tag-derived version (produces `.nupkg` + `.snupkg` symbols).
3. Authenticates to nuget.org via OIDC (`NuGet/login@v1`, user `edcsu`) and pushes with `--skip-duplicate`.
4. Creates a GitHub Release with auto-generated notes and the `.nupkg` attached.

## If something goes wrong

- **Tests fail on the tag build**: nothing was published. Fix on `main`, delete the tag (`git tag -d v1.2.3 && git push origin :refs/tags/v1.2.3`), and re-tag.
- **Push partially succeeded / job needs a re-run**: just re-run the workflow — `--skip-duplicate` makes the NuGet push idempotent.
- **A bad version reached nuget.org**: packages cannot be deleted, only [unlisted](https://learn.microsoft.com/nuget/nuget-org/policies/deleting-packages). Unlist it on nuget.org and ship a fixed patch version.

## Documentation site

The docs site (DocFX → GitHub Pages) deploys automatically via `.github/workflows/docs.yml` on pushes to `main` that touch `docs/`, `src/`, or `README.md`.

One-time setup: in the repo, go to **Settings → Pages** and set **Source** to **GitHub Actions**. The site is served at https://edcsu.github.io/netpdf/. You can also trigger a deploy manually from the Actions tab (`workflow_dispatch`).

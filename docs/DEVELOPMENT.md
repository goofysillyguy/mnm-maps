# Development and releases

## Build environment

Use Windows x64 with the .NET 10 SDK. `global.json` accepts installed .NET 10 feature bands. The application is WPF with a Windows Forms tray icon, and has no third party NuGet dependencies.

```powershell
dotnet build .\src\MnMMaps.App -c Release
dotnet run --project .\src\MnMMaps.App -c Release
```

## Source layout

| Path | Purpose |
| --- | --- |
| `src/MnMMaps.App/MainWindow.*` | Window layout, selection, pins, and interaction |
| `src/MnMMaps.App/MapSurface.cs` | Raster map rendering, zoom, pan, and pin coordinates |
| `src/MnMMaps.App/MapImages.cs` | Image validation, wiki image downloads, and cache reads |
| `src/MnMMaps.App/Models.cs` | Catalog models and this app's local settings |
| `src/MnMMaps.App/OverlayControls.cs` | Shortcuts, tray icon, and styles applied to our own window |
| `src/MnMMaps.App/Data/maps.json` | Embedded map catalog, original sources, and discovery records |
| `scripts/publish-maps.ps1` | Self-contained Windows x64 EXE |
| `scripts/package-release.ps1` | Portable ZIP and SHA-256 manifest |
| `scripts/check-package.ps1` | Isolated packaged application check |
| `scripts/update-map-catalog.py` | Optional discovery and validation of public wiki image links |

## Build a distribution

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-release.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-package.ps1
```

The package outputs are:

```text
artifacts/release/MnMMaps.exe
artifacts/release/MnMMaps-win-x64.zip
artifacts/release/SHA256SUMS.txt
```

The default publish includes .NET, uses a single executable, and compresses its bundle. `scripts/publish-maps.ps1 -FrameworkDependent` is available for local development with an installed .NET desktop runtime; build release packages with the default self-contained mode.

The ZIP contains the executable, installer and uninstaller helpers, user instructions, and license notices. Original wiki map images, local caches, personal settings, and build artifacts are excluded from Git. Release binaries are attached to GitHub Releases rather than committed into source history.

## Package check

`check-package.ps1` generates a synthetic raster fixture and invokes the published EXE's `--smoke-test` mode. The application renders its own visual tree without displaying a window or capturing the desktop. The fixture has no game artwork or personal data.

It exercises image decoding, offline cache loading, pin projection, zoom, centering, settings and pin persistence, local import, and WPF rendering. Test settings and output remain under a unique directory inside `artifacts`. Native hotkeys, tray interactions, mixed DPI displays, and mouse passthrough still require an interactive check.

To use a real map image you have saved, pass it directly to the EXE:

```powershell
.\artifacts\release\MnMMaps.exe --smoke-test .\artifacts\map-check C:\Maps\example.png
```

## Catalog upkeep

Python 3.10 or later is needed only for this utility:

```powershell
python .\scripts\update-map-catalog.py --discover-only
python .\scripts\update-map-catalog.py
```

Requests are cached under `artifacts/map-catalog-sources`. `--refresh` re-fetches public reference pages and images. Results and skipped sources are recorded in `artifacts/map-catalog-report.json`.

The utility looks for original wiki file links in public map galleries and source-credit metadata, rather than assuming a zone's main map field contains every drawing. It excludes reviewed scenic fallbacks and placeholders, validates raster headers and dimensions, and asks the wiki image server for a smaller rendition when necessary. It does not copy atlas artwork or access any game data.

Visually review newly discovered images before publishing. An image's gallery caption alone does not establish that it is a map. Retain existing IDs and original image URLs when adding a variant; players' pins use those IDs and image coordinates. After a catalog change, update the coverage documentation and rebuild the EXE.

## GitHub Actions

**Windows build** runs on pushes to `main` and pull requests. It builds the distribution, checks the packaged EXE, and uploads the EXE, ZIP, and checksums as a temporary Actions artifact.

**Publish release** is a manual workflow for maintainers. To ship a new version:

1. Update the `<Version>` in the project and add `docs/releases/vX.Y.Z.md`.
2. Commit and push the source.
3. Create and push the matching tag, such as `v0.1.1`.
4. Open **Actions → Publish release → Run workflow** on the default branch and enter that tag.
5. The workflow checks out the tag, validates that the project version matches it, builds and checks the package, and creates a release using the matching notes file.

The workflow uses the repository's automatic `GITHUB_TOKEN` with content write permission; no personal token is stored in source. Existing releases are not overwritten automatically.

## Contribution boundaries

Keep character state, game files, game process access, screen reading, input automation, and game traffic inspection outside the application. Features should use manual user actions, the viewer's own data, or public reference material. Read [CONTRIBUTING.md](../CONTRIBUTING.md) before sending a change.

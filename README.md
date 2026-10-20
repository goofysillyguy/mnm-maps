# MnM Maps

A standalone Windows map overlay for **Monsters & Memories**. Use the full interactive **MnM Atlas** website above the game, including its map selection, floors, search, markers, personal notes, and community features. The **Wiki maps** view retains **33 map views across 26 zones**, local image import, offline caching, and the existing personal pins.

**Atlas edition: 0.2.0.** Download the Windows EXE or ZIP from Releases. See the [Atlas guide](docs/ATLAS.md) for setup, browser notes, and shortcuts.

**[Download the latest Windows release](https://github.com/goofysillyguy/mnm-maps/releases/latest)** · [Detailed installation guide](docs/INSTALLATION.md) · [Map coverage](docs/MAPS.md) · [Report a problem](https://github.com/goofysillyguy/mnm-maps/issues)

## What it does

- Displays maps in a movable, resizable window with adjustable opacity and an always on top option.
- Hosts the actual MnM Atlas site, so its menus, search, map controls, markers, note editing, import/export, sign-in, and community tools remain part of the page.
- Remembers the last Atlas map, floor, center, and zoom, with a persistent browser profile for notes and sign-in.
- Supports Atlas pop-up windows and native save dialogs for exports; external reference links open your usual browser.
- Supports scrolling to zoom, dragging to pan, and resetting the view with **Fit**.
- Saves personal pins, notes, and the last view separately for each map.
- Provides a tray icon and keyboard shortcuts to show or hide the overlay.
- Offers mouse passthrough so clicks reach the window underneath while the map stays visible.
- Downloads selected wiki maps once and caches them for offline viewing.
- Imports PNG, JPEG, GIF, BMP, and TIFF images you choose from your own files.

The map is selected manually. MnM Maps does not access the game process, game files, screen contents, or game network traffic. It does not read memory, inject code, automate input, or track your live character position. The only Windows shortcuts it registers are its own overlay controls.

## Install

You need **Windows 10 version 2004 or newer, x64**, and a mouse and keyboard. Use Monsters & Memories in **windowed or borderless mode** so Windows can display the overlay above it. The EXE includes .NET. Atlas also needs the **Microsoft Edge WebView2 Runtime**, commonly already installed on Windows; the app offers a download link if it cannot start the browser. Python and developer tools are not needed to run the EXE.

1. Open [Releases](https://github.com/goofysillyguy/mnm-maps/releases/latest).
2. Download **`MnMMaps-win-x64.zip`** from the release's **Assets** section. The GitHub **Source code** archives are for developers.
3. Right-click the ZIP, choose **Extract All**, and open the extracted folder.
4. Double-click **`MnMMaps.exe`** to run it, or double-click **`install.bat`** to install it for your Windows account and add a Start Menu shortcut.
5. In the Atlas edition, use the site's **Map** selector and search menu, or choose **Wiki maps** for the original image viewer. Uncached Atlas content and the first wiki-image download need internet access.

For the smallest download, get **`MnMMaps.exe`** directly from the same release and run it from a folder you control. The EXE works on its own. See the [installation guide](docs/INSTALLATION.md) for detailed instructions, file verification, updates, backups, uninstalling, and troubleshooting.

Windows may show an unknown-publisher prompt because these releases are not code signed. Verify that you downloaded the file from this repository's release page; the release also includes `SHA256SUMS.txt` for file verification.

## Controls

| Control | Action |
| --- | --- |
| **Ctrl+Shift+M** | Show or hide the overlay |
| **Ctrl+Shift+L** | Toggle map-only lock and mouse passthrough; unlock restores the controls |
| **MnM Atlas** / **Wiki maps** | Switch between the live interactive site and the original image viewer |
| **Ctrl+F** | Atlas's own search shortcut; repeated presses follow the website's normal behavior |
| **Ctrl+L** | Select the Atlas address so you can paste a map or place link; Enter opens it |
| **F5** / **Ctrl+R** | Reload Atlas |
| **Alt+Left** / **Alt+Right** | Back / forward through Atlas navigation |
| **Alt+Home** | Open the world map |
| Mouse wheel | Zoom around the pointer |
| Drag the map | Pan |
| **Fit** | Fit the entire map in the window |
| **Add pin**, then click the map | Save a named personal pin with optional notes |
| **Escape** | Cancel pin placement |
| **Pins** | Show the list; select a pin to center it, edit it, or remove it |
| **Lock** | Show only the map and personal pins, with mouse passthrough; Ctrl+Shift+L or the tray menu restores the controls |
| **Opacity** | Change the transparency of the whole overlay |
| **On top** | Keep the overlay above other windows |
| Drag the title bar / lower right grip | Move / resize the window |
| **Hide** | Keep running in the tray |
| **×** | Quit |

Lock hides the app's title, selectors, buttons, sliders, panels, status text, borders, and resize grip. In Atlas it also hides the website's controls while keeping its map, markers, and attribution visible. Layout is retained so locking does not pan or rescale the map. Pop-up windows hide while the overlay is hidden or locked and return when it is shown and unlocked. Unlock to interact with the site or change the view.

If the map ignores clicks, mouse passthrough is probably enabled. Press **Ctrl+Shift+L**, or right-click the tray icon and select **Unlock mouse and show**, to restore the controls and mouse interaction. If a shortcut is already used by another app, MnM Maps reports that in its status line; the tray icon remains available.

## Maps and dungeons

Dungeon coverage includes **Wyrmsbane Tomb** (combined and isometric), **Tel Ekir**, **Grain Cellar** (plan and isometric), **Blind Midden** (ground floor), **Sunken Resort** (its floors and caves), **Caves of Irem**, **Ancient Crypt**, **Infested Crypt**, **Fallen Watch**, **Rothold**, **Blacktide Bay**, and **Grimtide Sanctum**.

Some maps contain several floors on the same drawing. Zoom and pan that plan to inspect the floor you need. Separate drawings have separate entries and personal pins. [See the complete coverage table and known gaps](docs/MAPS.md).

**Wiki source** opens the original file page for authorship, revisions, and image-specific licensing. Community maps can be incomplete or outdated. You can use **Import image** for a map saved on your own PC.

## Saved data and updates

Settings, pins, map views, downloaded images, imported maps, and the persistent Atlas browser profile are stored in:

```text
%LOCALAPPDATA%\MnMMaps
```

Atlas's notes and sign-in are in `atlas-browser`, separate from your normal browser and the Wiki maps pin lists. Export notes on the Atlas site in your usual browser, then use Import inside the overlay to transfer them. Use Atlas's own Export for portable note backups; the browser profile is retained during app updates and uninstallation.

To update, quit the old overlay, download the new release, and replace the EXE or rerun `install.bat`. Keeping this data folder preserves your pins and settings. The app does not download program updates automatically.

The optional installer puts program files in `%LOCALAPPDATA%\Programs\MnMMaps`. Uninstalling with `uninstall.bat` removes those program files and its shortcut while keeping the data folder. [Backup and restore instructions](docs/INSTALLATION.md#back-up-and-restore-your-pins) cover how to retain your notes.

## Build from source

Development requires Windows x64, the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), and Git. Python 3.10 or later is only needed for the optional catalog update script.

```powershell
git clone https://github.com/goofysillyguy/mnm-maps.git
cd mnm-maps
dotnet run --project .\src\MnMMaps.App -c Release
```

Build the standalone EXE, ZIP, and checksums:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\package-release.ps1
```

The outputs are in `artifacts\release`. [Development and release notes](docs/DEVELOPMENT.md) describe the source layout, package checks, GitHub Actions, and how to update the map catalog.

## Project status and credits

Atlas support is new. The app hosts the live site, whose features and sign-in services can change independently of this release. If something behaves differently in the overlay, report the map, visible error, and steps to reproduce it. The existing package-check utility covers the Wiki maps image viewer, not Atlas.

Interface ideas came from Amy Lindley's [Mo Betta Quests](https://github.com/amyklindley/mo-betta-quests) and [Mo Betta Crafts](https://github.com/amyklindley/mo-betta-crafts). This project uses its own implementation and stores its own data. [NOTICE.md](NOTICE.md) lists map sources and credits.

MnM Maps is an unofficial fan companion and is not endorsed by Niche Worlds Cult. Its architecture avoids game access; that does not establish developer approval under the game's [Play Nice Policy](https://account.monstersandmemories.com/policy/pnp) or [Master User Agreement](https://account.monstersandmemories.com/policy/mau).

Application code: [MIT](LICENSE). Third party map artwork retains its own licensing.

# Installation and everyday use

## Requirements

- A Windows x64 PC. Windows 11 is the recommended setup for the current release.
- Monsters & Memories in windowed or borderless mode when using the overlay above the game.
- Internet access when downloading a wiki map for the first time. Cached maps and imported images work offline.

The published release includes the .NET desktop runtime. You do not need Python, Visual Studio, a game addon, an API key, or administrator access to install or run it. Builds for macOS, Linux, or Windows ARM64 are not provided.

## Download the right file

1. Open the [latest release](https://github.com/goofysillyguy/mnm-maps/releases/latest).
2. Expand **Assets** if needed.
3. Choose one of these downloads:

| File | Use it for |
| --- | --- |
| `MnMMaps-win-x64.zip` | The EXE, installation helpers, instructions, and license notices |
| `MnMMaps.exe` | Running the portable app directly |
| `SHA256SUMS.txt` | Checking the EXE or ZIP download against the published hashes |
| GitHub's **Source code** ZIP or TAR | Building or modifying the program; these archives do not contain the ready-to-run EXE |

## Option A: run it directly

1. Download `MnMMaps.exe` into a folder you control, such as `Documents\MnM Maps`.
2. Double-click the EXE.
3. Select a map in the dropdown. The first download may take a moment for large maps.

The program saves its data under `%LOCALAPPDATA%\MnMMaps`, independently of the EXE's location. Moving the EXE later does not move or erase those settings.

## Option B: install a Start Menu shortcut

1. Download `MnMMaps-win-x64.zip`.
2. Right-click it and choose **Extract All**. Open the extracted folder; run the files after extraction, rather than from inside the ZIP preview.
3. Double-click `install.bat`.
4. The installer copies program files into `%LOCALAPPDATA%\Programs\MnMMaps` and creates **MnM Maps** in your Start Menu.
5. Open **MnM Maps** from Start and select a map.

The installer uses your Windows account. It does not need administrator privileges and does not add the app to Windows startup. It leaves your existing pins and settings in place.

If Windows shows an unknown-publisher prompt, the EXE is unsigned. Check its download origin and, if desired, verify its hash before choosing whether to run it.

## Set it up above the game

1. Set the game to **windowed** or **borderless** display mode.
2. Open MnM Maps and select your zone or dungeon drawing.
3. Keep **On top** checked.
4. Drag the title bar to place it where you want. Resize using the grip in the lower right corner.
5. Adjust **Opacity** to make the window easier to read over your game.
6. Press **Ctrl+Shift+M** whenever you want to show or hide it.

The map selection is manual. There is no automatic zone detection, player marker, GPS arrow, or distance calculation.

## Zoom, pan, and annotate

- Roll the mouse wheel over the map to zoom around the pointer. The **+** and **−** buttons zoom around the middle of the window.
- Drag the image to pan. Press **Fit** to see the whole map again.
- Click **Add pin**, then click a location on the map. Enter a name and optional notes and choose **Save pin**.
- Click **Pins** to show or hide the pin list. Selecting a pin centers the map there. Use **Edit** or **Remove** for the selected pin.
- Press **Escape** to cancel pin placement.

Pins are personal annotations. They do not move your character or alter the map image. Different map drawings have separate pin lists, even when they describe the same zone.

## Lock to show only the map

Click **Lock** or press **Ctrl+Shift+L** to show only the map and your personal pins. The title, zone selector, buttons, sliders, pin panel, status text, borders, and resize grip disappear. The map stays at its current position and zoom, with the chosen opacity. Empty space around the map becomes transparent.

Lock also enables mouse passthrough: the whole overlay ignores mouse clicks so the window below can receive them.

To restore the controls and mouse interaction, press **Ctrl+Shift+L** again. You can also right-click the MnM Maps tray icon near the Windows clock and choose **Unlock mouse and show**. Showing a hidden overlay with Ctrl+Shift+M also unlocks it. Unlock before changing the map, opacity, size, or pins.

## Import your own map image

Click **Import image…**, choose a supported image, and select **Open**. The app copies the image into its own data folder and adds a map entry named after the file. Your original file is unchanged.

Supported formats are PNG, JPEG, GIF, BMP, and TIFF. Images must fit the viewer's size limits: at most 40 MiB and 100 million pixels. For a very large image, import a smaller version you have saved yourself. Imported maps are available offline.

## Update to a new version

1. Quit the current app using **×** or the tray menu's **Quit MnM Maps**. **Hide** leaves it running.
2. Download the new EXE or ZIP from [Releases](https://github.com/goofysillyguy/mnm-maps/releases/latest).
3. For a portable copy, replace the old EXE. For an installed copy, extract the new ZIP and rerun `install.bat`.
4. Open MnM Maps again.

Keep `%LOCALAPPDATA%\MnMMaps` to preserve your settings, cached maps, and pins. New map drawings get their own IDs so annotations on one drawing are not applied to a different drawing.

## Back up and restore your pins

1. Quit MnM Maps.
2. Press **Win+R**, enter `%LOCALAPPDATA%\MnMMaps`, and press Enter.
3. Copy the whole folder to your backup location.

`settings.json` contains pins, notes, selected map, window preferences, and views. The `images` folder contains cached wiki images and copies of imported maps. Backing up both retains offline access as well as annotations.

To restore on the same Windows account and path, quit the app and put the saved folder back in the same location. If you move a backup to a different account or directory, imported-image paths may need to be updated or those images imported again. Keep the backup until you have checked your notes.

## Uninstall

If you used the installer, quit the app and run `uninstall.bat` from `%LOCALAPPDATA%\Programs\MnMMaps` or the extracted release folder. It removes the installed program and the Start Menu shortcut. Your pins, cached maps, and imported images remain in `%LOCALAPPDATA%\MnMMaps`.

For a portable copy, quit and delete the EXE or its extracted download folder. To remove saved data too, first back it up if needed, then delete `%LOCALAPPDATA%\MnMMaps` yourself.

## Verify a download

Download `SHA256SUMS.txt` from the same release as the EXE or ZIP. In PowerShell, run this from your downloads folder:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath .\MnMMaps.exe
```

Compare the result to the line for `MnMMaps.exe` in the manifest. For the ZIP, substitute `MnMMaps-win-x64.zip`. Letter case does not matter; every hexadecimal digit must match. A matching hash confirms that the file matches that release's manifest.

## Troubleshooting

| Symptom | What to try |
| --- | --- |
| Overlay is behind the game | Enable **On top** and use windowed or borderless mode. Exclusive fullscreen can cover ordinary desktop windows. |
| Clicking the overlay does nothing | Press Ctrl+Shift+L or use the tray menu's **Unlock mouse and show** command. |
| Ctrl+Shift+M or Ctrl+Shift+L does not work | Another app may own that shortcut. Read the status line and use the tray menu. Mouse passthrough is disabled if its unlock shortcut could not be registered. |
| Cannot find the tray icon | Expand the hidden-icons arrow near the clock. Look for the gold map-and-compass icon whose tooltip says **MnM Maps**. |
| Map unavailable | Check the connection, then choose **Retry download**. A cached map works offline after a successful download. An original wiki image can also be moved or removed; **Import image** provides a local alternative. |
| Missing a dungeon or floor | Check the [coverage table](MAPS.md). Some plans contain all floors on one image. For a separate map you have saved, use **Import image**. |
| Installer says a file is in use | Quit the old app with **×** or its tray menu and rerun the installer. |
| Download contains only code | Get the EXE or Windows ZIP under the release's **Assets**, rather than GitHub's generated source archive. |
| App is running twice | Quit both copies, then open the one you intend to use. A single-instance guard is not implemented in this release. |

If a problem persists, [open an issue](https://github.com/goofysillyguy/mnm-maps/issues). Include the version, Windows version, selected map, error text, and display scaling if relevant. Sharing your personal `settings.json` is not necessary.

# MnM Atlas in the overlay

The Atlas edition loads the complete live website at [www.mnmatlas.com](https://www.mnmatlas.com/). It hosts the page in Microsoft Edge WebView2 rather than copying Atlas's application code or map artwork. Its normal responsive layout, menus, and site updates remain available.

## Start

1. Quit any older MnM Maps copy from its tray menu.
2. Run `MnMMaps.exe`. The initial view is the world map; later launches restore your last Atlas view.
3. If **Atlas browser unavailable** appears, use **Get WebView2 Runtime** to visit Microsoft's download page. Install the Evergreen Runtime for your PC, then choose **Retry Atlas**. The app does not install it automatically.
4. Keep **On top** checked and use the game in windowed or borderless mode.
5. Use Atlas's **Map** selector for zones and dungeons, its floor controls for multi-level maps, and its search menu for places, NPCs, trainers, trades, and other marked locations.

The page needs internet access for uncached resources and online features. **Wiki maps** keeps the previous image viewer available, including its downloaded images and imported maps when offline.

## Interactive features

- Drag, zoom, switch maps and floors, follow exits, and open marker popups using the site's controls.
- Use the site's search, marker and vendor filters, place-name controls, hidden-area controls, and available community tools.
- Add, edit, move, and remove personal notes with the site's own forms. Import and export notes through its menus.
- Copy map/place links and use ordinary text entry, selection, copy, paste, and the page's keyboard controls.
- Paste a shared Atlas link into the app's address field and press Enter or **Go**. The address field accepts HTTPS links for MnM Atlas; external reference links open your usual browser.
- Use **Browser ↗** to open the same map view in your usual browser. This transfers the view, not private notes or a sign-in session.
- Sign-in follows the website's own flow, in the overlay or an Atlas pop-up window. Pop-ups use the same browser profile so the site's own window communication can finish. Availability also depends on the website and identity provider.
- Export downloads use a Windows save dialog. Choose a destination; the status line reports completion or an interrupted download. Import uses the website's file chooser.
- The site's fullscreen button maximizes the overlay and hides its app controls while keeping the page interactive. Escape follows the website's fullscreen behavior; Ctrl+L or showing/unlocking the overlay also restores app controls.

The overlay does not automatically submit suggestions, reports, edits, or other community actions. Those remain explicit actions in the website's own interface.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| Ctrl+Shift+M | Show/hide the overlay, including Atlas pop-ups; works while another app has focus |
| Ctrl+Shift+L | Map-only lock/unlock and mouse passthrough; works while another app has focus |
| Ctrl+F | The website's search shortcut; when focus is in app controls, focus Atlas search |
| Ctrl+L | Select the Atlas address; paste a link and press Enter |
| Escape in the address field | Restore the current address and focus the page |
| F5 or Ctrl+R | Reload Atlas |
| Alt+Left / Alt+Right | Back / forward |
| Alt+Home | World map |

Only the two show/hide and lock shortcuts are global. Other shortcuts operate while the overlay has focus. The site's own shortcuts, text entry, and dialogs otherwise retain their normal behavior. If a global shortcut is unavailable, use the tray menu to show or unlock the app.

## Map-only lock

**Lock** hides the native app controls and the website's map controls, panels, titles, dialogs, and decorative frame. The current map and markers remain at their existing position and zoom. The website's map attribution remains visible. The entire overlay passes mouse clicks to the window beneath it.

Unlock with Ctrl+Shift+L or **Unlock mouse and show** in the tray menu. The page stays loaded, and its hidden controls return. Atlas pop-ups are hidden while locked or while the overlay is hidden, then restored when the overlay is shown and unlocked.

## Notes and saved state

The overlay creates its own persistent WebView2 profile:

```text
%LOCALAPPDATA%\MnMMaps\atlas-browser
```

The site's local storage, notes, cookies, and sign-in use that profile. The app remembers the last map, floor, center, and zoom in its existing `settings.json`. Temporary authentication callback parameters are excluded from that saved map address.

Notes from Chrome, Edge, Firefox, or another browser do not appear automatically in this profile. To move them:

1. Open Atlas in the browser that has your notes.
2. Use the site's note export command and save the file.
3. Open Atlas in the overlay and use the site's note import command to select that file.

The original Wiki maps pins remain in `settings.json`, keyed to their image coordinates. They remain available in **Wiki maps**; they are not converted to Atlas coordinates. Use the site's export command for portable Atlas note backups. App updates and the existing uninstaller retain the saved-data folder.

## Sources and limits

MnM Atlas is by [ocombe and contributors](https://github.com/ocombe/mnmatlas). Original map authors and source credits remain in the site and its About panel. The app retains those credits. See the site's [map and embedding documentation](https://www.mnmatlas.com/embed/) and this project's [source notices](../NOTICE.md).

The overlay chooses maps manually and does not read the game process, memory, game files, game display, or game network traffic. It does not automate game input or provide live position tracking.

Atlas is a live website whose features can change independently of the overlay. If a feature behaves differently in WebView2, report the map, visible status, Windows version, and steps to reproduce the problem. The existing automated package-check utility covers the Wiki maps image viewer, not Atlas's online features.

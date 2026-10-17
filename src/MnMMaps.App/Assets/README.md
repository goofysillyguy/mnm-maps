# Application icon

`MnMMaps.png` is the transparent master artwork. `MnMMaps.ico` contains 32-bit frames at 16, 20, 24, 32, 40, 48, 64, 96, 128, and 256 pixels for Windows display scaling. Sizes below 256 pixels use native DIB data with alpha and a transparency mask for small-icon compatibility; the 256-pixel frame uses PNG compression. The executable, WPF window/taskbar, and notification-area icon use the same ICO resource; no external icon file is required at runtime.

The artwork was generated with the built-in image generation tool on October 17, 2026 for this application. It is included with this project's MIT-licensed assets and is independent of the game's official artwork and logos.

## Generation prompt

```text
Use case: logo-brand.
Asset type: one finished square Windows desktop application icon for 'MnM Maps', an independent fantasy map viewer for Monsters & Memories. Produce a single large icon, not a mockup, screenshot, icon sheet, or a collection of alternatives.
Primary subject: a beautifully crafted folded parchment map, with a bold four-point brass compass rose placed prominently in its center. The parchment should have a simple recognizable angular folded-map silhouette with slightly curled corners and a warm aged-gold rim. The compass has one striking turquoise north-point needle and a small luminous teal center, with the other points in antique gold. Use just a few thick dark cartographic strokes and a broad winding trail on the parchment, no tiny symbols or labels. Suggest an old-school fantasy MMORPG inventory treasure icon, with hand-painted materials, tasteful depth, softly polished brass and warm desert parchment. Rich saturated antique gold and turquoise against deep shadow inside the object; high contrast and clean readable shapes.
Composition: centered, square 1:1, icon occupies roughly 85 percent of the image with balanced transparent padding, a cohesive compact silhouette, completely inside the canvas. Slight dimensionality and carefully controlled highlights, crisp contour, strong readability when reduced to 32 pixels or 16 pixels. Prioritize a clear map-and-compass emblem over intricate decorative detail.
Background: truly transparent alpha outside the icon; no colored square behind it, no environment, no floor, no cast shadow extending beyond its silhouette, no checkerboard painted into the image.
Constraints: no text, no initials, no lettering, no numbers, no watermark, no border around the canvas, no characters, no weapons, no official game logo. A professional polished fantasy mapping app icon with a transparent background.
```

## Rebuild the Windows icon

From the repository root on Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\update-app-icon.ps1
```

The converter preserves the source alpha channel and updates only the ICO file. To use different artwork without replacing the master PNG, pass `-SourcePath C:\Art\icon.png`.

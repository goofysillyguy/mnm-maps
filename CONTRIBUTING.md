# Contributing

Issues and pull requests are welcome. For bugs, include the release version, Windows version, selected map, visible error text, and reproduction steps. Display scaling and monitor layout are useful for window or mouse problems. Personal settings and screenshots of private notes are not required.

## Map corrections

Link the original public file page and identify its zone, floor, revision, and author if known. Keep image-specific attribution. Avoid placeholders, screenshots presented as maps, and redistributed artwork without suitable permission. The app normally downloads original wiki images for each user rather than bundling them in the repository or release.

Existing map IDs must remain stable because saved pins are keyed by them. Give a new drawing or a differently arranged revision its own ID. Update `docs/MAPS.md` when coverage changes.

## Application changes

- Keep the viewer independent of the game process, game files, screen contents, and game network traffic.
- Keep map choice and annotations manual. Additions should not automate game inputs.
- Store application data under the viewer's own data directory.
- Keep network access limited to documented public reference downloads initiated by application use.
- Preserve the ability to show and unlock the overlay through its tray menu.
- Keep installation per user and retain notes during updates and uninstallation.

Use the build and package check commands in [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md). Describe what changed and the checks you performed in the pull request. The source code is licensed under MIT; third party map artwork retains its own licensing.

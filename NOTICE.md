# Credits and map sources

MnM Maps is an unofficial fan companion for Monsters & Memories. It is not affiliated with or endorsed by Niche Worlds Cult. Monsters & Memories and its game material belong to their respective owners.

The application code, scripts, project documentation, and original application icon are licensed under [MIT](LICENSE). That license does not grant rights to third party map artwork, the game, or the bundled Microsoft runtime.

## Community maps

### Live MnM Atlas

The Atlas view hosts [MnM Atlas](https://www.mnmatlas.com/), by [ocombe and contributors](https://github.com/ocombe/mnmatlas), in an embedded browser. The site, its artwork, reference data, third party libraries, and online services remain supplied by that site under their respective terms. Atlas application code and map artwork are not copied into this project's source or release.

Map attribution remains visible during map-only lock. Original source authors are credited by each map and the site's About panel, including community wiki map authors such as Maggot and Nindaan. The original authors retain their rights. See [Atlas's credits and embedding documentation](https://www.mnmatlas.com/embed/#credits).

### Wiki image viewer

The executable embeds a catalog of links, not copies of the map images. When a player selects a wiki map, the app downloads that image from `static.wikitide.net` and caches it on that player's PC. Personal pins are stored separately; the original drawing is unchanged.

The catalog's **Wiki source** links point to the original image description pages on the [Monsters and Memories community wiki](https://monstersandmemories.miraheze.org/). Consult those pages and the markings on each drawing for the original author, revision, and image license. The wiki's general content license should not be assumed to apply to every uploaded image. Nothing in this project grants additional rights to those images.

Map credits include Maggot, Skoop, Walabus, Doola, chazsolo, ElusiveGarden, Nindaan, Ganderdot, Naawa, Keirvan, Neeferio, and the other authors identified on the original file pages and drawings. Names and attribution remain with their sources.

The original links were found through:

- The public wiki and the zone catalog in [Mo Betta Quests](https://github.com/amyklindley/mo-betta-quests/blob/main/zones.json).
- The public map galleries at [Stories of Monsters and Memories](https://storiesofmnm.com/zones/), by Fattboi.
- The original wiki source credits published by [MnM Atlas](https://github.com/ocombe/mnmatlas/blob/main/data/maps.json), by ocombe.

New catalog entries retain their discovery page and review date. No atlas artwork or website implementation is copied. The large Evershade overview uses the wiki server's published 4096-pixel rendition to fit the viewer's image limits.

## Interface references

The compact overlay, tray controls, show/hide shortcut, and remembered window position were informed by Amy Lindley's [Mo Betta Quests](https://github.com/amyklindley/mo-betta-quests) and [Mo Betta Crafts](https://github.com/amyklindley/mo-betta-crafts). MnM Maps is an independent WPF implementation; no source code from those projects is copied.

## Runtime

The Windows release includes the Microsoft .NET 10 desktop runtime under Microsoft's applicable license. Its notices are available in the [.NET runtime repository](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT) and the [Windows Desktop repository](https://github.com/dotnet/windowsdesktop). Building from source uses the installed .NET SDK.

The Atlas edition also redistributes the Microsoft.Web.WebView2 SDK assemblies and loader. Its original [license](docs/third-party/WebView2-LICENSE.txt) and [third party notices](docs/third-party/WebView2-NOTICE.txt) accompany the ZIP and installed documentation. The separately installed Microsoft Edge WebView2 Evergreen Runtime is not redistributed by this application. The Windows SDK .NET projection and WinRT runtime are supplied through the versioned Windows .NET target; their [license and notices](https://github.com/microsoft/CsWinRT) remain applicable.

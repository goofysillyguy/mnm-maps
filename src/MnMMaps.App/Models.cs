using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MnMMaps;

public sealed class MapEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string Credit { get; set; } = "Community wiki map. See the source page for image credits and licensing.";
    public string? LocalFile { get; set; }
    public override string ToString() => Name;
}

public sealed class MapPin
{
    public string Name { get; set; } = "";
    public string Notes { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public override string ToString() => Name;
}

public sealed class MapView
{
    public double Zoom { get; set; } = 1;
    public double CenterX { get; set; } = 0.5;
    public double CenterY { get; set; } = 0.5;
}

public sealed class OverlaySettings
{
    public double Width { get; set; } = 1020;
    public double Height { get; set; } = 800;
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Opacity { get; set; } = 0.95;
    public bool Topmost { get; set; } = true;
    public string Viewer { get; set; } = "atlas";
    public string AtlasUrl { get; set; } = AtlasBrowser.InitialUrl;
    public string SelectedMap { get; set; } = "night-harbor";
    public List<MapEntry> ImportedMaps { get; set; } = [];
    public Dictionary<string, List<MapPin>> Pins { get; set; } = [];
    public Dictionary<string, MapView> Views { get; set; } = [];
}

public sealed class MapStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public string Root { get; }
    public OverlaySettings Settings { get; }

    public MapStore(string? root = null)
    {
        Root = root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MnMMaps");
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Path.Combine(Root, "images"));
        try
        {
            Settings = JsonSerializer.Deserialize<OverlaySettings>(File.ReadAllText(Path.Combine(Root, "settings.json")), JsonOptions) ?? new();
            if (Settings.ImportedMaps is null || Settings.Pins is null || Settings.Views is null)
                throw new JsonException("Incomplete settings.");
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            Settings = new();
            var original = Path.Combine(Root, "settings.json");
            if (File.Exists(original)) File.Copy(original, Path.Combine(Root, "settings-recovery-" + DateTime.UtcNow.Ticks + ".json"));
        }
    }

    public List<MapEntry> LoadCatalog()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MnMMaps.Data.maps.json")
            ?? throw new InvalidOperationException("The map catalog is missing.");
        var maps = JsonSerializer.Deserialize<List<MapEntry>>(stream, JsonOptions) ?? [];
        return maps.Concat(Settings.ImportedMaps).OrderBy(map => map.Name).ToList();
    }

    public List<MapPin> PinsFor(string id)
    {
        if (!Settings.Pins.TryGetValue(id, out var pins)) Settings.Pins[id] = pins = [];
        return pins;
    }

    public string CachedImagePath(MapEntry map)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(map.ImageUrl)));
        return Path.Combine(Root, "images", hash + ".image");
    }

    public MapEntry Import(string path)
    {
        var id = "local-" + Guid.NewGuid().ToString("N");
        var destination = Path.Combine(Root, "images", id + Path.GetExtension(path));
        File.Copy(path, destination);
        var map = new MapEntry { Id = id, Name = Path.GetFileNameWithoutExtension(path), LocalFile = destination,
            Credit = "Your imported map. Personal pins are stored separately; the image is unchanged." };
        Settings.ImportedMaps.Add(map);
        Save();
        return map;
    }

    public void Save()
    {
        var temporary = Path.Combine(Root, "settings.json.tmp");
        File.WriteAllText(temporary, JsonSerializer.Serialize(Settings, JsonOptions));
        File.Move(temporary, Path.Combine(Root, "settings.json"), overwrite: true);
    }
}

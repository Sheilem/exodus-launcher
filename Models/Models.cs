using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExodusLauncher.Models;

/// <summary>Shared JSON options: snake_case on the wire, PascalCase in C#.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

/// <summary>launcher.config.json — endpoints and game wiring.</summary>
public sealed class LauncherConfig
{
    public string ManifestUrl { get; set; } = "http://localhost:8777/manifest.json";
    public string StatusUrl { get; set; } = "http://localhost:8777/status.json";
    public string NewsUrl { get; set; } = "http://localhost:8777/news.json";
    public string GameExe { get; set; } = "Exodus.exe";
    public string GameArgs { get; set; } = "";
    public string GameDir { get; set; } = ".";
    public string PatchNotesUrl { get; set; } = "http://localhost:8777/patchnotes.json";
    public string DiscordUrl { get; set; } = "";
    public string ForumUrl { get; set; } = "";
}

/// <summary>Remote patch manifest published by the backend generator.</summary>
public sealed class Manifest
{
    public string Version { get; set; } = "0.0.0";
    public string PublishedUtc { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string ClientExe { get; set; } = "Exodus.exe";
    public List<FileEntry> Files { get; set; } = new();
}

public sealed class FileEntry
{
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public string Sha256 { get; set; } = "";
    /// <summary>Optional relative download path override (defaults to <see cref="Path"/>).</summary>
    public string? Url { get; set; }
}

/// <summary>Local verification cache (our equivalent of Mush's patchcache.xml).</summary>
public sealed class CacheEntry
{
    public long Size { get; set; }
    public long MTimeTicks { get; set; }
    public string Sha256 { get; set; } = "";
}

public sealed class ServerStatus
{
    public bool Online { get; set; }
    public string Uptime { get; set; } = "";
    public int? Players { get; set; }
    public string Updated { get; set; } = "";
}

public sealed class NewsFeed
{
    public List<NewsItem> Items { get; set; } = new();
}

public sealed class NewsItem
{
    public string Date { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Tag { get; set; }
}

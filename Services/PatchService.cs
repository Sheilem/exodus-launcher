using System.IO;
using System.Net.Http;
using System.Text.Json;
using ExodusLauncher.Models;

namespace ExodusLauncher.Services;

public sealed class PatchProgress
{
    public string Phase { get; init; } = "";
    public string Detail { get; init; } = "";
    public int Current { get; init; }
    public int Total { get; init; }
    /// <summary>0..1 overall, or -1 for indeterminate.</summary>
    public double Fraction { get; init; } = -1;
}

public sealed class PatchPlan
{
    public Manifest Manifest { get; init; } = new();
    public List<FileEntry> Outdated { get; init; } = new();
    public long BytesToDownload => Outdated.Sum(f => f.Size);
    public bool UpToDate => Outdated.Count == 0;
}

/// <summary>
/// Incremental patcher. Mirrors the smart part of Mush's design: a local cache keyed by
/// (size, mtime) lets us skip re-hashing unchanged files, so repeat launches are instant.
/// </summary>
public sealed class PatchService
{
    private readonly LauncherConfig _cfg;
    private readonly string _root;
    private readonly string _cachePath;
    private readonly HttpClient _http;
    private Dictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public PatchService(LauncherConfig cfg, string root)
    {
        _cfg = cfg;
        _root = root;
        _cachePath = Path.Combine(root, "patchcache.json");
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ExodusLauncher/1.0");
    }

    private void LoadCache()
    {
        try
        {
            if (File.Exists(_cachePath))
                _cache = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(
                    File.ReadAllText(_cachePath), Json.Options) ?? new(StringComparer.OrdinalIgnoreCase);
        }
        catch { _cache = new(StringComparer.OrdinalIgnoreCase); }
    }

    private void SaveCache()
    {
        try { File.WriteAllText(_cachePath, JsonSerializer.Serialize(_cache, Json.Options)); }
        catch { /* cache is an optimization; ignore write failures */ }
    }

    public async Task<Manifest> FetchManifestAsync(CancellationToken ct)
    {
        var json = await _http.GetStringAsync(_cfg.ManifestUrl, ct);
        return JsonSerializer.Deserialize<Manifest>(json, Json.Options)
               ?? throw new InvalidDataException("Manifest was empty or invalid.");
    }

    private string LocalPath(FileEntry f) =>
        Path.Combine(_root, f.Path.Replace('/', Path.DirectorySeparatorChar));

    public async Task<PatchPlan> CheckAsync(IProgress<PatchProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new PatchProgress { Phase = "Contacting patch server…" });
        var manifest = await FetchManifestAsync(ct);
        LoadCache();

        var outdated = new List<FileEntry>();
        int i = 0, total = manifest.Files.Count;
        foreach (var f in manifest.Files)
        {
            ct.ThrowIfCancellationRequested();
            i++;
            progress?.Report(new PatchProgress
            {
                Phase = "Verifying files", Detail = f.Path,
                Current = i, Total = total, Fraction = total == 0 ? 1 : (double)i / total
            });

            var local = LocalPath(f);
            if (!File.Exists(local)) { outdated.Add(f); continue; }

            var fi = new FileInfo(local);
            string localHash;
            if (_cache.TryGetValue(f.Path, out var ce) && ce.Size == fi.Length && ce.MTimeTicks == fi.LastWriteTimeUtc.Ticks)
            {
                localHash = ce.Sha256; // trusted cache hit — no re-hash
            }
            else
            {
                if (fi.Length != f.Size) { outdated.Add(f); continue; } // size differs => definitely stale
                localHash = await HashUtil.Sha256FileAsync(local, ct);
                _cache[f.Path] = new CacheEntry { Size = fi.Length, MTimeTicks = fi.LastWriteTimeUtc.Ticks, Sha256 = localHash };
            }

            if (!string.Equals(localHash, f.Sha256, StringComparison.OrdinalIgnoreCase))
                outdated.Add(f);
        }

        SaveCache();
        return new PatchPlan { Manifest = manifest, Outdated = outdated };
    }

    public async Task ApplyAsync(PatchPlan plan, IProgress<PatchProgress>? progress, CancellationToken ct)
    {
        LoadCache();
        long totalBytes = Math.Max(1, plan.BytesToDownload);
        long doneBytes = 0;
        int i = 0, count = plan.Outdated.Count;

        foreach (var f in plan.Outdated)
        {
            ct.ThrowIfCancellationRequested();
            i++;
            var url = CombineUrl(plan.Manifest.BaseUrl, f.Url ?? f.Path);
            var dest = LocalPath(f);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            var tmp = dest + ".part";

            using (var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                resp.EnsureSuccessStatusCode();
                await using var src = await resp.Content.ReadAsStreamAsync(ct);
                await using var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None);
                var buf = new byte[1 << 18];
                int read;
                while ((read = await src.ReadAsync(buf, ct)) > 0)
                {
                    await fs.WriteAsync(buf.AsMemory(0, read), ct);
                    doneBytes += read;
                    progress?.Report(new PatchProgress
                    {
                        Phase = $"Downloading {i}/{count}", Detail = f.Path,
                        Current = i, Total = count, Fraction = Math.Min(1, (double)doneBytes / totalBytes)
                    });
                }
            }

            var got = await HashUtil.Sha256FileAsync(tmp, ct);
            if (!string.Equals(got, f.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(tmp);
                throw new InvalidDataException($"Hash mismatch after download: {f.Path}");
            }

            if (File.Exists(dest)) File.Delete(dest);
            File.Move(tmp, dest);

            var fi = new FileInfo(dest);
            _cache[f.Path] = new CacheEntry { Size = fi.Length, MTimeTicks = fi.LastWriteTimeUtc.Ticks, Sha256 = f.Sha256 };
        }

        SaveCache();
    }

    private static string CombineUrl(string baseUrl, string rel)
    {
        if (string.IsNullOrEmpty(baseUrl)) return rel;
        return $"{baseUrl.TrimEnd('/')}/{rel.TrimStart('/')}";
    }
}

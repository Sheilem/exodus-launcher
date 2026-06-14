using System.Net.Http;
using System.Text.Json;
using ExodusLauncher.Models;

namespace ExodusLauncher.Services;

public sealed class StatusService
{
    private readonly LauncherConfig _cfg;
    private readonly HttpClient _http;

    public StatusService(LauncherConfig cfg)
    {
        _cfg = cfg;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ExodusLauncher/1.0");
    }

    /// <summary>Polls the status endpoint. Returns null if unreachable (UI shows "unknown").</summary>
    public async Task<ServerStatus?> FetchAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await _http.GetStringAsync(_cfg.StatusUrl, ct);
            return JsonSerializer.Deserialize<ServerStatus>(json, Json.Options);
        }
        catch { return null; }
    }
}

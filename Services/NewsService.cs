using System.Net.Http;
using System.Text.Json;
using ExodusLauncher.Models;

namespace ExodusLauncher.Services;

public sealed class NewsService
{
    private readonly LauncherConfig _cfg;
    private readonly HttpClient _http;

    public NewsService(LauncherConfig cfg)
    {
        _cfg = cfg;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ExodusLauncher/1.0");
    }

    public Task<NewsFeed?> FetchAsync(CancellationToken ct = default) => FetchUrlAsync(_cfg.NewsUrl, ct);

    /// <summary>Fetch any feed (news or patch notes) by URL. Returns null if unreachable.</summary>
    public async Task<NewsFeed?> FetchUrlAsync(string url, CancellationToken ct = default)
    {
        try
        {
            var json = await _http.GetStringAsync(url, ct);
            return JsonSerializer.Deserialize<NewsFeed>(json, Json.Options);
        }
        catch { return null; }
    }
}

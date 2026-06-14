using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ExodusLauncher.Models;
using ExodusLauncher.Services;

namespace ExodusLauncher;

public partial class MainWindow : Window
{
    private enum PlayMode { Busy, Update, Play }

    private readonly PatchService _patch;
    private readonly StatusService _status;
    private readonly NewsService _news;

    private PatchPlan? _plan;
    private PlayMode _mode = PlayMode.Busy;

    public MainWindow()
    {
        InitializeComponent();
        var cfg = App.Config;
        _patch = new PatchService(cfg, App.GameDir);
        _status = new StatusService(cfg);
        _news = new NewsService(cfg);

        Loaded += async (_, _) =>
        {
            ShowTab(patch: false);
            _ = RefreshStatusAsync();
            _ = RefreshFeedsAsync();
            await RunCheckAsync();
        };
    }

    // ---------------- server status ----------------

    private async Task RefreshStatusAsync()
    {
        var s = await _status.FetchAsync();
        if (s is null)
        {
            StatusDot.Fill = (Brush)FindResource("Unknown");
            StatusGlow.Color = Colors.Transparent;
            StatusText.Text = "STATUS UNKNOWN";
            UptimeText.Text = "could not reach status server";
            PlayersText.Text = "";
            return;
        }

        if (s.Online)
        {
            StatusDot.Fill = (Brush)FindResource("Online");
            StatusGlow.Color = Color.FromRgb(0x45, 0xD6, 0x7E);
            StatusText.Text = "SERVER ONLINE";
            UptimeText.Text = string.IsNullOrWhiteSpace(s.Uptime) ? "" : $"Uptime: {s.Uptime}";
            PlayersText.Text = s.Players is int p ? $"{p} players online" : "";
        }
        else
        {
            StatusDot.Fill = (Brush)FindResource("Offline");
            StatusGlow.Color = Color.FromRgb(0xEF, 0x53, 0x50);
            StatusText.Text = "SERVER OFFLINE";
            UptimeText.Text = "the server is currently down";
            PlayersText.Text = "";
        }
    }

    // ---------------- news + patch notes ----------------

    private async Task RefreshFeedsAsync()
    {
        var news = await _news.FetchUrlAsync(App.Config.NewsUrl);
        NewsList.ItemsSource = (news?.Items is { Count: > 0 })
            ? news.Items
            : new List<NewsItem> { new() { Title = "No news yet", Tag = "INFO", Body = "Announcements will show up here." } };

        var patch = await _news.FetchUrlAsync(App.Config.PatchNotesUrl);
        PatchList.ItemsSource = (patch?.Items is { Count: > 0 })
            ? patch.Items
            : new List<NewsItem> { new() { Title = "No patch notes yet", Tag = "INFO", Body = "Version changelogs will show up here." } };
    }

    private void ShowTab(bool patch)
    {
        NewsScroller.Visibility = patch ? Visibility.Collapsed : Visibility.Visible;
        PatchScroller.Visibility = patch ? Visibility.Visible : Visibility.Collapsed;
        NewsTab.Tag = patch ? null : "active";
        PatchTab.Tag = patch ? "active" : null;
    }

    private void NewsTab_Click(object sender, RoutedEventArgs e) => ShowTab(patch: false);
    private void PatchTab_Click(object sender, RoutedEventArgs e) => ShowTab(patch: true);

    // ---------------- patch check / apply ----------------

    private async Task RunCheckAsync()
    {
        SetMode(PlayMode.Busy, "CHECKING…");
        var progress = new Progress<PatchProgress>(OnProgress);
        try
        {
            _plan = await _patch.CheckAsync(progress, CancellationToken.None);
            VersionLabel.Text = $"client v{_plan.Manifest.Version}";

            if (_plan.UpToDate)
            {
                PhaseText.Text = "Up to date";
                DetailText.Text = "";
                Progress.Value = 1;
                SetMode(PlayMode.Play, "PLAY");
            }
            else
            {
                PhaseText.Text = $"Update available — {_plan.Outdated.Count} file(s)";
                DetailText.Text = HumanBytes(_plan.BytesToDownload);
                Progress.Value = 0;
                SetMode(PlayMode.Update, "UPDATE & PLAY");
            }
        }
        catch (Exception ex)
        {
            PhaseText.Text = "Could not reach patch server";
            DetailText.Text = ex.Message;
            // Let the player launch anyway with whatever they have.
            SetMode(PlayMode.Play, "PLAY OFFLINE");
        }
    }

    private async Task RunUpdateAsync()
    {
        if (_plan is null) return;
        SetMode(PlayMode.Busy, "UPDATING…");
        var progress = new Progress<PatchProgress>(OnProgress);
        try
        {
            await _patch.ApplyAsync(_plan, progress, CancellationToken.None);
            PhaseText.Text = "Update complete";
            DetailText.Text = "";
            Progress.Value = 1;
            LaunchGame();
        }
        catch (Exception ex)
        {
            PhaseText.Text = "Update failed";
            DetailText.Text = ex.Message;
            SetMode(PlayMode.Update, "RETRY UPDATE");
        }
    }

    private void OnProgress(PatchProgress p)
    {
        PhaseText.Text = p.Phase;
        DetailText.Text = p.Detail;
        if (p.Fraction >= 0)
        {
            Progress.IsIndeterminate = false;
            Progress.Value = p.Fraction;
        }
    }

    // ---------------- play button ----------------

    private void SetMode(PlayMode mode, string text)
    {
        _mode = mode;
        PlayBtn.Content = text;
        PlayBtn.IsEnabled = mode != PlayMode.Busy;
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        switch (_mode)
        {
            case PlayMode.Update: await RunUpdateAsync(); break;
            case PlayMode.Play: LaunchGame(); break;
        }
    }

    private void LaunchGame()
    {
        var exe = Path.Combine(App.GameDir, App.Config.GameExe);
        if (!File.Exists(exe))
        {
            MessageBox.Show($"Game client not found:\n{exe}\n\nPlace the launcher in your Exodus folder (next to {App.Config.GameExe}).",
                "Exodus Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = App.Config.GameArgs,
                WorkingDirectory = App.GameDir,
                UseShellExecute = true
            });
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Exodus Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---------------- nav ----------------

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var iniPath = Path.Combine(App.GameDir, "config.ini");
        new SettingsWindow(iniPath) { Owner = this }.ShowDialog();
    }

    private void Discord_Click(object sender, RoutedEventArgs e) => OpenUrl(App.Config.DiscordUrl);
    private void Forum_Click(object sender, RoutedEventArgs e) => OpenUrl(App.Config.ForumUrl);

    private static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    // ---------------- window chrome ----------------

    private void TitleBar_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    // ---------------- helpers ----------------

    private static string HumanBytes(long bytes)
    {
        string[] u = { "B", "KB", "MB", "GB" };
        double v = bytes; int i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return $"{v:0.#} {u[i]}";
    }
}

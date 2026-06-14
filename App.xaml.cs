using System.IO;
using System.Text.Json;
using System.Windows;
using ExodusLauncher.Models;

namespace ExodusLauncher;

public partial class App : Application
{
    public static LauncherConfig Config { get; private set; } = new();

    /// <summary>Folder the launcher exe lives in (where the game + config.ini sit).</summary>
    public static string BaseDir => AppContext.BaseDirectory;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var path = Path.Combine(BaseDir, "launcher.config.json");
            if (File.Exists(path))
                Config = JsonSerializer.Deserialize<LauncherConfig>(File.ReadAllText(path), Json.Options) ?? new();
        }
        catch { Config = new LauncherConfig(); }

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "Exodus Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }

    /// <summary>Resolve the game directory from config (relative paths are relative to the exe).</summary>
    public static string GameDir
    {
        get
        {
            var d = Config.GameDir;
            if (string.IsNullOrWhiteSpace(d) || d == ".") return BaseDir;
            return Path.IsPathRooted(d) ? d : Path.GetFullPath(Path.Combine(BaseDir, d));
        }
    }
}

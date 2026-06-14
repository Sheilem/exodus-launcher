using System.IO;
using System.Windows;
using System.Windows.Input;
using ExodusLauncher.Services;

namespace ExodusLauncher;

public partial class SettingsWindow : Window
{
    private readonly ConfigIniService _ini;

    public SettingsWindow(string iniPath)
    {
        InitializeComponent();
        _ini = new ConfigIniService(iniPath);
        PathNote.Text = _ini.Exists ? iniPath : $"{iniPath}  (will be created)";
        Load();
    }

    private void Load()
    {
        WidthBox.Text = _ini.Get("general", "width") ?? "1280";
        HeightBox.Text = _ini.Get("general", "height") ?? "720";
        WindowedChk.IsChecked = Bool(_ini.Get("general", "WindowedMode"), true);
        WasdChk.IsChecked = Bool(_ini.Get("general", "use_wasd"), false);

        SkipFadeChk.IsChecked = Bool(_ini.Get("features", "skip_fade"), false);
        SkipMakerChk.IsChecked = Bool(_ini.Get("features", "skip_maker"), false);
        LightbulbsChk.IsChecked = Bool(_ini.Get("features", "disable_lightbulbs"), false);
        WinKeyChk.IsChecked = Bool(_ini.Get("features", "enable_winkey"), false);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _ini.Set("general", "width", Digits(WidthBox.Text, "1280"));
        _ini.Set("general", "height", Digits(HeightBox.Text, "720"));
        _ini.Set("general", "WindowedMode", Str(WindowedChk.IsChecked));
        _ini.Set("general", "use_wasd", Str(WasdChk.IsChecked));

        _ini.Set("features", "skip_fade", Str(SkipFadeChk.IsChecked));
        _ini.Set("features", "skip_maker", Str(SkipMakerChk.IsChecked));
        _ini.Set("features", "disable_lightbulbs", Str(LightbulbsChk.IsChecked));
        _ini.Set("features", "enable_winkey", Str(WinKeyChk.IsChecked));

        try
        {
            _ini.Save();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Game Settings", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private static bool Bool(string? v, bool fallback)
        => bool.TryParse(v, out var b) ? b : fallback;

    private static string Str(bool? b) => (b == true) ? "true" : "false";

    private static string Digits(string s, string fallback)
    {
        var d = new string((s ?? "").Where(char.IsDigit).ToArray());
        return d.Length == 0 ? fallback : d;
    }
}

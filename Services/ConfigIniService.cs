using System.IO;
using System.Text;

namespace ExodusLauncher.Services;

/// <summary>
/// Minimal, comment-and-order-preserving INI reader/writer for the game's config.ini
/// (the exodus.dll feature toggles: resolution, windowed, skip_fade, use_wasd, …).
/// </summary>
public sealed class ConfigIniService
{
    private readonly string _path;
    private readonly List<string> _lines = new();

    public ConfigIniService(string path)
    {
        _path = path;
        if (File.Exists(path))
            _lines = File.ReadAllLines(path).ToList();
    }

    public bool Exists => File.Exists(_path);

    /// <summary>Get a key's value within [section], or null if absent.</summary>
    public string? Get(string section, string key)
    {
        var idx = FindKey(section, key);
        if (idx < 0) return null;
        var eq = _lines[idx].IndexOf('=');
        return _lines[idx][(eq + 1)..].Trim();
    }

    /// <summary>Set a key within [section], creating the section/key if needed.</summary>
    public void Set(string section, string key, string value)
    {
        var idx = FindKey(section, key);
        if (idx >= 0)
        {
            var eq = _lines[idx].IndexOf('=');
            var prefix = eq >= 0 ? _lines[idx][..(eq + 1)] : $"{key}=";
            _lines[idx] = $"{prefix}{value}";
            return;
        }

        var sec = FindSection(section);
        if (sec < 0)
        {
            if (_lines.Count > 0 && _lines[^1].Trim().Length > 0) _lines.Add("");
            _lines.Add($"[{section}]");
            _lines.Add($"{key}={value}");
        }
        else
        {
            // insert right after the section header
            _lines.Insert(sec + 1, $"{key}={value}");
        }
    }

    public void Save() => File.WriteAllLines(_path, _lines, new UTF8Encoding(false));

    private int FindSection(string section)
    {
        var want = $"[{section}]";
        for (int i = 0; i < _lines.Count; i++)
            if (_lines[i].Trim().Equals(want, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    private int FindKey(string section, string key)
    {
        int start = FindSection(section);
        if (start < 0) return -1;
        for (int i = start + 1; i < _lines.Count; i++)
        {
            var t = _lines[i].Trim();
            if (t.StartsWith('[')) break; // next section
            if (t.Length == 0 || t.StartsWith(';')) continue;
            var eq = t.IndexOf('=');
            if (eq < 0) continue;
            if (t[..eq].Trim().Equals(key, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }
}

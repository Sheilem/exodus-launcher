using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExodusLauncher;

/// <summary>Empty/null string -> Collapsed, otherwise Visible.</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

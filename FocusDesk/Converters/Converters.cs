using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FocusDesk.Models;

namespace FocusDesk.Converters;

/// <summary>Converte un bool in Visibility (true = Visible)</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>Converte un bool in Visibility invertito (true = Collapsed)</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}

/// <summary>Converte SessionType nel colore accent corrispondente</summary>
public class SessionTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SessionType type)
        {
            return type switch
            {
                SessionType.Focus => new SolidColorBrush(Color.FromRgb(0xE9, 0x45, 0x60)),
                SessionType.PausaBreve => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x91)),
                SessionType.PausaLunga => new SolidColorBrush(Color.FromRgb(0x45, 0x7B, 0xBD)),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converte SessionType nel colore di sfondo della tab corrispondente</summary>
public class ModeTabBackgroundConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return Brushes.Transparent;
        var currentMode = values[0];
        var tabMode = values[1];
        return currentMode?.ToString() == tabMode?.ToString()
            ? new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF))
            : Brushes.Transparent;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converte progress (0-1) in Stroke dash per il timer circolare</summary>
public class ProgressToArcConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            double radius = 80;
            if (parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r))
                radius = r;

            double circumference = 2 * Math.PI * radius;
            double dashLength = circumference * Math.Clamp(progress, 0, 1);
            return new DoubleCollection(new[] { dashLength, circumference });
        }
        return new DoubleCollection(new[] { 0.0, 502.0 }); // Default fallback per raggio 80
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converte il numero di pomodori completati/stimati in stringa emoji</summary>
public class PomodoroCountConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return "";
        int completed = values[0] is int c ? c : 0;
        int estimated = values[1] is int e ? e : 1;
        var completed_icons = string.Concat(Enumerable.Repeat("🍅", completed));
        var remaining_icons = string.Concat(Enumerable.Repeat("⬜", Math.Max(0, estimated - completed)));
        return completed_icons + remaining_icons;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converte int in stringa con prefisso fisso (es: "x3")</summary>
public class IntToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => $"x{value}";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString()?.TrimStart('x') is string s && int.TryParse(s, out var n) ? n : 1;
}

/// <summary>Converte un bool in FontWeight (true = Bold)</summary>
public class BoolToFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? FontWeights.Bold : FontWeights.Normal;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converte un bool invertito in bool</summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>Converte intensità sessioni (0-N) in colore heatmap</summary>
public class HeatmapColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count switch
            {
                0 => new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x3A)),
                1 => new SolidColorBrush(Color.FromRgb(0x7B, 0x1A, 0x2B)),
                2 => new SolidColorBrush(Color.FromRgb(0xAD, 0x26, 0x3D)),
                3 => new SolidColorBrush(Color.FromRgb(0xD4, 0x33, 0x4E)),
                _ => new SolidColorBrush(Color.FromRgb(0xE9, 0x45, 0x60))
            };
        }
        return new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x3A));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converte SessionType nel colore di sfondo dell'app in modo sfumato per migliore leggibilità</summary>
public class SessionTypeToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Color topColor;
        var bottomColor = Color.FromRgb(13, 13, 26); // #0D0D1A - base scura dell'app

        if (value is SessionType type)
        {
            topColor = type switch
            {
                SessionType.Focus => Color.FromRgb(175, 73, 73),
                SessionType.PausaBreve => Color.FromRgb(41, 116, 121),
                SessionType.PausaLunga => Color.FromRgb(47, 106, 149),
                _ => bottomColor
            };
        }
        else
        {
            topColor = bottomColor;
        }

        if (topColor == bottomColor)
            return new SolidColorBrush(bottomColor);

        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        // Parte dal colore vivo in alto e sfuma verso i colori scuri standard dell'app
        brush.GradientStops.Add(new GradientStop(topColor, 0.0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(26, 26, 46), 0.5)); // #1A1A2E al centro
        brush.GradientStops.Add(new GradientStop(bottomColor, 1.0));

        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

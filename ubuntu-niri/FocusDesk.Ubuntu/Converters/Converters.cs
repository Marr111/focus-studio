using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia;
using Avalonia.Media.Imaging;

namespace FocusDesk.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return b;
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class IntToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value != null && parameter?.ToString() == "ModeName")
            {
                var s = value.ToString();
                if (s == "Focus") return "Focus";
                if (s == "PausaBreve") return "Pausa Breve";
                if (s == "PausaLunga") return "Pausa Lunga";
            }
            return value?.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class PomodoroCountConverter : IMultiValueConverter
    {
        public object? Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values != null && values.Count >= 2 && values[0] is int completed && values[1] is int estimated)
            {
                string res = "";
                for (int i = 0; i < completed; i++) res += "🍅";
                for (int i = completed; i < estimated; i++) res += "⚪";
                return res;
            }
            return "";
        }
    }

    public class BoolToFontWeightConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b && b) return FontWeight.Normal;
            return FontWeight.SemiBold;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ExePathToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Fallback for Linux since ExtractAssociatedIcon is Windows only.
            // Ideally we'd return a default executable icon.
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class SessionTypeToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var str = value?.ToString();
            if (str == "Focus") return new SolidColorBrush(Color.Parse("#E94560"));
            if (str == "PausaBreve") return new SolidColorBrush(Color.Parse("#4CAF91"));
            if (str == "PausaLunga") return new SolidColorBrush(Color.Parse("#457BBD"));
            return new SolidColorBrush(Color.Parse("#E94560"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class SessionTypeToBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var str = value?.ToString();
            var c = Color.Parse("#E94560");
            if (str == "PausaBreve") c = Color.Parse("#4CAF91");
            if (str == "PausaLunga") c = Color.Parse("#457BBD");
            
            var gradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative)
            };
            gradient.GradientStops.Add(new GradientStop(Color.Parse("#0D0D1A"), 0));
            gradient.GradientStops.Add(new GradientStop(Color.FromArgb(20, c.R, c.G, c.B), 1));
            return gradient;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ModeTabBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value?.ToString() == parameter?.ToString())
            {
                return new SolidColorBrush(Color.Parse("#252545"));
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class HeatmapColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                if (count == 0) return new SolidColorBrush(Color.Parse("#21213A"));
                if (count <= 2) return new SolidColorBrush(Color.Parse("#7B1A2B"));
                if (count <= 5) return new SolidColorBrush(Color.Parse("#AD263D"));
                if (count <= 8) return new SolidColorBrush(Color.Parse("#D4334E"));
                return new SolidColorBrush(Color.Parse("#E94560"));
            }
            return new SolidColorBrush(Color.Parse("#21213A"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    
    public class ProgressToArcConverter : IMultiValueConverter
    {
        public object? Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            return 0;
        }
    }

    public class StringEqualityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

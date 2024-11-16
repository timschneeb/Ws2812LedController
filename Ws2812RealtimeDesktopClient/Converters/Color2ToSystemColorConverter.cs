using System.Drawing;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Utilities;
using FluentAvalonia.UI.Media;

namespace Ws2812RealtimeDesktopClient.Converters;

public class Color2ToSystemColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return Color2.FromARGB(color.A, color.R, color.G, color.B);
        }
        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color2 color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        return AvaloniaProperty.UnsetValue;
    }
}
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Utilities;

namespace Ws2812RealtimeDesktopClient.Converters;

public class FuncValueDynamicConverter<TIn, TOut> : IValueConverter
{
    private readonly Func<TIn?, TOut>? _convert;
    private readonly Func<TOut?, TIn>? _convertBack;
    
    public FuncValueDynamicConverter(Func<TIn?, TOut>? convert = null, Func<TOut?, TIn>? convertBack = null)
    {
        _convert = convert; 
        _convertBack = convertBack;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (_convert != null)
        {
            return TypeUtilities.CanCast<TIn>(value) ? _convert((TIn?)value) : AvaloniaProperty.UnsetValue;
        }
        
        if (TypeUtilities.CanCast<TIn>(value))
        {
            var v = (TIn?)value;
            return v == null ? AvaloniaProperty.UnsetValue : (TOut)(object)v;
        }
        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (_convertBack != null)
        {
            return TypeUtilities.CanCast<TOut>(value) ? _convertBack((TOut?)value) : AvaloniaProperty.UnsetValue;
        }
        
        if (TypeUtilities.CanCast<TOut>(value))
        {
            var v = (TOut?)value;
            return v == null ? AvaloniaProperty.UnsetValue : (TIn)(object)v;
        }
        return AvaloniaProperty.UnsetValue;
    }
}
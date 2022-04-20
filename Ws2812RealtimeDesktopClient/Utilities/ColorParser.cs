using Avalonia.Media;
using Config.Net;
using Newtonsoft.Json;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Utilities
{
    public class ColorParser : ITypeParser
    {
        public IEnumerable<Type> SupportedTypes => new[] { typeof(Color) };

        public string? ToRawString(object? value)
        {
            if (value is Color c)
            {
                return c.ToUint32().ToString();
            }
            return null;
        }

        public bool TryParse(string? value, Type t, out object? result)
        {
            if(string.IsNullOrEmpty(value))
            {
                result = null;
                return false;
            }

            if(t == typeof(Color))
            {
                try
                {
                    result = Color.FromUInt32(uint.Parse(value));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ColorParser: Exception raised ({ex.Message})");
                    result = null;
                    return false;
                }

                return true;
            }

            result = null;
            return false;
        }
    }
}
using Config.Net;
using Newtonsoft.Json;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Utilities
{
    public class ClassArrayParser<T> : ITypeParser
    {
        public IEnumerable<Type> SupportedTypes => new[] { typeof(T[]) };

        public string? ToRawString(object? value)
        {
            if (value is T[] enumerable)
            {
                return JsonConvert.SerializeObject(enumerable);
            }
            return null;
        }

        public bool TryParse(string? value, Type t, out object? result)
        {
            if(string.IsNullOrEmpty(value))
            {
                result = Array.Empty<T>();
                return false;
            }

            if(t == typeof(T[]))
            {
                try
                {
                    result = JsonConvert.DeserializeObject<T[] >(value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ClassArrayParser: Exception raised ({ex.Message})");
                    result = Array.Empty<T>();
                    return false;
                }

                return true;
            }

            result = Array.Empty<T>();
            return false;
        }
    }
}
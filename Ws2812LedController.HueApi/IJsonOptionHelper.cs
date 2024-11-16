using System.Text.Json;
using System.Text.Json.Serialization;
using Ws2812LedController.HueApi.Converters;

namespace Ws2812LedController.HueApi;

public interface IJsonOptionHelper
{
    private static JsonSerializerOptions? _globalJsonOptions;
    protected static JsonSerializerOptions GlobalJsonOptions
    {
        get
        {
            if (_globalJsonOptions == null)
            {
                _globalJsonOptions = new JsonSerializerOptions();
                _globalJsonOptions.Converters.Add(new ColorConverter());
                _globalJsonOptions.Converters.Add(new JsonStringEnumConverter());
            }
            return _globalJsonOptions;
        }
    }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ws2812LedController.Core.Model;

[JsonConverter(typeof(StringEnumConverter))]
public enum Edge
{
    None,
    Start,
    End
}
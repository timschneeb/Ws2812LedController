using System.Text.Json.Serialization;

namespace Ws2812LedController.HueApi.Serializable;

public class HueDescriptor
{ 
    [JsonPropertyName("name")]
    public string Name { init; get; }
    [JsonPropertyName("lights")]
    public int Lights { init; get; }
    [JsonPropertyName("protocol")]
    public string Protocol { init; get; }
    [JsonPropertyName("modelid")]
    public string ModelId { init; get; }
    [JsonPropertyName("type")]
    public string Type { init; get; }
    [JsonPropertyName("mac")]
    public string Mac { init; get; }
    [JsonPropertyName("version")]
    public string Version { init; get; } 
}
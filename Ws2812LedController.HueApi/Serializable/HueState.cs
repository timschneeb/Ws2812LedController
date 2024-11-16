using System.Text.Json.Serialization;

namespace Ws2812LedController.HueApi.Serializable;

public class HueState
{
    [JsonPropertyName("on")]
    public bool? IsPowered { set; get; } = null;
    [JsonPropertyName("bri")]
    public byte? Brightness { set; get; } = null;
    [JsonPropertyName("bri_inc")]
    public byte? BrightnessIncrease { set; get; } = null;
    [JsonPropertyName("transitiontime")]
    public int? TransitionTime { set; get; } = null;
    
    [JsonPropertyName("alert")]
    public string? Alert { set; get; } = null;
    
    [JsonPropertyName("ct")]
    public int? ColorTemperature { set; get; } = null;
    
    [JsonPropertyName("hue")]
    public int? Hue { set; get; } = null;
    [JsonPropertyName("sat")]
    public int? Saturation { set; get; } = null;
    
    [JsonPropertyName("xy")]
    public float[]? Xy { set; get; } = null;
    
    [JsonPropertyName("colormode")]
    public ColorMode? ColorMode { set; get; } = null;
}
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Models;

public class PropertyRow
{
    public string Name { init; get; }
    public object Value { set; get; }
    
    [JsonIgnore]
    public Type Type { init; get; }
    [JsonIgnore]
    public string Group { init; get; }
}
using Avalonia.Collections;
using Newtonsoft.Json;

namespace Ws2812RealtimeDesktopClient.Models;

public class PresetEntry
{
    public PresetEntry(string name)
    {
        Name = name;
    }

    public string Name { set; get; }

    public EffectAssignment[]? Effects { set; get; }

    [JsonIgnore] public string Description
    {
        get
        {
            var multi = Effects?.Length == 1 ? $"1 effect" : $"{Effects?.Length ?? 0} effects";
            var segments = string.Empty;
            Effects?.ToList().ForEach(x => segments += $"{x.SegmentName}, ");
            return $"{multi}; {segments.TrimEnd(',')}";
        }
    }
}
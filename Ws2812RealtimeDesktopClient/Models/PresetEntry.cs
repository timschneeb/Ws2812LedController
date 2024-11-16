using Avalonia.Collections;
using Newtonsoft.Json;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Models;

public class PresetEntry : ViewModelBase
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
            return $"{multi}; Segments: {segments[..^2]}";
        }
    }
    
    public void UpdateFromViewModel()
    {
        RaisePropertyChanged(nameof(Name));
        RaisePropertyChanged(nameof(Effects));
    }
}
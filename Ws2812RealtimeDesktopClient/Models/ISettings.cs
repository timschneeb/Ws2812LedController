using Avalonia.Media;
using Config.Net;
using Ws2812RealtimeDesktopClient.ViewModels;

namespace Ws2812RealtimeDesktopClient.Models;

public interface ISettings
{
    SegmentEntry[]? Segments { set; get; }
    EffectAssignment[]? ReactiveEffectAssignments { set; get; }
    PaletteEntry[]? Palettes { set; get; }
    
    [Option(DefaultValue = "Light")]
    string Theme { set; get; }
    [Option(DefaultValue = false)]
    bool UseCustomAccentColor { set; get; }
    Color CustomAccentColor { set; get; }
    string IpAddress { set; get; }
    int StripWidth { set; get; }
}
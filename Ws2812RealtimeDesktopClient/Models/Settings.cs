using Avalonia.Media;

namespace Ws2812RealtimeDesktopClient.Models;

public class Settings
{
    public SegmentEntry[]? Segments { set; get; }
    public EffectAssignment[]? ReactiveEffectAssignments { set; get; }
    public PaletteEntry[]? Palettes { set; get; }
    public string Theme { set; get; }
    public bool UseCustomAccentColor { set; get; }
    public Color CustomAccentColor { set; get; }
    public string IpAddress { set; get; }
    public int StripWidth { set; get; }

    public Settings()
    {
        Theme = "Light";
        UseCustomAccentColor = false;
    }
}
namespace Ws2812RealtimeDesktopClient.Models;

public class EffectAssignment
{
    public string SegmentName { set; get; }
    public string EffectName { set; get; }

    public override string ToString()
    {
        return SegmentName;
    }
}
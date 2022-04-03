using System.Drawing;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.PowerEffects;

public class NullPowerEffect : BasePowerEffect
{
    public override string Description => "Turn LEDs instantly on/off when toggling power states";
    public override int Speed { get; set; } = 100;
    public override bool IsSingleShot => true;

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var color = TargetState == PowerState.On ? Color.FromArgb(0,0,0,0) : Color.Black;
        segment.Clear(color, layer);
        End();
        return Speed;
    }
}
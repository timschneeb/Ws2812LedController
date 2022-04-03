using System.Drawing;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core.Effects.PowerEffects;

public class FadePowerEffect : BasePowerEffect
{
    public override string Description => "Fade LEDs in/out when toggling power states";
    public override int Speed { get; set; } = 1000;
    public override bool IsSingleShot => true;
    public int FadeStep { get; set; } = 4;
    
    private int _stepCounter = 0;
    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var lum = _stepCounter;
        if(lum > 255) 
        {
            lum = 255;
            _stepCounter = 0;
            CancellationMethod?.NextCycle();
            End();
        }

        var color = ColorBlend.Blend(TargetState != PowerState.On ? Color.FromArgb(0,0,0,0) : Color.Black,
                                     TargetState == PowerState.On ? Color.FromArgb(0,0,0,0) : Color.Black, (byte)lum);
        segment.Clear(color, layer);

        _stepCounter += FadeStep;

        return Speed / 128;
    }
}
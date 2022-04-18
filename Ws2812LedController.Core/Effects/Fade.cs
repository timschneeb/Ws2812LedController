using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core.Effects;

public class Fade : BaseEffect
{
    public override string Description => "Fades the LEDs between two colors";
    public override int Speed { get; set; } = 3000;
    
    public FadeMode FadeMode { set; get; } = FadeMode.InOut;
    public Color StartColor { set; get; } = Color.Black;
    public Color EndColor { set; get; } = Color.White;

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
            if (FadeMode == FadeMode.In)
            {
                lum = 255;
                _stepCounter = 0;
                CancellationMethod?.NextCycle();
            }
            else
            {
                lum = 511 - lum; // lum = 0 -> 255 -> 0
            }
        }

        var color = ColorBlend.Blend(StartColor, EndColor, (byte)lum);
        segment.Clear(color, layer);

        _stepCounter += 4;
        if(_stepCounter > 511)
        {
            _stepCounter = 0;
            CancellationMethod?.NextCycle();
        }

        return Speed / 128;
    }
}
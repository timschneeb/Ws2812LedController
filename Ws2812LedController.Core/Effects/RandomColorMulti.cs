using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class RandomColorMulti : BaseEffect
{
    public override string Description => "Lights every LED in a random color. Changes all LED at the same time to new random colors.";
    public override int Speed { get; set; } = 1000;
    

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        for(var i = 0; i < segment.Width; i++) 
        {
            segment.SetPixel(i, ColorWheel.RandomColor(), layer);
        }
        
        CancellationMethod?.NextCycle();
        return Speed;
    }
}
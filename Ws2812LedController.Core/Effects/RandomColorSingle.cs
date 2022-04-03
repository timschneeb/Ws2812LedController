using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class RandomColorSingle : IEffect
{
    public override string Description => "Lights every LED in a random color. Changes one random LED after the other to another random color.";
    public override int Speed { get; set; } = 100;
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (Frame == 0)
        {
            for(var i = 0; i < segment.Width; i++) 
            {
                segment.SetPixel(i, ColorWheel.RandomColor(), layer);
            }
        }
        else
        {
            segment.SetPixel(0 + Random.Shared.Next(0, segment.Width - 1), ColorWheel.RandomColor(), layer);
        }
        
        CancellationMethod?.NextCycle();
        return Speed;
    }
}
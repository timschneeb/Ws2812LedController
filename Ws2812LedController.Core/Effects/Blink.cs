using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class Blink : BaseEffect
{
    public override string FriendlyName => "Blink";
    public override string Description => "Alternate between two colors";
    public override int Speed { get; set; } = 2000;
    
    public bool Strobe { get; set; } = false;
    public Color ColorA { get; set; } = Color.Black;
    public Color ColorB { get; set; } = Color.White;
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if ((Frame & 1) != 0)
        {
            segment.Clear(ColorA, layer);
            CancellationMethod?.NextCycle();
            return Strobe ? Speed - 20 : (Speed / 2);
        }
        else
        {
            segment.Clear(ColorB, layer);
            return Strobe ? 20 : (Speed / 2);
        }
    }
}
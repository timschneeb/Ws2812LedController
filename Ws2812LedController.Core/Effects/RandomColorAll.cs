using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class RandomColorAll : IEffect
{
    public override string Description => "Lights all LEDs in one random color up. Then switches them to the next random color.";
    public override int Speed { get; set; } = 1000;
    

    private byte _previousColorIndex = 0;
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    { 
        var nextColorId = _previousColorIndex = ColorWheel.NextRandomIndex(_previousColorIndex);
        segment.Clear(ColorWheel.ColorAtIndex(nextColorId), layer);
        
        CancellationMethod?.NextCycle();

        return Speed;
    }
}
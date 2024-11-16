using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class Rainbow : BaseEffect
{
    public override string FriendlyName => "Rainbow";
    public override string Description => "Cycles all LEDs at once through a rainbow.";
    public override int Speed { get; set; } = 1000;
    

    private byte _stepCounter = 0;

    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var color = ColorWheel.ColorAtIndex(_stepCounter);

        _stepCounter = (byte)((_stepCounter + 1) & 0xFF);

        if (_stepCounter == 0)
        {
            CancellationMethod?.NextCycle();
        }

        segment.Clear(color, layer);
        
        return Speed / 256;
    }
}
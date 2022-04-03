using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class MultiStrobe : IEffect
{
    public override string Description => "Strobe effect with different strobe count and pause, controlled by speed";
    public override int Speed { get; set; } = 1000;
    
    public Color ColorA { get; set; } = Color.Blue;
    public Color ColorB { get; set; } = Color.BlueViolet;
    
    private int _stepCounter = 0;
    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        segment.Clear(ColorA, layer);

        var delay = 200 + (9 - (Speed % 10)) * 100;
        var count = 2 * (Speed / 100 + 1);
        if (_stepCounter < count)
        {
            if ((_stepCounter & 1) == 0)
            {
                segment.Clear(ColorB, layer);
                delay = 20;
            }
            else
            {
                delay = 50;
            }
        }
        
        _stepCounter = (_stepCounter + 1) % (count + 1);
        if (_stepCounter == 0)
        {
            CancellationMethod?.NextCycle();
        }
        return delay;
    }
}
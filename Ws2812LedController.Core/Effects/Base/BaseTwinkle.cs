using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Base;

public abstract class BaseTwinkle : IEffect
{
    public override int Speed { get; set; } = 100;
    

    protected Color _colorForeground = Color.White;
    protected Color _colorBackground = Color.Black;

    private int _stepCounter = 0;

    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (_stepCounter == 0)
        {
            segment.Clear(_colorBackground, layer);
            
            var minLeds = (segment.Width / 4) + 1;
            _stepCounter = Random.Shared.Next(minLeds, minLeds * 2);
            CancellationMethod?.NextCycle();
        }

        segment.SetPixel(Random.Shared.Next(segment.Width), _colorForeground, layer);

        _stepCounter--;
        return (Speed / segment.Width);
    }
}
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Alternate;

public class RandomAlternate : BaseAlternate
{
    public override string FriendlyName => "Alternate (random)";
    public override string Description => "Random colored pixels running";
    private byte _randomLedIndex = 0;
    
    public override void Reset()
    {
        _randomLedIndex = 0;
        base.Reset();
    }

    protected override Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var size = 1 << (byte)Size;
        if((StepCounter) % size == 0) 
        {
            _randomLedIndex = ColorWheel.NextRandomIndex(_randomLedIndex);
        }

        _colorA = _colorB = ColorWheel.ColorAtIndex(_randomLedIndex);
        return base.PerformFrameAsync(segment, layer);
    }
}
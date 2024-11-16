using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Base;

public abstract class BaseFlashChase : BaseEffect
{
    public override int Speed { get; set; } = 1000;
    
    public bool Reverse { get; set; } = false;
    public int FlashCount { get; set; } = 4;

    protected Color _colorForeground = Color.White;
    protected Color _colorBackground = Color.BlueViolet;

    private int _stepCounter = 0;
    protected byte RandomLedIndex = 0;

    public override void Reset()
    {
        _stepCounter = 0;
        RandomLedIndex = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var flashStep = Frame % ((FlashCount * 2) + 1);

        if (flashStep < (FlashCount * 2))
        {
            var color = (flashStep % 2 == 0) ? _colorForeground : _colorBackground;
            var n = _stepCounter;
            var m = (_stepCounter + 1) % segment.Width;
            if (Reverse)
            {
                segment.SetPixel(segment.RelEnd - n, color, layer);
                segment.SetPixel(segment.RelEnd - m, color, layer);
            }
            else
            {
                segment.SetPixel(n, color, layer);
                segment.SetPixel(m, color, layer);
            }
            return 30;
        }
        else
        {
            _stepCounter = (_stepCounter + 1) % segment.Width;
            if (_stepCounter == 0)
            {
                RandomLedIndex = ColorWheel.NextRandomIndex(RandomLedIndex);
                CancellationMethod?.NextCycle();
            }
        }
        return (Speed / segment.Width);
    }
}
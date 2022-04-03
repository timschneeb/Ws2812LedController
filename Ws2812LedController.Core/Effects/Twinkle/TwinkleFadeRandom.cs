using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Twinkle;

public class TwinkleFadeRandom : BaseTwinkleFade
{
    public override string Description => "Blink several LEDs on, fading out";

    public Color BackgroundColor
    {
        get => _colorBackground;
        set => _colorBackground = value;
    }
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        _colorForeground = ColorWheel.RandomColor();
        return await base.PerformFrameAsync(segment, layer);
    }
}
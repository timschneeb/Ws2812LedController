using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Twinkle;

public class TwinkleRandom : BaseTwinkle
{
    public override string FriendlyName => "Twinkle (random)";
    public override string Description => "Blink several LEDs in random colors on, reset, repeat";
    
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
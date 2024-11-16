using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Chase;

public class RandomFlashChase : BaseFlashChase
{
    public override string FriendlyName => "Flash chase (random)";

    public override string Description => "Flashes running on random background";
    
    public Color ForegroundColor
    {
        get => _colorForeground;
        set => _colorForeground = value;
    }

    protected override Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        _colorBackground = ColorWheel.ColorAtIndex(RandomLedIndex);
        return base.PerformFrameAsync(segment, layer);
    }
}
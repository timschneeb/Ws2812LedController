using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Firework;

public class RandomFirework : BaseFirework
{
    public override string FriendlyName => "Firework (random)";
    public override string Description => "Firework sparks with random colors";
    
    private byte _lastRandomColor = 0;

    public Color BackgroundColor
    {
        get => _colorBackground;
        set => _colorBackground = value;
    }
    
    public override void Reset()
    {
        _lastRandomColor = 0;
        base.Reset();
    }
    
    protected override Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        _lastRandomColor = ColorWheel.NextRandomIndex(_lastRandomColor);
        _colors = new[]{ ColorWheel.ColorAtIndex(_lastRandomColor) };
        return base.PerformFrameAsync(segment, layer);
    }
}
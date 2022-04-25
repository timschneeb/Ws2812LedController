using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Chase;

public class RandomChase : BaseChase
{
    public override string FriendlyName => "Random chase";
    public override string Description => "Custom color running on random background";
    private byte _lastRandomColor = 0;

    public Color Color
    {
        set =>  _colorA = _colorB = value;
        get => _colorA;
    }
    
    public RandomChase()
    {
        // Set appropriate default value
        _colorA = _colorB = Color.White;
    }
    
    public override void Reset()
    {
        _lastRandomColor = 0;
        base.Reset();
    }
    
    protected override Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        _lastRandomColor = ColorWheel.NextRandomIndex(_lastRandomColor);
        _colorBackground = ColorWheel.ColorAtIndex(_lastRandomColor);
        return base.PerformFrameAsync(segment, layer);
    }
}
using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Chase;

public class TheatreRainbowChase : BaseTricolorChase
{
    public override string Description => "Theatre-style crawling lights with rainbow effect";

    public Color Color { get; set; }

    private byte _colorIndex;
    
    public override void Reset()
    {
        _colorIndex = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        _colorIndex = (byte)((_colorIndex + 1) & 0xFF); 
        _colorA = ColorWheel.ColorAtIndex(_colorIndex);
        _colorB = _colorC = Color;
        return await base.PerformFrameAsync(segment, layer);
    }
}
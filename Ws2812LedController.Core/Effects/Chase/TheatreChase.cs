using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Chase;

public class TheatreChase : BaseTricolorChase
{
    public override string FriendlyName => "Theatre chase";
    public override string Description => "Theatre-style crawling lights";

    public Color ColorA
    {
        get => _colorA;
        set => _colorA = value;
    }
    
    public Color ColorB
    {
        get => _colorB;
        set => _colorB = value;
    }
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        _colorC = _colorB;
        return await base.PerformFrameAsync(segment, layer);
    }
}
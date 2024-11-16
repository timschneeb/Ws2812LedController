using System.Drawing;
using System.Net;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class StaticLegacy : BaseEffect
{
    public override string FriendlyName => "Static (Legacy)";
    public override string Description => "Static color (no transitions)";
    public override int Speed { get; set; } = 100;
    public Color Color { get; set; } = Color.White;

    private Color? _previousFrame;
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        /* Frequent redraws are unnecessary */
        if((Frame % 100) != 0 && _previousFrame == Color)
        {
            return Speed;
        }
        
        segment.Clear(Color, layer);
        CancellationMethod.NextCycle();

        _previousFrame = Color;
        return Speed;
    }
    
    
}
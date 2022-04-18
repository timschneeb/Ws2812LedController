using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class FadeTo : BaseEffect
{
    public override string Description => "Fade from current to new color";
    public override bool IsSingleShot => true;
    public override int Speed { get; set; } = 20;
    
    public FadeRate FadeRate { get; set; } = FadeRate.Slow;
    public Color Color { get; set; } = Color.Black;
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (FadeOut.DoFrame(segment, Color, FadeRate, layer))
        {
            End();
        }
        return Speed;
    }
}
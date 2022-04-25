using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Sparkle;

public class HyperSparkle : BaseEffect
{
    public override string FriendlyName => "Sparkle (hyper)";

    public override string Description => "Like flash sparkle. With more flash.";
    public override int Speed { get; set; } = 1500;
    
    public SizeOption Size { get; set; } = SizeOption.Small;
    public Color BackgroundColor { set; get; } = Color.BlueViolet;

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        segment.Clear(BackgroundColor, layer);
        
        for(var i = 0; i < 8; i++) {
            segment.Fill(Random.Shared.Next(segment.Width - (byte)Size + 1), (byte)Size, Color.White, layer);
        }

        CancellationMethod?.NextCycle();
        return (Speed / 32);
    }
}
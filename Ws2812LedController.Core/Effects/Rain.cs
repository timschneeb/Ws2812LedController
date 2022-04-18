using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class Rain : BaseEffect
{
    public override string Description => "Combination of the Fireworks effect and the running effect to create rain";
    public override int Speed { get; set; } = 1000;
    
    public bool Reverse { get; set; } = false;
    public SizeOption Size { get; set; } = SizeOption.Small;
    public FadeRate FadeRate { get; set; } = FadeRate.None;

    public Color BackgroundColor { get; set; } = Color.Black;
    public Color[] Colors { get; set; } =
    {
        Color.CornflowerBlue,
        Color.DodgerBlue,
        Color.SteelBlue,
        Color.RoyalBlue
    };
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var rainColor = Colors[Random.Shared.Next(Colors.Length)];
        
        // run the fireworks effect to create a "raindrop"
        Fireworks.DoFrame(segment, rainColor, BackgroundColor, FadeRate, Size, false, layer);
        
        // shift everything two pixels
        if(Reverse) 
        {
            segment.CopyPixels(0, 2, segment.Width - 2, layer);
        } else 
        {
            segment.CopyPixels(2, 0, segment.Width - 2, layer);
        }
        CancellationMethod?.NextCycle();

        return (Speed / 16);
    }
}
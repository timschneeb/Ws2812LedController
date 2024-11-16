using System.Diagnostics;
using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class FireFlicker : BaseEffect
{
    public override string FriendlyName => "Fire flicker";

    public override string Description => "Random flickering";
    public override int Speed { get; set; } = 1000;
    
    /* 1 = Intense, 3 = Normal, 6 = Soft */
    public int Intensity { get; set; } = 3;
    public Color Color { set; get; } = Color.OrangeRed;

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var w = Color.A;
        var r = Color.R;
        var g = Color.G;
        var b = Color.B;
        var lum = Math.Max(w, Math.Max(r, Math.Max(g, b))) / Intensity;
        for (ushort i = 0; i <= segment.RelEnd; i++)
        {
            var flicker = Random.Shared.Next(lum);
            segment.SetPixel(i, Color.FromArgb(Math.Max(w - flicker, 0), Math.Max(r - flicker, 0), Math.Max(g - flicker, 0), Math.Max(b - flicker, 0)), layer);
        }
        
        CancellationMethod?.NextCycle();
        return (Speed / segment.Width);
    }
}
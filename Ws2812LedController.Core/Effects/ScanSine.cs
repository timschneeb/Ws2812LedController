using System.Drawing;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class ScanSine : BaseEffect
{
    public override string FriendlyName => "Scan (sinewave)";
    public override string Description => "Runs a block of pixels back and forth. Interpolated using sine waves";
    public override int Speed { get; set; } = 20 /* bpm */;
    public byte FadeSpeed { get; set; } = 20;
    public Color ScanColor { set; get; } = Color.White;
    public bool Transparent { set; get; } = true;
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ FadeSpeed), Transparent),layer);
        }
        
        var loc = Beat.beatsin16((ushort)Speed, 0, (ushort)(segment.Width - 1));
        segment.SetPixel(loc, Math8.AddColor(segment.PixelAt(loc, layer), ScanColor), layer);

        if (loc <= 0)
        {
            CancellationMethod.NextCycle();
        }
        
        return 1000/60;
    }
}
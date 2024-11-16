using System.Drawing;
using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.AudioReactive.Effects.Fft;

public class DjLightReactiveEffect : BaseAudioReactiveEffect
{
    public override string FriendlyName => "DJ Light";
    public override string Description => "An effect emanating from the center to the edges";
    public override int Speed { set; get; } = 1000 / 60;
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (Frame == 0)
        {
            segment.Clear(Color.Black, layer);
        }
        
        var count = NextSample();
        if (count < 1)
        {
            goto NEXT_FRAME;
        }
        
        var mid = segment.Width / 2;

        var fadeBy = FftBins[1 * 4].Map(0, 255, 255, 10);
        segment.SetPixel(mid, Color.FromArgb((byte)(FftBins[16] / 2), (byte)(FftBins[5] / 2), (byte)(FftBins[0] / 2)), layer);
        segment.SetPixel(mid, Scale.nscale8x3(segment.PixelAt(mid, layer), (short)(255 - /*fadeBy*/ (byte)fadeBy)),layer);

        //move to the left
        for (var i = segment.Width - 1; i > mid; i--)
        {
            segment.SetPixel(i, segment.PixelAt(i - 1, layer), layer);
        }
        // move to the right
        for (var i = 0; i < mid; i++)
        {
            segment.SetPixel(i, segment.PixelAt(i + 1, layer), layer);
        }

        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
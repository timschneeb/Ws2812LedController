using Ws2812LedController.AudioReactive.Dsp;
using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.AudioReactive.Utils;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.AudioReactive.Effects.Fft;

public class NoiseMoveReactiveEffect : BaseAudioReactiveEffect, IHasFftBinSelection
{
    public override string Description => "	Using perlin noise as movement for different frequency bins";
    public override int Speed { set; get; } = 1000 / 60;
    public int AnimationSpeed { set; get; } = (int)(128);
    public FftCBinSelector FftCBinSelector { set; get; } = new(0,4);

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var count = NextSample();


        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), 255 - /*fadeBy*/ 224 /*64*/),layer);
        }
        
        var millis = Time.Millis();
        for (var i=FftCBinSelector.Start; i<=FftCBinSelector.End; i++) {                         // How many active bins are we using.
            /* (uint)(millis*AnimationSpeed+i*50000) */
            var loc = Noise.inoise16((uint)(FftAvg[i]*AnimationSpeed+i*50000), (uint)(millis*AnimationSpeed));   // Get a new pixel location from moving noise.

            loc = loc.Map(7500,58000,0,segment.Width-1);               // Map that to the length of the strand, and ensure we don't go over.
            loc = (ushort)(loc % (segment.Width - 1));                           // Just to be bloody sure.

            var v = (byte)(FftAvg[i].Map(0, 255, 0, 255, true));
            segment.SetPixel(loc, ColorBlend.Blend(segment.PixelAt(loc, layer), Conversions.ColorFromHSV(i.Map(FftCBinSelector.Start, FftCBinSelector.End, 0, 255), 255, 255), v), layer);

        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
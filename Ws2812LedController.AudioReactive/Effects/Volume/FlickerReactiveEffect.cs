using System.Drawing;
using Ws2812LedController.AudioReactive.Dsp;
using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.AudioReactive.Effects.Volume;

public class FlickerReactiveEffect : BaseAudioReactiveEffect, IHasOptionalFftBinSelection
{
    public override string Description => "Flicker LEDs based on volume or FFT peaks";
    public override int Speed { set; get; } = 1000 / 60;
    public FftCBinSelector? FftCBinSelector { set; get; }

    public Color Color { set; get; } = Color.DarkRed;
    public double Threshold { set; get; } = 500;
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var count = NextSample();
        if (count < 1)
        {
            goto NEXT_FRAME;
        }

        var isPeak = FftCBinSelector == null ? IsPeak(Threshold) : IsFftPeak(FftCBinSelector, Threshold);
        
        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), 255 - /*fadeBy*/ 45),layer);
        } 
        
        if (isPeak)
        {
            segment.Clear(Color, layer);
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.AudioReactive.Effects.Fft;

public class FreqMapReactiveEffect : BaseAudioReactiveEffect, IHasFrequencyLimits
{
    public override string FriendlyName => "Frequency map";

    public override string Description => "Map major FFT peaks to LED strip length";
    public override int Speed { set; get; } = 1000 / 60;
    public int StartFrequency { set; get; } = 93;
    public int EndFrequency { set; get; } = 5120;
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var count = NextSample();
        if (count < 1)
        {
            goto NEXT_FRAME;
        }

        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), 255 - /*fadeBy*/ 20),layer);
        }
        
        if (FftMajorPeak[0] <= 0 || FftMajorPeak[1] <= 0)
        {
            goto NEXT_FRAME;
        }
        
        var locn = (int)((Math.Log10(FftMajorPeak[0]) - Math.Log10(StartFrequency)) * (float)segment.Width/(Math.Log10(EndFrequency)-Math.Log10(StartFrequency))); 

        if (locn >= segment.Width) locn = segment.Width-1;
        if (locn < 0)
        {
            goto NEXT_FRAME;
        }
        
        var pixCol = (byte)((Math.Log10((int)FftMajorPeak[0]) - Math.Log10(StartFrequency)) * 255.0/(Math.Log10(EndFrequency)-Math.Log10(StartFrequency)));   // Scale log10 of frequency values to the 255 colour index.
        var bright = (byte)255;//(byte)((int)FftMajorPeak[1]>>4);
        
        
        var color = ColorBlend.Blend(segment.PixelAt(locn, layer), ColorWheel.ColorAtIndex(pixCol, bright), 100);
        segment.SetPixel(locn, color, layer);
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
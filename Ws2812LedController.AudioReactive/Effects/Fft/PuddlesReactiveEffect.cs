using Ws2812LedController.AudioReactive.Dsp;
using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.AudioReactive.Effects.Fft;

public class PuddlesReactiveEffect : BaseAudioReactiveEffect, IHasFftBinSelection, IHasPeakDetection
{
    public override string Description => "Blast colored puddles based on volume";
    public override int Speed { set; get; } = 1000 / 60;
    public byte AnimationSpeed { set; get; } = 32;
    public byte Intensity { set; get; } = 4;
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Ocean);
    public FftCBinSelector FftCBinSelector { set; get; } = new(0);
    public double Threshold { set; get; } = 128;
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var count = NextSample();
        if (count < 1)
        {
            goto NEXT_FRAME;
        }
        
        ushort size = 0;
        var fadeVal = AnimationSpeed.Map(0,255, 40, 255);
        var pos = Random.Shared.Next(segment.Width); // Set a random starting position.

        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ fadeVal)),layer);
        }

        if (IsFftPeak(FftCBinSelector, Threshold))
        {
            size = (ushort)(FftCBinSelector.Mean(FftCompressedBins) * Intensity / 256.0 / 8 + 1); // Determine size of the flash based on the volume.
            if (pos + size >= segment.Width)
            {
                size = (ushort)(segment.Width - pos);
            }
        }

        for (var i = 0; i < size; i++)
        { 
            // Flash the LED's.
            segment.SetPixel(pos + i, Palette.ColorFromPalette((byte)Time.Millis(), 255, TBlendType.None), layer);
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
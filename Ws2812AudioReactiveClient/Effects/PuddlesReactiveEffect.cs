using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812AudioReactiveClient.FastLedCompatibility;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Effects;

public class PuddlesReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Blast colored puddles based on volume";
    public override int Speed { set; get; } = 1000 / 60;
    public byte AnimationSpeed { set; get; } = 32;
    public byte Intensity { set; get; } = 4;
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Ocean);
    public FftCBinSelector FftCBinSelector { set; get; } = new(0);
    public int MaxVolume { set; get; } = 128;
    
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

        if (IsFftPeak(FftCBinSelector, MaxVolume))
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
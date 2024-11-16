using System.Drawing;
using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.AudioReactive.Effects.Fft;

public class BinMapReactiveEffect : BaseAudioReactiveEffect
{
    public override string FriendlyName => "Bin map";
    public override string Description => "Map FFT bins to LED segment";
    public override int Speed { set; get; } = 1000 / 60;
    [ValueRange(0, 255)]
    public int FirstBin { set; get; } = 2;
    [ValueRange(0, 255)]
    public int LastBin { set; get; } = 100;
    [ValueRange(0, 255)]
    public int SoundSquelch { set; get; } = 50;
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Lava);

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

        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            //segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ FadeStrength)),layer);
        }
        
        var maxVal = 1024.0; // Kind of a guess as to the maximum output value per combined logarithmic bins.

        for (int i = 0; i < segment.Width; i++)
        {

            var startBin = FirstBin + i * LastBin / segment.Width; // This is the START bin for this particular pixel.
            var endBin = FirstBin + (i + 1) * LastBin / segment.Width; // This is the END bin for this particular pixel.

            double sumBin = 0;

            for (var j = startBin; j <= endBin; j++)
            {
                sumBin += (FftBins[j] < SoundSquelch * 6) ? 0 : FftBins[j]; // We need some sound temporary squelch for fftBin, because we didn't do it for the raw bins in audio_reactive.h
            }

            sumBin = sumBin / (endBin - startBin + 1); // Normalize it.
            sumBin = sumBin * (i + 5) / (endBin - startBin + 5); // Disgusting frequency adjustment calculation. Lows were too bright. Am open to quick 'n dirty alternatives.

            sumBin = sumBin * 8; // Need to use the 'log' version for this.

            if (sumBin > maxVal)
            {
                sumBin = maxVal; // Make sure our bin isn't higher than the max . . which we capped earlier.
            }

            var bright = (byte)sumBin.Map(0, maxVal, 0, 255, true); // Map the brightness in relation to maxVal and crunch to 8 bits.

            var color = ColorBlend.Blend(Color.Black, Palette.ColorFromPalette((byte)(i * 8 + Time.Millis() / 50), 255, TBlendType.None), bright);
            segment.SetPixel(i, color, layer);
        }

        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
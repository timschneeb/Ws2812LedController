using System.Drawing;
using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.AudioReactive.Effects.Fft;

public class BlurzReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Flash a FFT bin per frame and then fade out";
    public override int Speed { set; get; } = 1000 / 60;
    public byte BlurIntensity { set; get; } = 255;
    public byte FadeStrength { set; get; } = 10;
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Lava);

    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }

    private int _stepCounter;
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
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ FadeStrength)),layer);
        }

        var loc = Random.Shared.Next(segment.Width);
        var color = ColorBlend.Blend(Color.Black, Palette.ColorFromPalette((byte)(FftCompressedBins[_stepCounter] * 240 / (segment.Width - 1)), 255, TBlendType.None), (byte)FftCompressedBins[_stepCounter]);
        segment.SetPixel(loc, color, layer);

        _stepCounter++;
        _stepCounter %= 16;

        Blur.blur1d(ref segment, BlurIntensity, layer);

        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
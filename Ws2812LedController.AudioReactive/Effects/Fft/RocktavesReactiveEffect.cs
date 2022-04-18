using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.AudioReactive.Utils;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.AudioReactive.Effects.Fft;

public class RocktavesReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Colors the same for each note between octaves, with sine wave going back and forth";
    public override int Speed { set; get; } = 1000 / 60;

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
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), 255 - /*fadeBy*/ 64),layer);
        }
        
        var frTemp = FftMajorPeak[0];
        byte octCount = 0; 
        var volTemp = FftMajorPeak[1].Map(3000, 8000, 100, 255);

        while (frTemp > 249) {
            octCount++;    // This should go up to 5.
            frTemp /= 2;
        }

        frTemp -= 132;                             // This should give us a base musical note of C3
        frTemp = Math.Abs(frTemp * 2.1);           // Fudge factors to compress octave range starting at 0 and going to 255;
        
        var loc = Beat.beatsin16((ushort)(8 + octCount * 4) , 0, (ushort)(segment.Width - 1), 0, (ushort)(octCount * 8));
        var newColor = Conversions.ColorFromHSV((byte)frTemp, 255, volTemp);
        segment.SetPixel(loc, Math8.AddColor(segment.PixelAt(loc, layer), newColor), layer);
      
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
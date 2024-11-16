using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.AudioReactive.Effects.Volume;

public class PlasmoidReactiveEffect : BaseAudioReactiveEffect
{
    public override string FriendlyName => "Plasmoid";
    public override string Description => "Sine wave based plasma";
    public override int Speed { set; get; } = 1000 / 60;
    public byte Intensity { set; get; } = 128;

    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Lava);

    private short _thisPhase = 0;
    private short _thatPhase = 0;

    public override void Reset()
    {
        _thisPhase = _thatPhase = 0;
        base.Reset();
    }

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
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ 64)),layer);
        }
 
        _thisPhase += Beat.beatsin8(6,0,8).Map(0, 8, -4, 4); // You can change direction and speed individually.
        _thatPhase += Beat.beatsin8(7,0,8).Map(0, 8, -4, 4); // Two phase values to make a complex pattern. By Andrew Tuline.

        for (var i = 0; i < segment.Width; i++)
        {
            // For each of the LED's in the strand, set a brightness based on a wave as follows.
            var thisbright = Math8.cubicwave8((byte)((i * 13) + _thisPhase)) / 2;
            thisbright = (int)(thisbright + Math.Cos((i * 117) + _thatPhase) / 2.0); // Let's munge the brightness a bit and animate it all with the phases.
            var colorIndex = thisbright;
            
            if (SampleAgc * Intensity / 128.0 < thisbright)
            {
                thisbright = 0;
            }

            segment.SetPixel(i, Palette.ColorFromPalette((byte)colorIndex, (byte)thisbright, TBlendType.None), layer);
        }

        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
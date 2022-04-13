using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using Ws2812AudioReactiveClient.FastLedCompatibility;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Effects;

public class PixelWaveReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Centered running light with brightness derived from volume levels";
    public override int Speed { set; get; } = 1000 / 60;
    public byte Intensity { set; get; } = 64;
    public int MinPeakMagnitude { set; get; } = 60;
    public int MaxPeakMagnitude { set; get; } = 150;
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Lava);

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (Frame == 0)
        {
            segment.Clear(layer);
        }
        
        var count = NextSample();
        if (count < 1)
        {
            goto NEXT_FRAME;
        }
        
        var tmpSound = /*SampleAvg*/ SampleAgc * Intensity / 64;//(soundAgc) ? sampleAgc : sample;
        Console.WriteLine($"SampleAGC={SampleAgc} ->\ttmpSound={tmpSound}");
        var pixBri = (byte)tmpSound.Map(MinPeakMagnitude, MaxPeakMagnitude, 0, 255, true);
            
        segment.SetPixel(segment.Width / 2, ColorBlend.Blend(Color.Black, Palette.ColorFromPalette((byte)Time.Millis(), 255, TBlendType.None), pixBri), layer);
            
        for (var i = segment.Width - 1; i > segment.Width / 2; i--)
        { 
            // Move to the right.
            segment.SetPixel(i, segment.PixelAt(i - 1, layer), layer);
        }
        for (var i = 0; i < segment.Width / 2; i++)
        { 
            // Move to the left.
            segment.SetPixel(i, segment.PixelAt(i + 1, layer), layer);
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
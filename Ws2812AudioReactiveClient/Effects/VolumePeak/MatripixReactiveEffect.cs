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

public class MatripixReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Simple running light with brightness derived from volume levels";
    public override int Speed { set; get; } = 1000 / 60;
    public byte FramesPerStep { set; get; } = 1;
    public int MinPeakMagnitude { set; get; } = 100;
    public int MaxPeakMagnitude { set; get; } = 8000;
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Lava);
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (Frame == 0)
        {
            segment.Clear(layer);
        }
        
        var count = NextSample();
        if (count < 1 || Frame % FramesPerStep != 0)
        {
            goto NEXT_FRAME;
        }
        
        var tmpSound = SampleAvg.Map(MinPeakMagnitude, MaxPeakMagnitude, 0, 255, true);//(soundAgc) ? sampleAgc : sampleAvg;
        segment.SetPixel(segment.Width - 1, ColorBlend.Blend(Color.Black, Palette.ColorFromPalette((byte)Time.Millis(), 255, TBlendType.None), (byte)tmpSound), layer);
        for (var i = 0; i < segment.Width - 1; i++)
        {
            segment.SetPixel(i, segment.PixelAt(i + 1, layer), layer);
        }

        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
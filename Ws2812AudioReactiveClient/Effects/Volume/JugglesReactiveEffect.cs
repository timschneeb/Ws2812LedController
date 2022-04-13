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

public class JugglesReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Simple scanning light with brightness derived from volume levels";
    public override int Speed { set; get; } = 1000 / 60;
    public int AnimationSpeed { set; get; } = 64;
    public byte Intensity { set; get; } = 32;
    public int MinPeakMagnitude { set; get; } = 100;
    public int MaxPeakMagnitude { set; get; } = 8000;
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Lava);
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var count = NextSample();
        if (count < 1)
        {
            goto NEXT_FRAME;
        }
        
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), 255 - /*fadeBy*/ 31),layer);
        } 
        
        for (var i=0; i<Intensity/32+1; i++)
        {
            var tmpSound = SampleAvg.Map(MinPeakMagnitude, MaxPeakMagnitude, 0, 255, true);//(soundAgc) ? sampleAgc : sampleAvg;
            var color = Palette.ColorFromPalette((byte)(Time.Millis() / 4 + i * 2), (byte)tmpSound, TBlendType.None);
            segment.SetPixel(Beat.beatsin16((ushort)(AnimationSpeed/4+i*2),0,(ushort)(segment.Width-1)), color, layer);
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
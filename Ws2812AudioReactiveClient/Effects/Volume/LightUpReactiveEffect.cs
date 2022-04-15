using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812AudioReactiveClient.FastLedCompatibility;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Effects;

/**
 * Adopted from https://github.com/atuline/FastLED-SoundReactive
 */
public class LightUpReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Light single LEDs based on volume peaks up";
    public override int Speed { set; get; } = 1000 / 60;
    public double Threshold { set; get; } = 1500;
    public int FadeSpeed { set; get; } = 6;
    public int MinPeakMagnitude { set; get; } = 100;
    public int MaxPeakMagnitude { set; get; } = 1000;
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var length = NextSample();
        if (length < 1)
        {
            goto NEXT_FRAME;
        }
        
        var isPeak = IsPeak(Threshold);
        var strength = (byte)SampleAvg.Map(MinPeakMagnitude, MaxPeakMagnitude, 0, 255);

        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ FadeSpeed)),layer);
        }
        
        if (isPeak)
        {
            segment.SetPixel((int)(((_timeSinceStart.ElapsedMilliseconds & 0xFFFF) % (segment.Width-1)) +1), Conversions.ColorFromHSV(strength, 255, strength), layer);
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
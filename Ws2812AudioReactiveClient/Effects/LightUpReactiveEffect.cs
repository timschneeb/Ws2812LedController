using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812AudioReactiveClient.FastLedCompatibility;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
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
    
    private double[] _buffer = new double[1024];
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        NextSample(ref _buffer);
        if (_buffer.Length < 1)
        {
            goto NEXT_FRAME;
        }
        
        var isPeak = IsPeak(0.08);
        var strength = (byte)SampleAvg.Map(0, 0.18, 0, 255);

        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), 255 - /*fadeBy*/ 4),layer);
        } 
        segment.SetPixel(0, Scale.nscale8x3(segment.PixelAt(0, layer), 255 - /*fadeBy*/ 32),layer);
        
        if (isPeak)
        {
            segment.SetPixel(0, Color.FromArgb(0xA9,0xA9,0xA9), layer);
        }

        segment.SetPixel((int)(((_timeSinceStart.ElapsedMilliseconds & 0xFFFF) % (segment.Width-1)) +1), Conversions.ColorFromHSV(strength, 255, strength), layer);
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
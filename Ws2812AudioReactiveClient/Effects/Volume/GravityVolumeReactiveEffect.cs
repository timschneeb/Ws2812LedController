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

public class GravityVolumeReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Volume reactive vu-meter from edge/center with gravity and perlin noise";
    public override int Speed { set; get; } = 1000 / 60;
    public bool Centered { set; get; } = false;
    public bool ColorNoise { set; get; } = true;
    public int AnimationSpeed { set; get; } = 16;
    public byte FadeSpeed { set; get; } = 15;
    public int Intensity { set; get; } = 128; 
    public int MinPeakMagnitude { set; get; } = 100;
    public int MaxPeakMagnitude { set; get; } = 8000;

    public CRGBPalette16 Palette = new(Color.Blue, Color.DarkBlue, Color.DarkSlateBlue, Color.RoyalBlue);
    
    public override void Reset()
    {
        _topLed = 0;
        _gravityCounter = 0;
        base.Reset();
    }

    private int _topLed;
    private int _gravityCounter;
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
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ FadeSpeed)),layer);
        }

        var segmentSampleAvg = (byte)(SampleAvg * Intensity / 128.0).Map(MinPeakMagnitude, MaxPeakMagnitude, 0, 255, true);
        var tempsamp = (int)segmentSampleAvg.Map(0, 255, 0, Centered ? segment.Width / 2 : segment.Width - 1, true); // Keep the sample from overflowing.
        var gravity = (byte)(8 - AnimationSpeed / 32.0);

        for (int i = 0; i < tempsamp; i++)
        {
            var index = !ColorNoise ? (byte)(segmentSampleAvg * 24 + Time.Millis() / 200.0) : 
                Noise.inoise8((ushort)(i*segmentSampleAvg+Time.Millis()), (ushort)(5000+i*segmentSampleAvg));
            
            var color = Palette.ColorFromPalette(index, segmentSampleAvg, TBlendType.LinearBlend);
            segment.SetPixel(Centered ? i + segment.Width / 2 : i, color, layer);
            if (Centered)
            {
                segment.SetPixel(segment.Width / 2 - i - 1, color, layer);
            }
        }

        if (tempsamp >= _topLed)
        {
            _topLed = tempsamp - 1;
        }
        else if (_gravityCounter % gravity == 0)
        {
            _topLed--;
        }

        if (_topLed >= 0)
        {
            var gray = Color.FromArgb(92, 92, 92);
            segment.SetPixel(Centered ? _topLed + segment.Width / 2 : _topLed, gray, layer);
            if (Centered)
            {
                segment.SetPixel(segment.Width / 2 - 1 - _topLed, gray, layer);
            }
        }
        _gravityCounter = (_gravityCounter + 1) % gravity;
        
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
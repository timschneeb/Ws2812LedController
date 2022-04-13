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

public class GravityFreqReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "VU Meter from center. Log of frequency is index to center colour";
    public override int Speed { set; get; } = 1000 / 60;
    public int AnimationSpeed { set; get; } = 16;
    public int Intensity { set; get; } = 2;
    public int StartFrequency { set; get; } = 70;
    public int EndFrequency { set; get; } = 5120;

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
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), 255 - /*fadeBy*/ 128 /*64*/),layer);
        }

        var segmentSampleAvg = (SampleAvg / 2 * Intensity / 255);
        var tempsamp = (int)segmentSampleAvg.Clamp(0, segment.Width / 2); // Keep the sample from overflowing.
        var gravity = (byte)(8 - AnimationSpeed / 32);

        for (int i = 0; i < tempsamp; i++)
        {
            var index = (byte)((Math.Log10((int)FftMajorPeak[0]) - (Math.Log10(EndFrequency) - Math.Log10(StartFrequency))) * 255);
            
            var color = Palette.ColorFromPalette(index, 255, TBlendType.LinearBlend);
            segment.SetPixel(i + segment.Width / 2, color, layer);
            segment.SetPixel(segment.Width / 2 - i - 1, color, layer);
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
            segment.SetPixel(_topLed + segment.Width / 2, gray, layer);
            segment.SetPixel(segment.Width / 2 - 1 - _topLed, gray, layer);
        }
        _gravityCounter = (_gravityCounter + 1) % gravity;
        
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
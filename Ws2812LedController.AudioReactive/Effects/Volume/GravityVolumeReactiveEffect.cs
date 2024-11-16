using System.Drawing;
using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.AudioReactive.Model;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.AudioReactive.Effects.Volume;

public class GravityVolumeReactiveEffect : BaseAudioReactiveEffect, IHasVolumeAnalysis
{
    public override string FriendlyName => "Gravity volume";
    public override string Description => "Volume reactive vu-meter from edge/center with gravity and perlin noise";
    public override int Speed { set; get; } = 1000 / 60;
    public bool Centered { set; get; } = false;
    public bool ColorNoise { set; get; } = true;
    public int AnimationSpeed { set; get; } = 32;
    public byte FadeSpeed { set; get; } = 120;
    public IVolumeAnalysisOption VolumeAnalysisOptions { set; get; } = new FixedVolumeAnalysisOption(100, 8000);

    public CRGBPalette16 Palette { set; get; } = new(Color.Blue, Color.DarkBlue, Color.DarkSlateBlue, Color.RoyalBlue);
    
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

        byte strength = VolumeAnalysisOptions switch
        {
            AgcVolumeAnalysisOption agc => (byte)(SampleAgc * agc.Intensity / 64.0).Clamp(0, 255),
            FixedVolumeAnalysisOption fix => (byte)SampleAvg.Map(fix.MinimumMagnitude, fix.MaximumMagnitude, 0, 255, true),
            _ => 0
        };     
        var tempsamp = (int)strength.Map(0, 255, 0, Centered ? segment.Width / 2 : segment.Width - 1, true); // Keep the sample from overflowing.
        var gravity = (byte)(8 - AnimationSpeed / 32.0);

        for (int i = 0; i < tempsamp; i++)
        {
            var index = !ColorNoise ? (byte)(strength * 24 + Time.Millis() / 200.0) : 
                Noise.inoise8((ushort)(i*strength+Time.Millis()), (ushort)(5000+i*strength));
            
            var color = Palette.ColorFromPalette(index, strength, TBlendType.LinearBlend);
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
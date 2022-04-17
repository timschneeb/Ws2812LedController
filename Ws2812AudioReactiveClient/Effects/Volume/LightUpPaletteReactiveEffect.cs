using System.Diagnostics;
using Ws2812AudioReactiveClient.Effects.Base;
using Ws2812AudioReactiveClient.Model;
using Ws2812AudioReactiveClient.Utils;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Effects.Volume;

/**
 * Adopted from https://github.com/atuline/FastLED-SoundReactive
 */
public class LightUpPaletteReactiveEffect : BaseAudioReactiveEffect, IHasVolumeAnalysis, IHasPeakDetection
{
    public override string Description => "Light single LEDs based on volume peaks up. Use colors from randomized palettes";
    public override int Speed { set; get; } = 1000 / 60;
    public int FadeSpeed { set; get; } = 6;
    public IVolumeAnalysisOption VolumeAnalysisOptions { set; get; } = new AgcVolumeAnalysisOption(128);
    public double Threshold { set; get; } = 1500;

    private readonly CRGBPalette16 _currentPalette = new(CRGBPalette16.Palette.Ocean);
    private CRGBPalette16 _targetPalette = new(CRGBPalette16.Palette.Lava);

    private readonly Stopwatch _timer100 = new();
    private readonly Stopwatch _timer5000 = new();
    private const byte MaxChanges = 24; // Value for blending between palettes.

    public override void Reset()
    {
        _timer100.Reset();
        _timer5000.Reset();
        base.Reset();
    }

    protected override void Begin()
    {
        _timer100.Restart();
        _timer5000.Restart();
        base.Begin();
    }

    protected override void End()
    {
        _timer100.Reset();
        _timer5000.Reset();
        base.End();
    }
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var length = NextSample();
        if (length < 1)
        {
            goto NEXT_FRAME;
        }

        var isPeak = IsPeak(Threshold);
        byte strength = VolumeAnalysisOptions switch
        {
            AgcVolumeAnalysisOption agc => (byte)(SampleAgc * agc.Intensity / 64.0).Clamp(0, 255),
            FixedVolumeAnalysisOption fix => (byte)SampleAvg.Map(fix.MinimumMagnitude, fix.MaximumMagnitude, 0, 255, true),
            _ => 0
        };        
        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ FadeSpeed)),layer);
        } 

        if (isPeak)
        {
            segment.SetPixel((int)(((TimeSinceStart.ElapsedMilliseconds & 0xFFFF) % (segment.Width-1)) +1), _currentPalette.ColorFromPalette(strength, strength, TBlendType.LinearBlend), layer);
        }

        if (_timer100.ElapsedMilliseconds >= 100)
        {
            CRGBPalette16.nblendPaletteTowardPalette(_currentPalette, _targetPalette, MaxChanges);
            _timer100.Restart();
        }

        if (_timer5000.ElapsedMilliseconds >= 5000)
        {
            GenerateNextPalette();
            _timer5000.Restart();
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
    
    private void GenerateNextPalette()
    {
        _targetPalette = new CRGBPalette16(
            Conversions.ColorFromHSV(Random.Shared.Next(0,255), 255, Random.Shared.Next(128,255)), 
            Conversions.ColorFromHSV(Random.Shared.Next(0,255), 255, Random.Shared.Next(128,255)), 
            Conversions.ColorFromHSV(Random.Shared.Next(0,255), 192, Random.Shared.Next(128,255)),
            Conversions.ColorFromHSV(Random.Shared.Next(0,255), 255, Random.Shared.Next(128,255)));
    }

}
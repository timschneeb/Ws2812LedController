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
public class LightUpPaletteReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Light single LEDs based on volume peaks up. Use colors from randomized palettes";
    public override int Speed { set; get; } = 1000 / 60;
    
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

        segment.SetPixel((int)(((_timeSinceStart.ElapsedMilliseconds & 0xFFFF) % (segment.Width-1)) +1), _currentPalette.ColorFromPalette(strength, strength, TBlendType.LinearBlend), layer);
        
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
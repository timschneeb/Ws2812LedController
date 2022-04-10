using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812AudioReactiveClient.FastLedCompatibility;
using Ws2812AudioReactiveClient.Model;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Effects;

/**
 * Adopted from https://github.com/atuline/FastLED-SoundReactive
 */
public class NoiseCenteredReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Light LEDs from center based on volume peaks and noise level";
    public override int Speed { set; get; } = 1000 / 60;

    private readonly Stopwatch _timer = new();
    private readonly Stopwatch _timerPaletteFade = new();
    
    private double _sampleAvg;
    private readonly CRGBPalette16 _currentPalette = new(CRGBPalette16.Palette.Ocean);
    private CRGBPalette16 _targetPalette = new(CRGBPalette16.Palette.Lava);
    private short _xdist;
    private short _ydist;
    private const byte MaxChanges = 24; // Value for blending between palettes.

    public NoiseCenteredReactiveEffect()
    {
        AvgSmoothingMode = AvgSmoothingMode.All;
    }
    
    public override void Reset()
    {
        _sampleAvg = 0;
        _timer.Reset();
        _timerPaletteFade.Reset();
        base.Reset();
    }

    protected override void Begin()
    {
        _timer.Restart();
        _timerPaletteFade.Restart();
        base.Begin();
    }

    protected override void End()
    {
        _timer.Reset();
        _timerPaletteFade.Reset();
        base.End();
    }

    private void Fillnoise8(LedSegmentGroup segmentGroup, LayerId layer)
    { 
        // Add Perlin noise with modifiers from the soundmems routine.
        int maxLen = (int)_sampleAvg;
        if (_sampleAvg > segmentGroup.Width)
        {
            maxLen = segmentGroup.Width;
        }

        for (int i = (segmentGroup.Width - maxLen) / 2; i < (segmentGroup.Width + maxLen + 1) / 2; i++)
        { 
            // The louder the sound, the wider the soundbar.
            byte index = Noise.inoise8((ushort)(i * _sampleAvg + _xdist), (ushort)(_ydist + i * _sampleAvg)); // Get a value from the noise function. I'm using both x and y axis.
            segmentGroup.SetPixel(i, _currentPalette.ColorFromPalette(index, 255, TBlendType.LinearBlend), layer); // With that value, look up the 8 bit colour palette value and assign it to the current LED.
        }

        _xdist = (short)(_xdist + Beat.beatsin8(5,0,10));
        _ydist = (short)(_ydist + Beat.beatsin8(4,0,10));
    } 
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var length = NextSample();
        if (length < 1)
        {
            goto NEXT_FRAME;
        }

        _sampleAvg = SampleAvg * 1024 * 1.5; //SmoothMean(_buffer) * 1024;;

       if (_timerPaletteFade.ElapsedMilliseconds >= 100)
        {
            CRGBPalette16.nblendPaletteTowardPalette(_currentPalette, _targetPalette, MaxChanges);
            _timerPaletteFade.Restart();
        }
        Fillnoise8(segment, layer); // Update the LED array with noise based on sound input

        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), 255 - /*fadeBy*/ 32),layer);
        }

        if (_timer.Elapsed > TimeSpan.FromSeconds(5))
        {
            GenerateNextPalette();
            _timer.Restart();
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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812AudioReactiveClient.FastLedCompatibility;
using Ws2812AudioReactiveClient.Model;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Effects;

public class NoiseMeterReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Volume reactive vu-meter";
    public override int Speed { set; get; } = 1000 / 60;
    public byte FadeSpeed { set; get; } = 120;
    public byte Intensity { set; get; } = 64;

    private readonly Stopwatch _timer = new();
    private readonly Stopwatch _timerPaletteFade = new();
    
    private double _sampleAvg;
    private readonly CRGBPalette16 _currentPalette = new(CRGBPalette16.Palette.Ocean);
    private CRGBPalette16 _targetPalette = new(CRGBPalette16.Palette.Lava);
    private short _xdist;
    private short _ydist;
    private const byte MaxChanges = 24; // Value for blending between palettes.

    public NoiseMeterReactiveEffect()
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
        Reset();
        base.End();
    }

    private void Fillnoise8(LedSegmentGroup segmentGroup, LayerId layer)
    { 
        // Add Perlin noise with modifiers from the soundmems routine.
        var maxLen = (int)(_sampleAvg * Intensity / 128.0);;
        if (maxLen > segmentGroup.Width)
        {
            maxLen = segmentGroup.Width;
        }

        for (var i = 0; i < maxLen; i++)
        { 
            // The louder the sound, the wider the soundbar.
            var index = Noise.inoise8((ushort)(i * _sampleAvg + _xdist), (ushort)(_ydist + i * _sampleAvg)); // Get a value from the noise function. I'm using both x and y axis.
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

        _sampleAvg = SampleAvg * 1024 * 1.5;

       if (_timerPaletteFade.ElapsedMilliseconds >= 100)
        {
            CRGBPalette16.nblendPaletteTowardPalette(_currentPalette, _targetPalette, MaxChanges);
            _timerPaletteFade.Restart();
        }
        Fillnoise8(segment, layer); // Update the LED array with noise based on sound input

        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ FadeSpeed)),layer);
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
using System.Drawing;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812AudioReactiveClient.Effects.Base;
using Ws2812AudioReactiveClient.Model;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;

namespace Ws2812AudioReactiveClient.Effects.Fft;

public class RipplePeakReactiveEffect : BaseAudioReactiveEffect, IHasFftBinSelection, IHasPeakDetection
{
    public override string Description => "Peak detection triggers ripples";
    public override int Speed { set; get; } = 1000 / 60;
    [ValueRange(max: 32)]
    public byte MaxRipples { set; get; } = 16; /* Up to 32 */
    public byte FadeStrength { set; get; } = 80;
    public int MaxSteps { set; get; } = 16;
    public FftCBinSelector FftCBinSelector { set; get; } = new(0);
    public double Threshold { get; set; } = 100;
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Lava);
    public bool RainbowColors { set; get; } = true;

    public RipplePeakReactiveEffect()
    {
        for (var i = 0; i < _ripples.Length; i++)
        {
            _ripples[i] = new Ripple();
        }
    }
    
    public override void Reset()
    {
        _counter = 255;
        foreach (var ripple in _ripples)
        {
            ripple.Reset();
        }
        base.Reset();
    }

    private class Ripple
    {
        internal int Position = 0;
        internal int Color = 0;
        internal int State = -2;

        internal void Reset()
        {
            Position = Color = 0;
            State = -2;
        }
    }
    
    private readonly Ripple[] _ripples = new Ripple[32];
    private byte _counter;
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (Frame == 0)
        {
            segment.Clear(Color.Black, layer);
            _counter = 255;
        }
        
        var count = NextSample();
        if (count < 1)
        {
            goto NEXT_FRAME;
        }

        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ FadeStrength)),layer);
        }

//  static uint8_t colour;                                  // Ripple colour is randomized.
//  static uint16_t centre;                                 // Center of the current ripple.
//  static int8_t steps = -1;                               // -1 is the initializing step.

//  static uint8_t ripFade = 255;                           // Starting brightness, which we'll say is SEGENV.aux0.


       // fade_out(240); // Lower frame rate means less effective fading than FastLED
        //fade_out(240);

        if (MaxRipples > 32)
        {
            MaxRipples = 32;
        }
        

        for (ushort i = 0; i < MaxRipples; i++)
        { // Limit the number of ripples.

            if (IsFftPeak(FftCBinSelector, Threshold))
            {
                _ripples[i].State = -1;
            }

            switch (_ripples[i].State)
            {

                case -2: // Inactive mode
                    break;

                case -1: // Initialize ripple variables.
                    _ripples[i].Position = Random.Shared.Next(segment.Width);
                    _ripples[i].Color = (int)(Math.Log10(FftMajorPeak[0]) * 128);
                    _ripples[i].State = 0;
                    break;

                case 0:
                {
                    var idx = (byte)_ripples[i].Color;
                    var color = RainbowColors ? ColorWheel.ColorAtIndex(idx, _counter) : ColorBlend.Blend(Color.Black,
                        Palette.ColorFromPalette(idx, 255, TBlendType.LinearBlend), _counter);
                    segment.SetPixel(_ripples[i].Position, color, layer);
                    _ripples[i].State++;
                    break;
                }
                default: 
                    if (MaxSteps == _ripples[i].State)
                    {
                        // At the end of the ripples. -2 is an inactive mode.
                        _ripples[i].State = -2;
                    }
                    else
                    {
                        // Middle of the ripples.
                        var idx = (byte)_ripples[i].Color;
                        var brightness = (byte)(_counter / _ripples[i].State * 2);
                        var color = RainbowColors ? ColorWheel.ColorAtIndex(idx, brightness) : 
                            ColorBlend.Blend(Color.Black, Palette.ColorFromPalette((byte)_ripples[i].Color, 255, TBlendType.None), brightness);
                        segment.SetPixel((_ripples[i].Position + _ripples[i].State + segment.Width) % segment.Width, color, layer);
                        segment.SetPixel((_ripples[i].Position - _ripples[i].State + segment.Width) % segment.Width, color, layer);
                        _ripples[i].State++; // Next step.
                    }
                    break;
            } 
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    } 
}
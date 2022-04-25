using System.Drawing;
using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.AudioReactive.Model;
using Ws2812LedController.AudioReactive.Utils;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.AudioReactive.Effects.Volume;

public class NoiseFireReactiveEffect : BaseAudioReactiveEffect, IHasVolumeAnalysis
{
    public override string FriendlyName => "Noise fire";
    public override string Description => "A perlin noise based volume reactive fire routine";
    public override int Speed { set; get; } = 1000 / 60;
    public byte AnimationSpeed { set; get; } = 64;
    public byte Intensity { set; get; } = 196;
    public IVolumeAnalysisOption VolumeAnalysisOptions { set; get; } = new FixedVolumeAnalysisOption(5000, 8000);

    private readonly CRGBPalette16 _palette;

    public NoiseFireReactiveEffect()
    {
        _palette = new CRGBPalette16(Conversions.ColorFromHSV(0,255,2), Conversions.ColorFromHSV(0,255,4), 
            Conversions.ColorFromHSV(0,255,8), Conversions.ColorFromHSV(0, 255, 8), Conversions.ColorFromHSV(0, 255, 16), 
            Color.Red, Color.Red, Color.Red, Color.FromArgb(0xFF,0x8C,0x00),Color.FromArgb(0xFF,0x8C,0x00),
            Color.FromArgb(0xFF,0xA5,0x00), Color.FromArgb(0xFF,0xA5,0x00), Color.Yellow,
            Color.FromArgb(0xFF,0xA5,0x00), Color.Yellow, Color.Yellow);
    }
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var length = NextSample();
        if (length < 1)
        {
            goto NEXT_FRAME;
        }

        for (var i = 0; i < segment.Width; i++)
        {
            ushort index = Noise.inoise8((ushort)(i * AnimationSpeed / 64.0),(ushort)(Time.Millis() * AnimationSpeed / 64 * segment.Width / 255)); // X location is constant, but we move along the Y at the rate of millis(). By Andrew Tuline.
            index = (ushort)((255 - i * 256.0 / segment.Width) * index / (256.0 - Intensity)); // Now we need to scale index so that it gets blacker as we get close to one of the ends.
            // This is a simple y=mx+b equation that's been scaled. index/128 is another scaling.

            byte strength = VolumeAnalysisOptions switch
            {
                AgcVolumeAnalysisOption agc => (byte)(SampleAgc * agc.Intensity / 64.0).Clamp(0, 255),
                FixedVolumeAnalysisOption fix => (byte)SampleAvg.Map(fix.MinimumMagnitude, fix.MaximumMagnitude, 0, 255, true),
                _ => 0
            };
            
            segment.SetPixel(i, _palette.ColorFromPalette((byte)index, strength, TBlendType.LinearBlend), layer);
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
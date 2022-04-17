using System.Drawing;
using Ws2812AudioReactiveClient.Effects.Base;
using Ws2812AudioReactiveClient.Model;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Effects.Volume;

public class PixelsReactiveEffect : BaseAudioReactiveEffect, IHasVolumeAnalysis
{
    public override string Description => "Random pixels based on volume peaks";
    public override int Speed { set; get; } = 1000 / 60;
    public byte FadeSpeed { set; get; } = 60;
    public byte Intensity { set; get; } = 1;
    public IVolumeAnalysisOption VolumeAnalysisOptions { set; get; } = new AgcVolumeAnalysisOption();
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Lava);

    private double[] _proc = new double[32];
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var count = NextSample(ref _proc);
        if (count < 1 || _proc.Length < 1)
        {
            goto NEXT_FRAME;
        }

        /* Fade to black by x */ 
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), (short)(255 - /*fadeBy*/ FadeSpeed)),layer);
        }

        for (var i = 0; i < Intensity; i++)
        {
            var segLoc = Random.Shared.Next(segment.Width); // 16 bit for larger strands of LED's.
            byte bright = VolumeAnalysisOptions switch
            {
                AgcVolumeAnalysisOption agc => (byte)(SampleAgc * agc.Intensity / 64.0).Clamp(0, 255),
                FixedVolumeAnalysisOption fix => (byte)SampleAvg.Map(fix.MinimumMagnitude, fix.MaximumMagnitude, 0, 255, true),
                _ => 0
            };
            var idx = (byte)(_proc[Random.Shared.Next(0, _proc.Length)] * 65536 + i * 4);

            segment.SetPixel(segLoc, ColorBlend.Blend(Color.Black, Palette.ColorFromPalette(idx, 255, TBlendType.None), bright), layer);
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
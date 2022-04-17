using System.Drawing;
using Ws2812AudioReactiveClient.Effects.Base;
using Ws2812AudioReactiveClient.Model;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Effects.Volume;

public class MatripixReactiveEffect : BaseAudioReactiveEffect, IHasVolumeAnalysis
{
    public override string Description => "Simple running light with brightness derived from volume levels";
    public override int Speed { set; get; } = 1000 / 60;
    public byte FramesPerStep { set; get; } = 1;
    public IVolumeAnalysisOption VolumeAnalysisOptions { set; get; } = new AgcVolumeAnalysisOption();
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Lava);
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (Frame == 0)
        {
            segment.Clear(layer);
        }
        
        var count = NextSample();
        if (count < 1 || Frame % FramesPerStep != 0)
        {
            goto NEXT_FRAME;
        }
        
        byte bright = VolumeAnalysisOptions switch
        {
            AgcVolumeAnalysisOption agc => (byte)(SampleAgc * agc.Intensity / 64.0).Clamp(0, 255),
            FixedVolumeAnalysisOption fix => (byte)SampleAvg.Map(fix.MinimumMagnitude, fix.MaximumMagnitude, 0, 255, true),
            _ => 0
        };
        segment.SetPixel(segment.Width - 1, ColorBlend.Blend(Color.Black, Palette.ColorFromPalette((byte)Time.Millis(), 255, TBlendType.None), (byte)bright), layer);
        for (var i = 0; i < segment.Width - 1; i++)
        {
            segment.SetPixel(i, segment.PixelAt(i + 1, layer), layer);
        }

        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
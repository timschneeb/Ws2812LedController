using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.AudioReactive.Model;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.AudioReactive.Effects.Volume;

public class JugglesReactiveEffect : BaseAudioReactiveEffect, IHasVolumeAnalysis
{
    public override string FriendlyName => "Juggles";
    public override string Description => "Simple scanning light with brightness derived from volume levels";
    public override int Speed { set; get; } = 1000 / 60;
    public byte AnimationSpeed { set; get; } = 64;
    public byte Count { set; get; } = 2;
    public IVolumeAnalysisOption VolumeAnalysisOptions { set; get; } = new AgcVolumeAnalysisOption(64);
    public CRGBPalette16 Palette { set; get; } = new(CRGBPalette16.Palette.Lava);
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var count = NextSample();
        if (count < 1)
        {
            goto NEXT_FRAME;
        }
        
        for(var i = 0; i < segment.Width; ++i) 
        {
            segment.SetPixel(i, Scale.nscale8x3(segment.PixelAt(i, layer), 255 - /*fadeBy*/ 31),layer);
        } 
        
        for (var i=0; i < Count; i++)
        {
            byte strength = VolumeAnalysisOptions switch
            {
                AgcVolumeAnalysisOption agc => (byte)(SampleAgc * agc.Intensity / 64.0).Clamp(0, 255),
                FixedVolumeAnalysisOption fix => (byte)SampleAvg.Map(fix.MinimumMagnitude, fix.MaximumMagnitude, 0, 255, true),
                _ => 0
            };
            var color = Palette.ColorFromPalette((byte)(Time.Millis() / 4 + i * 2), (byte)strength, TBlendType.None);
            segment.SetPixel(Beat.beatsin16((ushort)(AnimationSpeed/4+i*2),0,(ushort)(segment.Width-1)), color, layer);
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
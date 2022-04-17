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
public class LightUpReactiveEffect : BaseAudioReactiveEffect, IHasVolumeAnalysis, IHasPeakDetection
{
    public override string Description => "Light single LEDs based on volume peaks up";
    public override int Speed { set; get; } = 1000 / 60;
    public int FadeSpeed { set; get; } = 6;
    public IVolumeAnalysisOption VolumeAnalysisOptions { set; get; } = new AgcVolumeAnalysisOption(128);
    public double Threshold { set; get; } = 1500;
    
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
            segment.SetPixel((int)(((TimeSinceStart.ElapsedMilliseconds & 0xFFFF) % (segment.Width-1)) +1), Conversions.ColorFromHSV(strength, 255, strength), layer);
        }
        
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
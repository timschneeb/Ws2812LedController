using System.Drawing;
using Ws2812LedController.AudioReactive.Dsp;
using Ws2812LedController.AudioReactive.Effects.Base;
using Ws2812LedController.AudioReactive.Utils;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.AudioReactive.Effects.Fft;

public class WaterfallReactiveEffect : BaseAudioReactiveEffect, IHasFftBinSelection, IHasPeakDetection, IHasFrequencyLimits
{
    public override string FriendlyName => "Waterfall";
    public override string Description => "FFT version of a Waterfall";
    public override int Speed { set; get; } = 1000 / 60;
    public bool ColorBasedOnHz { set; get; } = false;
    public FftCBinSelector FftCBinSelector { set; get; } = new(0);
    public double Threshold { set; get; } = 4000;
    public int StartFrequency { set; get; } = 93;
    public int EndFrequency { set; get; } = 5120;

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (Frame == 0)
        {
            segment.Clear(Color.Black, layer);
        }
        
        var count = NextSample();
        if (count < 1)
        {
            goto NEXT_FRAME;
        }


        Color pixCol;
        var fftNoData = FftMajorPeak[0] <= 0 || FftMajorPeak[1] <= 0;
        if (fftNoData)
        {
            pixCol = Color.Black;
        }
        else
        {
            pixCol = ColorWheel.ColorAtIndex((byte)((Math.Log10((int)FftMajorPeak[0]) - Math.Log10(StartFrequency)) * 255.0/(Math.Log10(EndFrequency)-Math.Log10(StartFrequency))), 
                (byte)((int)FftMajorPeak[1] >> 4));
        }
        
        if (!fftNoData && IsFftPeak(FftCBinSelector, Threshold))
        {
            segment.SetPixel(segment.Width - 1, Conversions.ColorFromHSV(92, 92, 92), layer);
        }
        else
        {
            var color = ColorBasedOnHz ? pixCol : Conversions.ColorFromHSV(92, 92,((int)FftMajorPeak[1] >> 8).Map(0, 255, 0, 92));
            segment.SetPixel(segment.Width - 1, color, layer);
        }
        
        for (var i = 0; i < segment.Width-1; i++)
        {
            segment.SetPixel(i, segment.PixelAt(i + 1, layer), layer);
        }

        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
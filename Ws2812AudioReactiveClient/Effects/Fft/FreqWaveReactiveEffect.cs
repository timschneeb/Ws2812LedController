using System.Drawing;
using Ws2812AudioReactiveClient.Effects.Base;
using Ws2812AudioReactiveClient.Utils;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Effects.Fft;

public class FreqWaveReactiveEffect : BaseAudioReactiveEffect, IHasFrequencyLimits
{
    public override string Description => "Wave pushing outwards from center colored by frequency";
    public override int Speed { set; get; } = 1000 / 60;
    public Edge StartFromEdge = Edge.None;
    public byte FadeStrength { set; get; } = 10;
    public int Intensity { set; get; } = 255;
    public byte Sensitivity { set; get; } = 10;
    public int StartFrequency { set; get; } = 70;
    public int EndFrequency { set; get; } = 5000;
    public int MinFftPeakMagnitude { set; get; } = 100;
    public int MaxFftPeakMagnitude { set; get; } = 1800;
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
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
        
        if (FftMajorPeak[0] <= 0 || FftMajorPeak[1] <= 0)
        {
            goto NEXT_FRAME;
        }

        var sensitivity = Sensitivity.Map(1, 255, 1, 10);
        var pixVal = (int)(SampleAvg * Intensity / 256 * sensitivity);
        if (pixVal > 255)
        {
            pixVal = 255;
        }

        var intensity = pixVal.Map(0, 255, 0, 100) / 100.0; // make a brightness from the last avg

        Color color;
        var fftPeak = FftMajorPeak[0];

        if (fftPeak < StartFrequency || fftPeak > EndFrequency)
        {
            color = Color.Black;
        }
        else
        {
            var i = MinFftPeakMagnitude != MaxFftPeakMagnitude ? fftPeak.Map(MinFftPeakMagnitude, MaxFftPeakMagnitude, 0, 255, true) : fftPeak;
            var b = (ushort)(255 * intensity);
            if (b > 255)
            {
                b = 255;
            }
            color = Conversions.ColorFromHSV(i, 240, (byte)b);
        }


        switch (StartFromEdge)
        {
            // shift the pixels one pixel outwards
            case Edge.Start:
            {
                segment.SetPixel(0, color, layer);
            
                for (var i = segment.Width - 1; i > 0; i--)
                {
                    segment.SetPixel(i, segment.PixelAt(i - 1, layer), layer);
                }

                break;
            }
            case Edge.End:
            {
                segment.SetPixel(segment.Width - 1, color, layer);
            
                for (var i = 0; i < segment.Width - 1; i++)
                {
                    segment.SetPixel(i, segment.PixelAt(i + 1, layer), layer);
                }

                break;
            }
            case Edge.None:
            {
                segment.SetPixel(segment.Width / 2, color, layer);
            
                for (var i = segment.Width - 1; i > segment.Width / 2; i--)
                {
                    // Move to the right.
                    segment.SetPixel(i, segment.PixelAt(i - 1, layer), layer);
                }

                for (var i = 0; i < segment.Width / 2; i++)
                {
                    // Move to the left.
                    segment.SetPixel(i, segment.PixelAt(i + 1, layer), layer);
                }

                break;
            }
        }

        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
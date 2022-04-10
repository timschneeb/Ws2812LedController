using System.Diagnostics;

namespace Ws2812AudioReactiveClient.Dsp;

public class FftBinSelector
{
    public FftBinSelector(int start, int end)
    {
        Debug.Assert(start <= end, "Start index must be equal or lesser than end index");
        Start = start;
        End = end;
    }
    
    public FftBinSelector(int num)
    {
        Start = num;
        End = num;
    }

    public Span<double> AsSpan(double[] fftBins)
    {
        return fftBins[Start..End];
    }
    
    public double Mean(double[] fftBins)
    {
        return Start == End ? fftBins[Start] : fftBins.MeanSpan(Start, End);
    }

    public int Start { set; get; }
    public int End { set; get; }
}
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Ws2812LedController.AudioReactive.Dsp;

[Serializable]
public class FftCBinSelector : ISerializable
{
    public FftCBinSelector(int start, int end)
    {
        Debug.Assert(start <= end, "Start index must be equal or lesser than end index");
        Start = start;
        End = end;
    }
    
    public FftCBinSelector(int num)
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
        return Start == End ? fftBins[Start] : fftBins.Rms(Start, End);
    }

    public override string ToString()
    {
        return $"Start: {Start}; End: {End}";
    }

    public int Start { set; get; }
    public int End { set; get; }
    
    public FftCBinSelector(SerializationInfo info, StreamingContext context)
    {
        Start = (int)(info.GetValue(nameof(Start), typeof(int)) ?? 0);
        End = (int)(info.GetValue(nameof(End), typeof(int)) ?? 0);
    }
            
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Start), Start);
        info.AddValue(nameof(End), End);
    }
}
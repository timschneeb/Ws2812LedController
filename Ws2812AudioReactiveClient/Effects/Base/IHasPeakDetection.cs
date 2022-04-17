namespace Ws2812AudioReactiveClient.Effects.Base;

public interface IHasPeakDetection
{
    public double Threshold { set; get; }
}
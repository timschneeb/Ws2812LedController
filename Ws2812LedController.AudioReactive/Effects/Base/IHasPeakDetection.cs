namespace Ws2812LedController.AudioReactive.Effects.Base;

public interface IHasPeakDetection
{
    public double Threshold { set; get; }
}
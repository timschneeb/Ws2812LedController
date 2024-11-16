using Ws2812LedController.Core.Model;

namespace Ws2812LedController.AudioReactive.Effects.Base;

public interface IHasPeakDetection
{
    [ValueRange(0, int.MaxValue)]
    public double Threshold { set; get; }
}
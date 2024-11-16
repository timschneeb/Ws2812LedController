using Ws2812LedController.AudioReactive.Dsp;

namespace Ws2812LedController.AudioReactive.Effects.Base;

public interface IHasFftBinSelection
{
    public FftCBinSelector FftCBinSelector { set; get; }
}
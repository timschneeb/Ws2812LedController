using Ws2812LedController.AudioReactive.Dsp;

namespace Ws2812LedController.AudioReactive.Effects.Base;

public interface IHasOptionalFftBinSelection
{
    public FftCBinSelector? FftCBinSelector { set; get; }
}
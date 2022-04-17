using Ws2812AudioReactiveClient.Dsp;

namespace Ws2812AudioReactiveClient.Effects.Base;

public interface IHasOptionalFftBinSelection
{
    public FftCBinSelector? FftCBinSelector { set; get; }
}
using Ws2812AudioReactiveClient.Dsp;

namespace Ws2812AudioReactiveClient.Effects.Base;

public interface IHasFftBinSelection
{
    public FftCBinSelector FftCBinSelector { set; get; }
}
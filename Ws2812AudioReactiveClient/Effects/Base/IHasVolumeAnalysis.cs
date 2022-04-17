using Ws2812AudioReactiveClient.Model;

namespace Ws2812AudioReactiveClient.Effects.Base;

public interface IHasVolumeAnalysis
{
    public IVolumeAnalysisOption VolumeAnalysisOptions { set; get; }
}
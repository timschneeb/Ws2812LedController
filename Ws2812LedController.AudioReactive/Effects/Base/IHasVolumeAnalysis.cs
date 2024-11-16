using Ws2812LedController.AudioReactive.Model;

namespace Ws2812LedController.AudioReactive.Effects.Base;

public interface IHasVolumeAnalysis
{
    public IVolumeAnalysisOption VolumeAnalysisOptions { set; get; }
}
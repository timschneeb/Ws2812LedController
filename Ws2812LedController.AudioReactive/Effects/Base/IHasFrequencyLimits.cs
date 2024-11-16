using Ws2812LedController.Core.Model;

namespace Ws2812LedController.AudioReactive.Effects.Base;

public interface IHasFrequencyLimits
{
    [ValueRange(0, int.MaxValue)]
    public int StartFrequency { set; get; }
    [ValueRange(0, int.MaxValue)]
    public int EndFrequency { set; get; }
}
namespace Ws2812LedController.AudioReactive.Effects.Base;

public interface IHasFrequencyLimits
{
    public int StartFrequency { set; get; }
    public int EndFrequency { set; get; }
}
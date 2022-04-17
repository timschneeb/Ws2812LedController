namespace Ws2812AudioReactiveClient.Effects.Base;

public interface IHasFrequencyLimits
{
    public int StartFrequency { set; get; }
    public int EndFrequency { set; get; }
}
namespace Ws2812LedController.Core.Utils;

public static class Time
{
    public static uint Millis()
    {
        return (uint)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
    }
}
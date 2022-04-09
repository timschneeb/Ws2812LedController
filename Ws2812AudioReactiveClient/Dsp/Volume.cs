namespace Ws2812AudioReactiveClient.Dsp;

public static class Volume
{
    public static double LevelLinear(double[] buffer)
    {
        var energy = 0.0;
        uint j;
        for (j = 0; j < buffer.Length; j++)
        {
            energy += buffer[j]*buffer[j];
        }
        return energy / buffer.Length;
    }
    public static double DbSpl(double[] buffer)
    {
        return 10.0 * (double)Math.Log10((LevelLinear (buffer)));
    }
}
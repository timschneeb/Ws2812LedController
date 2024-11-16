namespace Ws2812LedController.AudioReactive.Dsp;

public static class VolumeLevel
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
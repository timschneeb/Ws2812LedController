namespace Ws2812AudioReactiveClient.Dsp;

public static class Volume
{
    public static float LevelLinear(float[] buffer)
    {
        var energy = 0.0f;
        uint j;
        for (j = 0; j < buffer.Length; j++)
        {
            energy += buffer[j]*buffer[j];
        }
        return energy / buffer.Length;
    }
    public static float DbSpl(float[] buffer)
    {
        return 10.0f * (float)Math.Log10((LevelLinear (buffer)));
    }
}
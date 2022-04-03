namespace Ws2812AudioReactiveClient.FastLedCompatibility;

public static class Math8
{
    public static byte qadd8(int i, int j)
    {
        var t = i + j;
        if (t > 255) t = 255;
        if (t < 0) t = 0;
        return (byte)t;
    }
    
    public static sbyte avg7(sbyte i, sbyte j)
    {
        return (sbyte)(((i + j) >> 1) + (i & 0x1));
    }

}
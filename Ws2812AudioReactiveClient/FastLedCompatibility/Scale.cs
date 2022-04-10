using System.Drawing;

namespace Ws2812AudioReactiveClient.FastLedCompatibility;

public static class Scale
{
    public static byte scale8(byte i, byte scale)
    {
//#if (FASTLED_SCALE8_FIXED == 1)
        return (byte)((((ushort)i) * (1 + (ushort)(scale))) >> 8);
//#else
	//return ((ushort)i * (ushort)(scale)) >> 8;
//#endif
    }
    
    public static Color nscale8x3( Color c, short scale)
    {
//#if (FASTLED_SCALE8_FIXED == 1)
        var scale_fixed = scale + 1;
        var r = (c.R * scale_fixed) >> 8;
        var g = (c.G * scale_fixed) >> 8;
        var b = (c.B * scale_fixed) >> 8;
        return Color.FromArgb(r, g, b);
//#else
        /*  r = ((int)r * (int)(scale) ) >> 8;
          g = ((int)g * (int)(scale) ) >> 8;
          b = ((int)b * (int)(scale) ) >> 8;*/
//#endif

    }
    
    public static ushort scale16(ushort i, ushort scale)
    {
        ushort result;
//#if FASTLED_SCALE8_FIXED == 1
        result = (ushort)(((uint)(i) * (1 + (uint)(scale))) / 65536);
//#else
	//result = (ushort)(((uint)(i) * (uint)(scale)) / 65536);
//#endif
        return result;
    }

}
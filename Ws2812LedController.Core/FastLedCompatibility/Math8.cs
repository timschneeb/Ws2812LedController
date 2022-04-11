using System.Drawing;

namespace Ws2812LedController.Core.FastLedCompatibility;

public static class Math8
{
    public static byte qadd8(int i, int j)
    {
        var t = i + j;
        if (t > 255) t = 255;
        if (t < 0) t = 0;
        return (byte)t;
    }
    
    public static Color AddColor(Color color, Color newColor)
    {
        var r = qadd8(color.R, newColor.R);
        var g = qadd8(color.G, newColor.G);
        var b = qadd8(color.B, newColor.B);
        return Color.FromArgb(r,g,b);
    }


    
    public static sbyte avg7(sbyte i, sbyte j)
    {
        return (sbyte)(((i + j) >> 1) + (i & 0x1));
    }

    /// Fast 16-bit approximation of sin(x). This approximation never varies more than
    /// 0.69% from the floating point value you'd get by doing
    ///
    ///     float s = sin(x) * 32767.0;
    ///
    /// @param theta input angle from 0-65535
    /// @returns sin of theta, value between -32767 to 32767.
    public static short Sin16(ushort theta)
    {
        ushort[] @base = {0, 6393, 12539, 18204, 23170, 27245, 30273, 32137};
        byte[] slope = {49, 48, 44, 38, 31, 23, 14, 4};

        ushort offset = (ushort)((theta & 0x3FFF) >> 3); // 0..2047
        if ((theta & 0x4000) != 0)
        {
            offset = (ushort)(2047 - offset);
        }

        byte section = (byte)(offset / 256); // 0..7
        ushort b = @base[section];
        byte m = slope[section];

        byte secoffset8 = (byte)((byte)(offset) / 2);

        ushort mx = (ushort)(m * secoffset8);
        short y = (short)(mx + b);

        if ((theta & 0x8000) != 0)
        {
            y = (short)(-y);
        }

        return y;
    }

}
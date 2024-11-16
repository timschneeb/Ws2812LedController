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
    
    /// triwave8: triangle (sawtooth) wave generator.  Useful for
    ///           turning a one-byte ever-increasing value into a
    ///           one-byte value that oscillates up and down.
    ///
    ///           input         output
    ///           0..127        0..254 (positive slope)
    ///           128..255      254..0 (negative slope)
    public static byte triwave8(byte @in)
    {
        if ((@in & 0x80) != 0)
        {
            @in = (byte)(255 - @in);
        }
        return (byte)(@in << 1);
    }
    
    /// quadwave8: quadratic waveform generator
    public static byte quadwave8(byte @in)
    {
        return ease8InOutQuad(triwave8(@in));
    }
    
    /// cubicwave8: cubic waveform generator.  Spends visibly more time
    ///             at the limits than 'sine' does.
    public static byte cubicwave8(byte @in)
    {
        return ease8InOutCubic(triwave8(@in));
    }

    /// ease8InOutCubic: 8-bit cubic ease-in / ease-out function
    ///                 Takes around 18 cycles on AVR
    public static byte ease8InOutCubic(byte i)
    {
        var ii = Scale.scale8(i, i);
        var iii = Scale.scale8(ii, i);

        var result = (byte)((3 * ii) - (2 * iii));

        // if we got "256", return 255:
        if ((result & 0x100) != 0)
        {
            result = 255;
        }
        return result;
    }

    
    /// ease8InOutQuad: 8-bit quadratic ease-in / ease-out function
    ///                Takes around 13 cycles on AVR
    public static byte ease8InOutQuad(byte i)
    {
        var j = i;
        if ((j & 0x80) != 0)
        {
            j = (byte)(255 - j);
        }
        var jj = Scale.scale8(j, j);
        var jj2 = (byte)(jj << 1);
        if ((i & 0x80) != 0)
        {
            jj2 = (byte)(255 - jj2);
        }
        return jj2;
    }

}
using System.Drawing;

namespace Ws2812LedController.Core.Colors;

public static class ColorWheel
{
    /*
     * Put a value 0 to 255 in to get a color value.
     * The colours are a transition r -> g -> b -> back to r
     * Inspired by the Adafruit examples.
     */
    public static Color ColorAtIndex(byte pos, byte brightness = 255)
    {
        pos = (byte)(255 - pos);
        switch (pos)
        {
            case < 85:
                return Color.FromArgb(brightness, 255 - pos * 3, 0, pos * 3);
                // return (uint)(((uint)(255 - pos * 3) << 16) | ((uint)0 << 8) | (pos * 3));
            case < 170:
                pos -= 85;
                return Color.FromArgb(brightness, 0, pos * 3, 255 - pos * 3);
                // return (uint)(((uint)0 << 16) | ((uint)(pos * 3) << 8) | (255 - pos * 3));
            default:
                pos -= 170;
                return Color.FromArgb(brightness, pos * 3, 255 - pos * 3, 0);
                // return ((uint)(pos * 3) << 16) | ((uint)(255 - pos * 3) << 8) | (0);
        }
    }
    
    /*
     * Returns a new, random wheel color
     */
    public static Color RandomColor(byte brightness = 255)
    {
        return ColorAtIndex((byte)Random.Shared.Next(0, 255), brightness);
    }

    /*
     * Returns a new, random wheel index with a minimum distance of 42 from pos.
     */
    public static byte NextRandomIndex(byte pos)
    {
        byte r = 0;
        byte x = 0;
        byte y = 0;
        byte d = 0;
    
        while (d < 42)
        {
            r = (byte)Random.Shared.Next(0, 255);
            x = (byte)Math.Abs(pos - r);
            y = (byte)(255 - x);
            d = Math.Min(x, y);
        }
    
        return r;
    }
}
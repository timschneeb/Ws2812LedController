using System.Drawing;

namespace Ws2812LedController.HueApi;

public static class ColorUtils
{
    public static Color FromHsb(int hue, int sat, int bri) // convert hue / sat values from HUE API to RGB
    {
        var s = sat / 255.0;
        var v = bri / 255.0;

        if (s <= 0.0)
        {
            return Color.FromArgb(0, 0, 0);
        }
        double hh = hue;
        if (hh >= 65535.0)
        {
            hh = 0.0;
        }
        hh /= 11850;
        var i = (int)hh;
        var ff = hh - i;
        var p = v * (1.0 - s);
        var q = v * (1.0 - (s * ff));
        var t = v * (1.0 - (s * (1.0 - ff)));

        return i switch
        {
            0 => Color.FromArgb((int)(v * 255), (int)(t * 255), (int)(p * 255)),
            1 => Color.FromArgb((int)(q * 255), (int)(v * 255), (int)(p * 255)),
            2 => Color.FromArgb((int)(p * 255), (int)(v * 255), (int)(t * 255)),
            3 => Color.FromArgb((int)(p * 255), (int)(q * 255), (int)(v * 255)),
            4 => Color.FromArgb((int)(t * 255), (int)(p * 255), (int)(v * 255)),
            _ => Color.FromArgb((int)(v * 255), (int)(p * 255), (int)(q * 255))
        };
    }
    
    public static void ToHsb(Color color, out int hue, out int sat, out int bri)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));

        hue = (int)color.GetHue();
        sat = (int)(max == 0 ? 0 : 1d - (1d * min / max)) * 255;
        bri = max;
    }

    public static Color FromColorTemperature(int ct, byte bri, byte[] rgbMultiplier) // convert ct (color temperature) value from HUE API to RGB
    {
        var hectemp = ct == 0 ? 0 : 10000 / ct;

        int r;
        int g;
        int b;
        if (hectemp <= 66)
        {
            r = 255;
            g = (int)(99.4708025861 * Math.Log(hectemp) - 161.1195681661);
            b = hectemp <= 19 ? 0 : (int)(138.5177312231 * Math.Log(hectemp - 10) - 305.0447927307);
        }
        else
        {
            r = (int)(329.698727446 * Math.Pow(hectemp - 60, -0.1332047592));
            g = (int)(288.1221695283 * Math.Pow(hectemp - 60, -0.0755148492));
            b = 255;
        }

        r = r > 255 ? 255 : r;
        g = g > 255 ? 255 : g;
        b = b > 255 ? 255 : b;

        // Apply multiplier for white correction
        r = r * rgbMultiplier[0] / 100;
        g = g * rgbMultiplier[1] / 100;
        b = b * rgbMultiplier[2] / 100;

        return Color.FromArgb((byte)(r * (bri / 255.0f)), (byte)(g * (bri / 255.0f)), (byte)(b * (bri / 255.0f)));
    }

    public static Color FromXy(float x, float y, byte bri, byte[] rgbMultiplier) // convert CIE xy values from HUE API to RGB
    {
        var optimal_bri = bri;
        if (optimal_bri < 5)
        {
            optimal_bri = 5;
        }

        var z = 1.0f - x - y;

        // sRGB D65 conversion
        var r = x * 3.2406f - y * 1.5372f - z * 0.4986f;
        var g = -x * 0.9689f + y * 1.8758f + z * 0.0415f;
        var b = x * 0.0557f - y * 0.2040f + z * 1.0570f;


        // Apply gamma correction
        r = r <= 0.0031308f ? 12.92f * r : (1.0f + 0.055f) * (float)Math.Pow(r, (1.0f / 2.4f)) - 0.055f;
        g = g <= 0.0031308f ? 12.92f * g : (1.0f + 0.055f) * (float)Math.Pow(g, (1.0f / 2.4f)) - 0.055f;
        b = b <= 0.0031308f ? 12.92f * b : (1.0f + 0.055f) * (float)Math.Pow(b, (1.0f / 2.4f)) - 0.055f;

        // Apply multiplier for white correction
        r = r * rgbMultiplier[0] / 100;
        g = g * rgbMultiplier[1] / 100;
        b = b * rgbMultiplier[2] / 100;

        if (r > b && r > g)
        {
            // red is biggest
            if (r > 1.0f)
            {
                g = g / r;
                b = b / r;
                r = 1.0f;
            }
        }
        else if (g > b && g > r)
        {
            // green is biggest
            if (g > 1.0f)
            {
                r = r / g;
                b = b / g;
                g = 1.0f;
            }
        }
        else if (b > r && b > g)
        {
            // blue is biggest
            if (b > 1.0f)
            {
                r = r / b;
                g = g / b;
                b = 1.0f;
            }
        }

        r = r < 0F ? 0F : r;
        g = g < 0F ? 0F : g;
        b = b < 0F ? 0F : b;

        return Color.FromArgb((int)(r * optimal_bri), (int)(g * optimal_bri), (int)(b * optimal_bri));
    }

}
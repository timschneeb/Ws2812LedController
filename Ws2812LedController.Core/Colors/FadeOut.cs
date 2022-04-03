using System.Drawing;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core.Colors;

public static class FadeOut
{
    public static bool DoFrame(LedSegmentGroup segment, Color targetColor, FadeRate fadeRate, LayerId layer)
    {
        byte[] rateMapH = { 0, 1, 1, 1, 2, 3, 4, 6 };
        byte[] rateMapL = { 0, 2, 3, 8, 8, 8, 8, 8 };

        var rate = (byte)fadeRate;
        var rateH = rateMapH[rate];
        var rateL = rateMapL[rate];

        var color = (int)targetColor.ToUInt32();
        
        var w2 = (color >> 24) & 0xff;
        var r2 = (color >> 16) & 0xff;
        var g2 = (color >> 8) & 0xff;
        var b2 = (color & 0xff);

        var finished = true;

        for (var i = 0; i <= segment.AbsEnd; i++)
        {
            color = (int)segment.PixelAt(i, layer).ToUInt32(); // current color
            if (rate == 0)
            {
                // old fade-to-black algorithm
                segment.SetPixel(i, ((uint)(color >> 1) & 0x7F7F7F7F).ToColor(), layer);
            }
            else
            {
                // new fade-to-color algorithm
                var w1 = (color >> 24) & 0xff;
                var r1 = (color >> 16) & 0xff;
                var g1 = (color >> 8) & 0xff;
                var b1 = (color & 0xff);

                // calculate the color differences between the current and target colors
                var wdelta = w2 - w1;
                var rdelta = r2 - r1;
                var gdelta = g2 - g1;
                var bdelta = b2 - b1;

                // if the current and target colors are almost the same, jump right to the target
                // color, otherwise calculate an intermediate color. (fixes rounding issues)
                wdelta = Math.Abs(wdelta) < 3 ? wdelta : (wdelta >> rateH) + (wdelta >> rateL);
                rdelta = Math.Abs(rdelta) < 3 ? rdelta : (rdelta >> rateH) + (rdelta >> rateL);
                gdelta = Math.Abs(gdelta) < 3 ? gdelta : (gdelta >> rateH) + (gdelta >> rateL);
                bdelta = Math.Abs(bdelta) < 3 ? bdelta : (bdelta >> rateH) + (bdelta >> rateL);

                var newColor = Color.FromArgb(w1 + wdelta, r1 + rdelta, g1 + gdelta, b1 + bdelta);
                if (newColor.ToArgb() != targetColor.ToArgb() && !(rdelta == 0 && gdelta == 0 && bdelta == 0 & wdelta == 0))
                {
                    finished = false;
                }
                segment.SetPixel(i, newColor, layer);
            }
            
        }
        
        return finished;
    }
}
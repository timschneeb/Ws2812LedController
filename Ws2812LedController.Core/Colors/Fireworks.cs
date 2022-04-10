using System.Diagnostics;
using System.Drawing;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Colors;

public static class Fireworks
{
    public static void DoFrame(LedSegmentGroup segment, Color color, Color backgroundColor, FadeRate fadeOutRate, SizeOption particleSize, bool triggered = false, LayerId layer = LayerId.BaseLayer)
    {
        DoFrame(segment, new[]{ color }, backgroundColor, fadeOutRate, particleSize, triggered, layer);
    }
    
    public static void DoFrame(LedSegmentGroup segment, Color[] colors, Color backgroundColor, FadeRate fadeOutRate, SizeOption particleSize, bool triggered = false, LayerId layer = LayerId.BaseLayer)
    {
        Debug.Assert(colors.Length > 0, "Color array must not be empty");
        FadeOut.DoFrame(segment, backgroundColor, fadeOutRate, layer);

        var color = colors[Random.Shared.Next(colors.Length)];
        var size = 1 << (byte)particleSize;
        
        var pixels = segment.DumpBytes(layer);
        const int bytesPerPixel = 4;
        var stopPixel = segment.AbsEnd * bytesPerPixel;
        
        for (var i = bytesPerPixel; i < stopPixel; i++)
        {
            var tmpPixel = (ushort)((pixels[i - bytesPerPixel] >> 2) + pixels[i] + (pixels[i + bytesPerPixel] >> 2));
            pixels[i] = (byte)(tmpPixel > 255 ? 255 : tmpPixel);
        }
        segment.UpdateBytes(pixels, layer);
        
        if (!triggered)
        {
            for (ushort i = 0; i < Math.Max(1, segment.Width / 20); i++)
            {
                if (Random.Shared.Next(10) == 0)
                {
                    var index = Random.Shared.Next(segment.Width - size);
                    segment.Fill(index, size, color, layer);
                }
            }
        }
        else
        {
            for (ushort i = 0; i < Math.Max(1, segment.Width / 10); i++)
            {
                var index = Random.Shared.Next(segment.Width - size);
                segment.Fill(index, size, color, layer);
            }
        }
    }
}
using System.Drawing;
using Ws2812LedController.Core.FastLedCompatibility;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Colors;

public static class Blur
{
    // Scale determines how far apart the pixels in our noise matrix are.  Try
// changing these values around to see how it affects the motion of the display.  The
// higher the value of scale, the more "zoomed out" the noise iwll be.  A value
// of 1 will be so zoomed in, you'll mostly see solid colors.
// static int scale_2d = 30; // scale is set dynamically once we've started up

// blur1d: one-dimensional blur filter. Spreads light to 2 line neighbors.
// blur2d: two-dimensional blur filter. Spreads light to 8 XY neighbors.
//
//           0 = no spread at all
//          64 = moderate spreading
//         172 = maximum smooth, even spreading
//
//         173..255 = wider spreading, but increasing flicker
//
//         Total light is NOT entirely conserved, so many repeated
//         calls to 'blur' will also result in the light fading,
//         eventually all the way to black; this is by design so that
//         it can be used to (slowly) clear the LEDs to black.

    public static void blur1d(ref LedSegmentGroup seg, byte blur_amount, LayerId layer)
    {
        var keep = (byte)(255 - blur_amount);
        var seep = (byte)(blur_amount >> 1);
        Color carryover = Color.Black;
        for (ushort x = 0; x < seg.Width; x++)
        {
            var cur = seg.PixelAt(x, layer);
            var part = cur;
            part = Scale.nscale8x3(part, seep);
            cur = Scale.nscale8x3(cur, keep);
            cur = Math8.AddColor(cur, carryover);
            if (x > 0) 
            {
                seg.SetPixel(x - 1, Math8.AddColor(seg.PixelAt(x - 1, layer), part), layer);
            }

            seg.SetPixel(x, cur, layer);

            carryover = part;
        }
    }
}
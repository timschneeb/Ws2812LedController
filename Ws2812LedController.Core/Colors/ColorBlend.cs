using System.Drawing;
using System.Runtime.InteropServices;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core.Colors;

public static class ColorBlend
{
    public static Color Blend(Color color1, Color color2, byte blendAmt, bool createOpaqueOutput = false)
    {
        return blendAmt switch
        {
            0 => color1,
            255 => color2,
            _ => Color.FromArgb(
                createOpaqueOutput ? 255 : BlendChannel(color1.A, color2.A, blendAmt), 
                BlendChannel(color1.R, color2.R, blendAmt),
                BlendChannel(color1.G, color2.G, blendAmt),
                BlendChannel(color1.B, color2.B, blendAmt))
        };
    }

    private static byte BlendChannel(int channelA, int channelB, byte blendAmt)
    {
        return (byte)(blendAmt * (channelB - channelA) / 256 + channelA);
    }
}
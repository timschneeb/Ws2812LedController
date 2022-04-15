using System.Diagnostics;
using System.Drawing;
using Iot.Device.Graphics;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core;

public class BitmapWrapper
{
    protected readonly BitmapImage? Image;
    protected readonly Color[] VirtualCopy;

    public bool ExclusiveMode { set; get; } = false;
    public int Width { get; }
    public Color[] State => VirtualCopy;

    public bool[] MutedPixels { get; }
    public BitmapWrapper(BitmapImage image)
    {
        Image = image;
        Width = image.Width;
        VirtualCopy = new Color[image.Width];
        MutedPixels = new bool[image.Width];
        Clear();
    }

    /* Virtual mode */
    public BitmapWrapper(int width)
    {
        Width = width;
        VirtualCopy = new Color[width];
        MutedPixels = new bool[width];
        Clear();
    }

    public void SetPixel(int i, Color color, byte brightness = 255, bool gammaCorrection = false, bool isExclusive = false)
    {
        if (!isExclusive && ExclusiveMode || MutedPixels[i])
        {
            return;
        }
        
        color = gammaCorrection ? GammaCorrection.Gamma(color) : color;

        var colorWithBrightness = Color.FromArgb(color.A, (color.R * brightness) >> 8, (color.G * brightness) >> 8, (color.B * brightness) >> 8);
        Debug.Assert(i >= 0 && i < Width, "Out of range");
        Image?.SetPixel(i, 0, colorWithBrightness);
        VirtualCopy[i] = colorWithBrightness;
    }

    public void RedrawBuffer(int start, int length, byte brightness)
    {
        for(var i = start; i < start + length; i++)
        {
            SetPixel(i, VirtualCopy[i], brightness);
        }
    }

    // TODO does not respect muted pixels
    public void CopyFrom(BitmapWrapper canvas, int start, int length, bool blend)
    {
        for (var i = 0; i < length; i++)
        {
            var color = canvas.PixelAt(i);
            if (blend)
            {
                color = ColorBlend.Blend(VirtualCopy[start + i], color, color.A, true);
            }
            
            Image?.SetPixel(start + i, 0, color);
            VirtualCopy[start + i] = color;
        }
    }

    public Color PixelAt(int i)
    {
        Debug.Assert(i >= 0 && i < Width, "Out of range");
        return VirtualCopy[i];
    }

    public void Clear(Color? color = null, bool isExclusive = false)
    {

        for (var i = 0; i < Width; i++)
        {
            SetPixel(i, color ?? Color.Black);
        }
    }
}
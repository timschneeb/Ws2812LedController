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

    public int Width { get; }
    public Color[] State => VirtualCopy;

    public BitmapWrapper(BitmapImage image)
    {
        Image = image;
        Width = image.Width;
        VirtualCopy = new Color[image.Width];
        Clear();
    }

    /* Virtual mode */
    public BitmapWrapper(int width)
    {
        Width = width;
        VirtualCopy = new Color[width];
        Clear();
    }

    public void SetPixel(int i, Color color, byte brightness = 255, bool gammaCorrection = false)
    {
        color = gammaCorrection ? GammaCorrection.Gamma(color) : color;

        var colorWithBrightness = Color.FromArgb(color.A, (color.R * brightness) >> 8, (color.G * brightness) >> 8, (color.B * brightness) >> 8);
        Debug.Assert(i >= 0 && i < Width);
        Image?.SetPixel(i, 0, colorWithBrightness);
        VirtualCopy[i] = color;
    }

    public void RedrawBuffer(int start, int length, byte brightness)
    {
        for(var i = start; i < start + length; i++)
        {
            SetPixel(i, VirtualCopy[i], brightness);
        }
    }

    public Color PixelAt(int i)
    {
        Debug.Assert(i >= 0 && i < Width);
        return VirtualCopy[i];
    }

    public void Clear(Color? color = null)
    {
        Image?.Clear(color ?? Color.Black);
        VirtualCopy.Populate(color ?? Color.Black);
    }
}
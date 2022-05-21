using ScreenCapture.Base;

namespace Ws2812LedController.Ambilight;

public class LedPixel
{
    public LedPixel(int offsetX, int offsetY, int width, int height)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
        Width = width;
        Height = height;
        var sub = new List<Point>();
        for (var y = offsetY; y < offsetY + height; y++)
        {
            for (var x = offsetX; x < offsetX + width; x++)
            {
                sub.Add(new Point(x,y));
            }
        }

        SubPixels = sub.ToArray();
    }

    public int OffsetX { get; }
    public int OffsetY { get; }
    public int Width { get; }
    public int Height { get; }
    
    /* Coordinates */
    public Point[] SubPixels { get; }
}
using System.Diagnostics;
using Newtonsoft.Json;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Ambilight;

public enum LedDirection
{
    Horizontal,
    Vertical
}

public class LedZone
{
    public LedZone(string name, int offsetX, int offsetY, int width, int height, int ledCount, LedDirection direction)
    {
        Name = name;
        OffsetX = offsetX;
        OffsetY = offsetY;
        Width = width;
        Height = height;
        LedCount = ledCount;
        Direction = direction;
        
        var pixels = new List<LedPixel>();
        switch (Direction)
        {
            case LedDirection.Horizontal:
            {
                for (var led = 0; led < LedCount; led++)
                {
                    var column = led.Map(0, LedCount - 1, 0, Width);
                    var columnNext = (led + 1).Map(0, LedCount - 1, 0, Width);
                    pixels.Add(new LedPixel(offsetX + column, offsetY, columnNext - column, height));
                }
                break;
            }
            case LedDirection.Vertical:
            {
                for (var led = 0; led < LedCount; led++)
                {
                    var row = led.Map(0, LedCount - 1, 0, Height);
                    var rowNext = (led + 1).Map(0, LedCount - 1, 0, Height);
                    var heightPx = Height / LedCount;
                    pixels.Add(new LedPixel(offsetX, offsetY + heightPx * led, width, heightPx));
                }
                break;
            }
        }
        
        Pixels = pixels.ToArray();
        Debug.Assert(Pixels.Length == LedCount);
    }
    
    [JsonIgnore]
    public LedPixel[] Pixels { get; }
    
    public string Name { get; }
    public int OffsetX { get; }
    public int OffsetY { get; }
    public int Width { get; }
    public int Height { get; }
    public int LedCount { get; }
    public LedDirection Direction { get; }
}
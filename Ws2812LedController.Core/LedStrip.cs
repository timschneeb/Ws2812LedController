using System.Device.Spi;
using System.Diagnostics;
using System.Drawing;
using Iot.Device.Ws28xx;

namespace Ws2812LedController.Core;

public class LedStrip
{
    public LedSegment FullSegment { get; }

    public event EventHandler<Color[]>? ActiveCanvasChanged; 
    
    internal BitmapWrapper Canvas { get; }

    private readonly Ws28xx? _device;
    private readonly ICustomStrip? _customDevice;

    public LedStrip(int width, bool virtualized = false)
    {
        if (virtualized)
        {
            Canvas = new BitmapWrapper(width);
        }
        else
        {
            var settings = new SpiConnectionSettings(0, 0)
            {
                ClockFrequency = 2_400_000,
                Mode = SpiMode.Mode0,
                DataBitLength = 8
            };

            var spi = SpiDevice.Create(settings);
            _device = new Ws2812b(spi, width);
            Canvas = new BitmapWrapper(_device.Image);
        }

        FullSegment = new LedSegment(0, width, this);
    }
    
    public LedStrip(ICustomStrip customStrip)
    {
        _customDevice = customStrip;
        Canvas = customStrip.Canvas;
        FullSegment = new LedSegment(0, customStrip.Canvas.Width, this);
    }
    
    public LedSegment CreateSegment(int start, int length)
    {
        Debug.Assert(start + length <= Canvas.Width);
        return new LedSegment(start, length, this);
    }
    
    public void Render()
    {
        _device?.Update();
        _customDevice?.Render();
        ActiveCanvasChanged?.Invoke(this, Canvas.State);
    }
}
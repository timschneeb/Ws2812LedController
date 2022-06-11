using Ws2812LedController.Core;

namespace Ws2812LedController.UdpServer;

public class RemoteLedDevice : ILedDevice
{
    private readonly RemoteLedCanvas _canvas;
    public BitmapWrapper Canvas => _canvas.Bitmap;
    
    public RemoteLedDevice(RemoteLedCanvas remoteLedCanvas)
    {
        _canvas = remoteLedCanvas;
    }
    
    public void Render()
    {
        _canvas.Render();
    }
    
    public double Voltage => 0;
    public double AmpsPerSubpixel => 0;
}
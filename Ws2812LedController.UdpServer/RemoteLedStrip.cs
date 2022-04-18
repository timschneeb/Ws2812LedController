using Ws2812LedController.Core;

namespace Ws2812LedController.UdpServer;

public class RemoteLedStrip : ICustomStrip
{
    private readonly RemoteLedCanvas _canvas;
    public BitmapWrapper Canvas => _canvas.Bitmap;
    
    public RemoteLedStrip(RemoteLedCanvas remoteLedCanvas)
    {
        _canvas = remoteLedCanvas;
    }
    
    public void Render()
    {
        _canvas.Render();
    }
}
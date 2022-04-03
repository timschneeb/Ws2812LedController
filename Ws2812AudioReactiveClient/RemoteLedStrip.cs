using Ws2812LedController.Core;
using Ws2812LedController.UdpServer;

namespace Ws2812AudioReactiveClient;

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
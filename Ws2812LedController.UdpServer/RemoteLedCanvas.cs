using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.UdpServer.Packets;

namespace Ws2812LedController.UdpServer;

public class RemoteLedCanvas
{
    private readonly LayerId _layer;
    private readonly int _offset;
    private readonly int _length;

    public BitmapWrapper Bitmap { get; }
    public event EventHandler<IPacket>? NewPacketAvailable;

    public RemoteLedCanvas(LayerId layer, int offset, int length)
    {
        _layer = layer;
        _offset = offset;
        _length = length;
        Bitmap = new BitmapWrapper(length);
    }
    
    public void Render()
    {
        var colors = new uint[_length];
        for (var i = 0; i < _length; i++)
        {
            colors[i] = Bitmap.PixelAt(i).ToUInt32();
        }

        NewPacketAvailable?.Invoke(this, new PaintInstructionPacket
        {
            Layer = _layer,
            PaintInstructionMode = PaintInstructionMode.Full,
            Colors = colors
        });
    }
}
using Ws2812LedController.Core.Model;
using Ws2812LedController.UdpServer.Model;

namespace Ws2812LedController.UdpServer.Packets;

public class ClearCanvasPacket : IPacket
{
    public LayerId Layer { set; get; }
    public uint Color { set; get; }
    
    public PacketTypeId TypeId => PacketTypeId.ClearCanvas;
    public uint Size => sizeof(byte) + sizeof(uint);
    
    public byte[] Encode()
    {
        var buffer = new byte[Size];
        buffer[0] = (byte)Layer;
        Array.Copy(BitConverter.GetBytes(Color), 0, buffer, 1, sizeof(uint));
        return PacketWrapper.Encode(TypeId, buffer);
    }

    void IPacket.Decode(byte[] payload)
    {
        Layer = (LayerId)payload[0];
        Color = BitConverter.ToUInt32(payload, 1);
    }
}
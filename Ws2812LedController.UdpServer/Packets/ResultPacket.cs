using Ws2812LedController.UdpServer.Model;

namespace Ws2812LedController.UdpServer.Packets;

public class ResultPacket : IPacket
{
    public PacketTypeId SourcePacketId { set; get; }
    public byte Result { set; get; }
    
    public PacketTypeId TypeId => PacketTypeId.Result;
    public uint Size => sizeof(PacketTypeId) + sizeof(byte);
    
    public byte[] Encode()
    {
        var buffer = new byte[Size];
        buffer[0] = (byte)SourcePacketId;
        buffer[1] = Result;
        return PacketWrapper.Encode(TypeId, buffer);
    }

    void IPacket.Decode(byte[] payload)
    {
        SourcePacketId = (PacketTypeId)payload[0];
        Result = payload[1];
    }
}
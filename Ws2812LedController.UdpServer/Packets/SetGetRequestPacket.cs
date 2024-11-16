using Ws2812LedController.UdpServer.Model;

namespace Ws2812LedController.UdpServer.Packets;

public enum SetGetRequestId {
    Brightness,
    Power
}

public enum SetGetAction {
    Get,
    Set
}

public class SetGetRequestPacket : IPacket
{
    public SetGetRequestId Key { set; get; }
    public SetGetAction Action { set; get; }
    public uint Value { set; get; }
    
    public PacketTypeId TypeId => PacketTypeId.SetGetRequest;
    public uint Size => sizeof(byte) + sizeof(byte) + sizeof(uint);
    
    public byte[] Encode()
    {
        var buffer = new byte[Size];
        buffer[0] = (byte)Key;
        buffer[1] = (byte)Action;
        Array.Copy(BitConverter.GetBytes(Value), 0, buffer, 2, sizeof(uint));
        return PacketWrapper.Encode(TypeId, buffer);
    }

    void IPacket.Decode(byte[] payload)
    {
        Key = (SetGetRequestId)payload[0];
        Action = (SetGetAction)payload[1];
        Value = BitConverter.ToUInt32(payload, 2);
    }
}
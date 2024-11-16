using Ws2812LedController.UdpServer.Model;

namespace Ws2812LedController.UdpServer.Packets;

public interface IPacket
{
    public PacketTypeId TypeId { get; }
    /** Size for fixed-sized packages or minimum size for dynamic-sized packages */
    public uint Size { get; }

    public byte[] Encode();
    protected void Decode(byte[] payload);

    private static Dictionary<PacketTypeId,Type>? _registeredPackets;
    public static IPacket FromEnetPacket(ENet.Packet packet)
    {
        var buffer = new byte[packet.Length];
        packet.CopyTo(buffer);
        return FromBytes(buffer);
    }

    public static IPacket FromBytes(byte[] bytes)
    {
        /* Query all available packet handlers */
        if (_registeredPackets == null)
        {
            _registeredPackets = new Dictionary<PacketTypeId, Type>();
            
            var classTypes = typeof(IPacket).Assembly.GetTypes()
                .Where(t =>  t.Namespace == typeof(IPacket).Namespace)
                .Where(t => !t.IsAbstract && t.IsClass)
                .ToArray();
            
            foreach (var t in classTypes)
            {
                if (Activator.CreateInstance(t) is IPacket temp)
                {
                    _registeredPackets[temp.TypeId] = t;
                }
            }
        }

        var (id, payload) = PacketWrapper.Decode(bytes);
        if (Activator.CreateInstance(_registeredPackets[id]) is IPacket packet)
        {
            if (payload.Length < packet.Size)
            {
                throw new ArgumentException("Incoming packet is smaller than expected. Cannot handle");
            }
            
            packet.Decode(payload);
            return packet;
        }
        else
        {
            throw new InvalidOperationException($"No packet handler found for packet type: {id}");
        }
    }
}
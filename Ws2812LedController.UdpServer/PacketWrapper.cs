using Ws2812LedController.UdpServer.Model;

namespace Ws2812LedController.UdpServer
{
    internal static class PacketWrapper
    {
        public static byte[] Encode(PacketTypeId id, byte[] payload)
        {
            var msg = new byte[sizeof(byte) + payload.Length];
            var size = BitConverter.GetBytes((ushort)payload.Length);
            
            msg[0] = (byte)id;
            Array.Copy(payload, 0, msg, 1, payload.Length);
            return msg;
        }
        
        public static (PacketTypeId id, byte[] payload) Decode(byte[] raw)
        {
            if (raw.Length < 1)
            {
                throw new ArgumentException($"Message too small (Length: {raw.Length})");
            }

            var payload = new byte[raw.Length - 1];
            Array.Copy(raw, 1, payload, 0, raw.Length - 1);
            return ((PacketTypeId)raw[0], payload);
        }
    }
}

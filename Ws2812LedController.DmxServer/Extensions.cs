using System.Net;
using System.Runtime.InteropServices;

namespace Ws2812LedController.DmxServer;

public static class Extensions
{
    public static Span<byte> AsSpan<T>(this ref T val, int length) where T : unmanaged
    {
        return MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref val, length));
    }
    
    #region Endian Helpers
    public static ushort ToNetworkOrder(this ushort value) => (ushort)IPAddress.HostToNetworkOrder((short)value);
    public static ushort ToHostOrder(this ushort value) => (ushort)IPAddress.NetworkToHostOrder((short)value);
    public static uint ToNetworkOrder(this uint value) => (uint)IPAddress.HostToNetworkOrder((int)value);
    public static uint ToHostOrder(this uint value) => (uint)IPAddress.NetworkToHostOrder((int)value);
    #endregion
}
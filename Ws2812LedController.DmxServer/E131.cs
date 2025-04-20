using System.ComponentModel;
// ReSharper disable InconsistentNaming

namespace Ws2812LedController.DmxServer;

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;


public static unsafe class E131
{
    // Public Constants
    public const ushort DEFAULT_PORT = 5568;
    public const byte DEFAULT_PRIORITY = 0x64;

    // Private Constants
    private const ushort PREAMBLE_SIZE = 0x0010;
    private const ushort POSTAMBLE_SIZE = 0x0000;
    private static readonly byte[] ACN_PID = [0x41,0x53,0x43,0x2d,0x45,0x31,0x2e,0x31,0x37,0x00,0x00,0x00];
    private const uint ROOT_VECTOR = 0x00000004;
    private const uint FRAME_VECTOR = 0x00000002;
    private const byte DMP_VECTOR = 0x02;
    private const byte DMP_TYPE = 0xa1;
    private const ushort DMP_FIRST_ADDR = 0x0000;
    private const ushort DMP_ADDR_INC = 0x0001;

    /// <summary>
    /// E1.31 Packet Layout: union of structured layers and raw buffer
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct E131Packet
    {       
        public RootLayer Root;
        public FrameLayer Frame;
        public DmpLayer Dmp;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RootLayer
    {
        public ushort PreambleSize;
        public ushort PostambleSize;
        public fixed byte AcnPid[12];
        public ushort FlagsLength;
        public uint Vector;
        public fixed byte Cid[16];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FrameLayer
    {
        public ushort FlagsLength;
        public uint Vector;
        public fixed byte SourceName[64];
        public byte Priority;
        public ushort Reserved;
        public byte SequenceNumber;
        public byte Options;
        public ushort Universe;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DmpLayer
    {
        public ushort FlagsLength;
        public byte Vector;
        public byte Type;
        public ushort FirstAddress;
        public ushort AddressIncrement;
        public ushort PropertyValueCount;
        public fixed byte PropertyValues[513];
    }

    public enum E131Option : byte
    {
        Terminated = 6,
        Preview = 7
    }

    #region Socket Helpers
    public static Socket CreateSocket()
    {
        return new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    public static void Bind(Socket sock, ushort port)
    {
        sock.Bind(new IPEndPoint(IPAddress.Any, port));
    }

    public static IPEndPoint UnicastDest(string host, ushort port)
    {
        ArgumentException.ThrowIfNullOrEmpty(host);
        var addr = Dns.GetHostAddresses(host).First(a => a.AddressFamily == AddressFamily.InterNetwork);
        return new IPEndPoint(addr, port);
    }

    public static IPEndPoint MulticastDest(ushort universe, ushort port)
    {
        if (universe is < 1 or > 63999) 
            throw new ArgumentOutOfRangeException(nameof(universe));
        
        var addr = new byte[]{ 239, 255, (byte)(universe >> 8), (byte)(universe & 0xFF) };
        return new IPEndPoint(new IPAddress(addr), port);
    }

    public static void JoinMulticast(Socket sock, ushort universe, int ifIndex = 0)
    {
        var group = MulticastDest(universe, DEFAULT_PORT).Address;
        sock.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
            new MulticastOption(group, ifIndex));
    }
    #endregion

    #region Packet Operations
    public static bool GetOption(E131Packet* pkt, E131Option opt)
    {
        if (pkt == null) return false;
        return (pkt->Frame.Options & (1 << ((byte)opt % 8))) != 0;
    }

    public static int SetOption(E131Packet* pkt, E131Option opt, bool state)
    {
        if (pkt == null) return -1;
        var mask = (byte)(1 << ((byte)opt % 8));
        pkt->Frame.Options ^= (byte)((-Convert.ToByte(state) ^ pkt->Frame.Options) & mask);
        return 0;
    }

    public static int Send(Socket sock, E131Packet* pkt, IPEndPoint dest)
    {
        if (pkt == null)
            return -1;
                
        return sock.SendTo(pkt->AsSpan(638), SocketFlags.None, dest);
    }

    public static int Receive(Socket sock, E131Packet* pkt)
    {
        if (pkt == null) return -1;
        EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        
        // Directly passing span causes native crash
        var buffer = new byte[638];
        var cnt = sock.ReceiveFrom(buffer, SocketFlags.None, ref ep);
        buffer.CopyTo(pkt->AsSpan(638));
        return cnt;
    }

    public static E131Error Validate(E131Packet* pkt)
    {
        if (pkt == null) return E131Error.NullPtr;
        if (pkt->Root.PreambleSize.ToHostOrder() != PREAMBLE_SIZE) return E131Error.PreambleSize;
        if (pkt->Root.PostambleSize.ToHostOrder() != POSTAMBLE_SIZE) return E131Error.PostambleSize;
        for (var i = 0; i < 12; i++)
            if (pkt->Root.AcnPid[i] != ACN_PID[i]) return E131Error.AcnPid;
        if (pkt->Root.Vector.ToHostOrder() != ROOT_VECTOR) return E131Error.VectorRoot;
        if (pkt->Frame.Vector.ToHostOrder() != FRAME_VECTOR) return E131Error.VectorFrame;
        if (pkt->Dmp.Vector != DMP_VECTOR) return E131Error.VectorDmp;
        if (pkt->Dmp.Type != DMP_TYPE) return E131Error.TypeDmp;
        if (pkt->Dmp.FirstAddress.ToNetworkOrder() != DMP_FIRST_ADDR) return E131Error.FirstAddrDmp;
        if (pkt->Dmp.AddressIncrement.ToNetworkOrder() != DMP_ADDR_INC) return E131Error.AddrIncDmp;
        return E131Error.None;
    }

    public static bool ShouldDiscard(E131Packet* pkt, byte lastSeq)
    {
        if (pkt == null)
            return true;
        
        var diff = pkt->Frame.SequenceNumber - lastSeq;
        return diff is > -20 and <= 0;
    }
    #endregion
}
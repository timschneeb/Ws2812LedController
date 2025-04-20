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
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct E131Packet
    {
        [FieldOffset(0)] public PacketLayers Layers;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PacketLayers
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
    public static int InitPacket(E131Packet* pkt, ushort universe, ushort numSlots)
    {
        if (pkt == null || universe < 1 || universe > 63999 || numSlots < 1 || numSlots > 512)
            return -1;
        
        // Compute lengths
        var propCount = (ushort)(numSlots + 1);
        var dmpLength = (ushort)(propCount + Marshal.OffsetOf<DmpLayer>(nameof(DmpLayer.PropertyValues)).ToInt32());
        var frameLength = (ushort)(Marshal.SizeOf<FrameLayer>() + dmpLength);
        var rootLength = (ushort)(Marshal.SizeOf<RootLayer>() - sizeof(ushort)*2 + // exclude flength fields
                                  frameLength + sizeof(uint) + 16);
        
        pkt->AsSpan(638).Clear();

        // Root Layer
        pkt->Layers.Root.PreambleSize = PREAMBLE_SIZE;
        pkt->Layers.Root.PostambleSize = POSTAMBLE_SIZE;
        for (var i = 0; i < 12; i++)
            pkt->Layers.Root.AcnPid[i] = ACN_PID[i];
        pkt->Layers.Root.FlagsLength = (ushort)(0x7000 | rootLength);
        pkt->Layers.Root.Vector = ROOT_VECTOR;

        // Framing Layer
        pkt->Layers.Frame.FlagsLength = (ushort)(0x7000 | frameLength);
        pkt->Layers.Frame.Vector = FRAME_VECTOR;
        pkt->Layers.Frame.Priority = DEFAULT_PRIORITY;
        pkt->Layers.Frame.Universe = universe;

        // DMP Layer
        pkt->Layers.Dmp.FlagsLength = (ushort)(0x7000 | dmpLength);
        pkt->Layers.Dmp.Vector = DMP_VECTOR;
        pkt->Layers.Dmp.Type = DMP_TYPE;
        pkt->Layers.Dmp.FirstAddress = DMP_FIRST_ADDR;
        pkt->Layers.Dmp.AddressIncrement = DMP_ADDR_INC;
        pkt->Layers.Dmp.PropertyValueCount = propCount;

        return 0;
    }

    public static bool GetOption(E131Packet* pkt, E131Option opt)
    {
        if (pkt == null) return false;
        return (pkt->Layers.Frame.Options & (1 << ((byte)opt % 8))) != 0;
    }

    public static int SetOption(E131Packet* pkt, E131Option opt, bool state)
    {
        if (pkt == null) return -1;
        var mask = (byte)(1 << ((byte)opt % 8));
        pkt->Layers.Frame.Options ^= (byte)((-Convert.ToByte(state) ^ pkt->Layers.Frame.Options) & mask);
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
        return sock.ReceiveFrom(pkt->AsSpan(638), SocketFlags.None, ref ep);
    }

    public static E131Error Validate(E131Packet* pkt)
    {
        if (pkt == null) return E131Error.NullPtr;
        if (pkt->Layers.Root.PreambleSize.ToHostOrder() != PREAMBLE_SIZE) return E131Error.PreambleSize;
        if (pkt->Layers.Root.PostambleSize.ToHostOrder() != POSTAMBLE_SIZE) return E131Error.PostambleSize;
        for (var i = 0; i < 12; i++)
            if (pkt->Layers.Root.AcnPid[i] != ACN_PID[i]) return E131Error.AcnPid;
        if (pkt->Layers.Root.Vector.ToHostOrder() != ROOT_VECTOR) return E131Error.VectorRoot;
        if (pkt->Layers.Frame.Vector.ToHostOrder() != FRAME_VECTOR) return E131Error.VectorFrame;
        if (pkt->Layers.Dmp.Vector != DMP_VECTOR) return E131Error.VectorDmp;
        if (pkt->Layers.Dmp.Type != DMP_TYPE) return E131Error.TypeDmp;
        if (pkt->Layers.Dmp.FirstAddress.ToNetworkOrder() != DMP_FIRST_ADDR) return E131Error.FirstAddrDmp;
        if (pkt->Layers.Dmp.AddressIncrement.ToNetworkOrder() != DMP_ADDR_INC) return E131Error.AddrIncDmp;
        return E131Error.None;
    }

    public static bool ShouldDiscard(E131Packet* pkt, byte lastSeq)
    {
        if (pkt == null)
            return true;
        
        var diff = pkt->Layers.Frame.SequenceNumber - lastSeq;
        return diff is <= -20 or > 0;
    }
    #endregion
}
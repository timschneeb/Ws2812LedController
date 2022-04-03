using System.Diagnostics;
using Ws2812LedController.Core.Model;
using Ws2812LedController.UdpServer.Model;

namespace Ws2812LedController.UdpServer.Packets;

public enum PaintInstructionMode
{ 
    /** Packet contains color data for every LED, no indices sent */
    Full = 0,
    /** Packet contains color data and a corresponding identifier for each LED */
    Selective = 1
}

public class PaintInstructionPacket : IPacket
{
    public LayerId Layer { set; get; }
    public PaintInstructionMode PaintInstructionMode { set; get; }
    public ushort[] Indices { set; get; } = Array.Empty<ushort>();
    public uint[] Colors { set; get; } = Array.Empty<uint>();
    
    public PacketTypeId TypeId => PacketTypeId.PaintInstruction;
    /* Minimum size */
    public uint Size => sizeof(byte) + sizeof(byte) + sizeof(ushort) ;
    
    public byte[] Encode()
    {
        if (PaintInstructionMode == PaintInstructionMode.Selective)
        {
            Debug.Assert(Indices.Length == Colors.Length);
        }

        var pixelCount = (ushort)Colors.Length;
        var payloadSize = Size +  /* colors */ (pixelCount * sizeof(uint));
        if (PaintInstructionMode == PaintInstructionMode.Selective)
        {
            /* indices */
            payloadSize += (pixelCount * sizeof(ushort));
        }
        
        var buffer = new byte[payloadSize];
        buffer[0] = (byte)Layer;
        buffer[1] = (byte)PaintInstructionMode;
        
        /* copy pixel amount */
        Array.Copy(BitConverter.GetBytes(pixelCount), 0, buffer, 2, sizeof(ushort));

        switch (PaintInstructionMode)
        {
            case PaintInstructionMode.Full:
                Buffer.BlockCopy(Colors, 0, buffer, 4, Colors.Length * sizeof(uint));
                break;
            case PaintInstructionMode.Selective:
                Buffer.BlockCopy(Indices, 0, buffer, 4, Indices.Length * sizeof(ushort));
                Buffer.BlockCopy(Colors, 0, buffer, 4 + Indices.Length * sizeof(ushort), Colors.Length * sizeof(uint));
                break;
        }
        
        return PacketWrapper.Encode(TypeId, buffer);
    }

    void IPacket.Decode(byte[] payload)
    {
        var pixelCount = BitConverter.ToUInt16(payload, 2);
        
        Layer = (LayerId)payload[0];
        PaintInstructionMode = (PaintInstructionMode)payload[1];
        Indices = new ushort[pixelCount];
        Colors = new uint[pixelCount];
        
        switch (PaintInstructionMode)
        {
            case PaintInstructionMode.Full:
                Buffer.BlockCopy(payload, 4, Colors, 0, pixelCount * sizeof(uint));
                break;
            case PaintInstructionMode.Selective:
                Buffer.BlockCopy(payload, 4, Indices, 0, pixelCount * sizeof(ushort));
                Buffer.BlockCopy(payload, 4 + pixelCount * sizeof(ushort), Colors, 0, pixelCount * sizeof(uint));
                break;
        }
    }
}
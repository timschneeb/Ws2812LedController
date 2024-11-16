namespace Ws2812LedController.UdpServer.Model;

public enum PacketTypeId : byte
{
    Result = 0,
    SetGetRequest = 1,
    ClearCanvas = 2,
    PaintInstruction = 3,
}
using System;
using System.Text;
using System.Threading.Tasks;
using ENet;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.UdpServer.Packets;

namespace UdpProtocolClient;

public static class Entrypoint
{

    public static async Task Main()
    {
        using Host client = new Host();
        Address address = new Address();

        //address.SetHost("127.0.0.1");
        address.SetHost("192.168.178.56");
        address.Port = 32670;
        client.Create();

        var peer = client.Connect(address);

        var length = 124 + 79 * 2 + 81;
        var indices = new ushort[length];
        var colors = new uint[length];
        var instruction = new PaintInstructionPacket();
        for (var i = 0; i < length; i++)
        {
            indices[i] = (ushort)i;
            colors[i] = 0x00000000;
        }
        instruction.Indices = indices;
        instruction.Layer = LayerId.ExclusiveEnetLayer;

        while (!Console.KeyAvailable) {
            var polled = false;

            while (!polled) {
                if (client.CheckEvents(out var netEvent) <= 0) 
                {
                    if (client.Service(15, out netEvent) <= 0)
                    {
                        {
                            colors[0 + Random.Shared.Next(0, length - 1)] = ColorWheel.RandomColor().ToUInt32();
                            instruction.Colors = colors;

                            var responseEnet = default(Packet);
                            var payload = instruction.Encode();
                            responseEnet.Create(payload);
                            var hex = new StringBuilder(payload.Length * 2);
                            foreach (byte b in payload)
                                hex.Append($"{b:x2}");
                            Console.WriteLine(hex.ToString());
                                
                            peer.Send(netEvent.ChannelID, ref responseEnet);
                            responseEnet.Dispose();
                        }
                        break;
                    }

                    polled = true;
                }
                    

                switch (netEvent.Type) {
                    case EventType.None:
                        break;

                    case EventType.Connect:
                        Console.WriteLine("Client connected to server");
                        /*var packet = default(Packet);
                            var pkg = new SetGetRequestPacket()
                            {
                                Action = SetGetAction.Set,
                                Key = SetGetRequestId.Power,
                                Value = 0
                            };

                            packet.Create(pkg.Encode());
                            peer.Send(0, ref packet);*/
                        break;

                    case EventType.Disconnect:
                        Console.WriteLine("Client disconnected from server");
                        break;

                    case EventType.Timeout:
                        Console.WriteLine("Client connection timeout");
                        break;

                    case EventType.Receive:
                        Console.WriteLine("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                        /* var p = IPacket.FromEnetPacket(netEvent.Packet);
                            var ba = p.Encode();
                            var hex = new StringBuilder(ba.Length * 2);
                            foreach (byte b in ba)
                                hex.AppendFormat("{0:x2}", b);
                            Console.WriteLine(hex.ToString());*/
                            
                        netEvent.Packet.Dispose();
                        break;
                }
            }
        }

        client.Flush();
    }
}
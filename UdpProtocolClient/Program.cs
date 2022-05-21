using System.Collections.Concurrent;
using System.Drawing;
using System.Text;
using ENet;
using ScreenCapture.Base;
using Ws2812LedController.Ambilight;
using Ws2812LedController.Core.Model;
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
        var colors = new uint[length];
        var instruction = new PaintInstructionPacket();
        for (var i = 0; i < length; i++)
        {
            colors[i] = 0xFF000000;
        }

        instruction.PaintInstructionMode = PaintInstructionMode.Full;
        instruction.Layer = LayerId.ExclusiveEnetLayer;

        
        /*var processor = new ImageProcessingUnit()
        {
            Zones = new List<LedZone>()
            {
                new LedZone(0, 200, 20, 1080 - 200, 79, LedDirection.Vertical),
                new LedZone(1920 - 20, 200, 20, 1080 - 200, 79, LedDirection.Vertical),
            }
        };

        var smoothing = new LinearSmoothing();

        var unprocessed = new Color[length];
        Array.Fill(unprocessed, 0xFF000000.ToColor());
        processor.DataReady += (zone, colors1) =>
        {
            lock (unprocessed)
            {
                if (zone.OffsetX == 0)
                {
                    Array.Copy(colors1, 0, unprocessed, 124, colors1.Length);
                }
                else
                {
                    Array.Reverse(colors1);
                    Array.Copy(colors1, 0, unprocessed, 124 + 79, colors1.Length);
                }
           

                smoothing.PushColors(unprocessed.ToList());
                
            }
        };
        smoothing.DataReady += colors1 =>
        {
            lock (colors)
            {
            var isEqual = true;
            for (var i = 0; i < colors1.Count; i++)
            {
                if (colors[i] != colors1[i].ToUInt32())
                {
                    Console.WriteLine($"{colors[i].ToColor()} != {colors1[i]}");
                    isEqual = false;
                    break;
                }
            }

            Array.Fill(colors, (uint)0xFF000000);
            for (var i = 0; i < colors1.Length; i++)
            {

                colors[i] = colors1[i].ToUInt32();
                if (!isEqual) Console.Write($"{colors[i]:x2} ");
            }
            if(!isEqual) Console.WriteLine();
            if(!isEqual)  Console.WriteLine("---------------------");
        }
        };*/

  
        var amb = new AmbilightManager
        {
            LinearSmoothingOptions = new LinearSmoothingOptions()
            {
                UpdateIntervalHz = 60,
                SmoothingMode = LinearSmoothing.SmoothingType.Linear,
                SettlingTime = 30,
                AntiFlickeringThreshold = 64,  
                AntiFlickeringStep = 16,
                AntiFlickeringTimeout = 100
            }
        };

        amb.RegisterZone(new LedZone("left", 0, 50, 200, 1080 - 50, 79, LedDirection.Vertical), 
            output =>
            {
                for (var i = 0; i < output.Count; i++)
                {
                    colors[i + 124] = output[i].ToUInt32();
                }
            });
        amb.RegisterZone(new LedZone("right", 1920 - 200, 50, 200, 1080 - 50, 79, LedDirection.Vertical), 
            output =>
            {
                for (var i = 0; i < output.Count; i++)
                {
                    colors[i + 124 + 79] = output[output.Count - i - 1].ToUInt32();
                }
            });
        
        while (!Console.KeyAvailable) {
            var polled = false;

            while (!polled) {
                if (client.CheckEvents(out var netEvent) <= 0) 
                {
                    if (client.Service(14, out netEvent) <= 0)
                    {
                        {
                            lock (colors)
                            {
                                instruction.Colors = colors;
                            }

                            var responseEnet = default(Packet);
                            var payload = instruction.Encode();
                            responseEnet.Create(payload);
                            var hex = new StringBuilder(payload.Length * 2);
                                
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
using System.Text;
using ENet;
using Ws2812LedController.UdpServer.Packets;

namespace Ws2812LedController.UdpServer;

public class EnetServer
{
    private Task? _loop;
    private CancellationTokenSource _cancelSource = new();
    private readonly Host _server;

    public event Func<IPacket, IPacket?>? PacketReceived;
    
    public EnetServer(ushort port = 32670)
    {
        _server = new Host();
        var address = new Address
        {
            Port = port
        };
        
        _server.Create(address, 100);
    }

    public void Start()
    {
        _cancelSource.Cancel();
        _server.Flush();

        _cancelSource = new CancellationTokenSource();
        _loop = Task.Run(ReceiverLoop);
    }

    public async Task StopAsync()
    {
        _cancelSource.Cancel();
        await (_loop?.WaitAsync(CancellationToken.None) ?? Task.CompletedTask);
    }
    
    private void ReceiverLoop()
    {
        while (true) 
        {
            var polled = false;

            while (!polled) 
            {
                if (_server.CheckEvents(out var netEvent) <= 0) 
                {
                    if (_server.Service(5, out netEvent) <= 0)
                        break;

                    polled = true;
                }

                switch (netEvent.Type) 
                {
                    case EventType.None:
                        break;

                    case EventType.Connect:
                        Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        break;

                    case EventType.Disconnect:
                        Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        break;

                    case EventType.Timeout:
                        Console.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        break;

                    case EventType.Receive:
                        //Console.WriteLine("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                        var packet = IPacket.FromEnetPacket(netEvent.Packet);
                        /*var ba = packet.Encode();
                        var hex = new StringBuilder(ba.Length * 2);
                        foreach (byte b in ba)
                            hex.AppendFormat("{0:x2}", b);
                            
                        Console.WriteLine(hex.ToString());*/
                        
                        var response = PacketReceived?.Invoke(packet);
                        if (response != null)
                        {
                            var responseEnet = default(Packet);
                            responseEnet.Create(response.Encode());
                            netEvent.Peer.Send(netEvent.ChannelID, ref responseEnet);
                            responseEnet.Dispose();
                        }
                        
                        netEvent.Packet.Dispose();
                        break;
                }

                if (_cancelSource.Token.IsCancellationRequested)
                {
                    goto STOP_SERVER;
                }
            }
        }
        
        STOP_SERVER:
        _server.Flush();
    }
}
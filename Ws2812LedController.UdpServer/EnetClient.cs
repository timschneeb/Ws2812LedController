using System.Collections.Concurrent;
using System.Text;
using ENet;
using Ws2812LedController.UdpServer.Packets;

namespace Ws2812LedController.UdpServer;

public class EnetClient
{
    private readonly Host _client;
    private readonly Address _address;
    private Peer? _peer;
    
    private Task? _loop;
    private CancellationTokenSource _cancelSource = new();
    private ConcurrentQueue<IPacket> _queue { get; } = new();
    public event EventHandler<IPacket>? PacketReceived;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler? Timeout;

    public EnetClient(string ip, ushort port = 32670)
    {
        _client = new Host();

        _address = new Address();
        _address.SetHost(ip);
        _address.Port = port;
        _client.Create();
    }
    
    public void Connect()
    {
        _cancelSource.Cancel();
        _client.Flush();

        _cancelSource = new CancellationTokenSource();
        _loop = Task.Run(ClientLoop);
        
        _queue.Clear();
        
        _peer = _client.Connect(_address);
    }

    public async Task DisconnectAsync()
    {
        _cancelSource.Cancel();
       
        _peer?.DisconnectNow(0);
        _peer = null;

        await (_loop?.WaitAsync(CancellationToken.None) ?? Task.CompletedTask);
    }

    public void Enqueue(IPacket packet)
    {
        if (_cancelSource.IsCancellationRequested)
        {
            // Seal queue
            return;
        }
        
        _queue.Enqueue(packet);
    }
    
    public void ClientLoop()
    {
        while (!_cancelSource.IsCancellationRequested || !_queue.IsEmpty) 
        {
            var polled = false;

            while (!polled)
            {
                if (_queue.TryDequeue(out var nextPacket))
                {
                    var enetPacket = default(Packet);
                    var payload = nextPacket.Encode();
                    enetPacket.Create(payload);
                                
                    _peer?.Send(0, ref enetPacket);
                    enetPacket.Dispose();
                    
                    /* Debug stuff */
                    /*var hex = new StringBuilder(payload.Length * 2);
                    foreach (byte b in payload.AsSpan((362*4)-(80*4),80*4))
                        hex.AppendFormat("{0:x2}", b);
                    Console.WriteLine(hex.ToString());*/
                }

                if (_client.CheckEvents(out var netEvent) <= 0) 
                {
                    if (_client.Service(5, out netEvent) <= 0)
                    {
                        break;
                    }

                    polled = true;
                }
                
                switch (netEvent.Type) 
                {
                    case EventType.None:
                        break;

                    case EventType.Connect:
                        Console.WriteLine("EnetClient.ClientLoop: Client connected to server");
                        Connected?.Invoke(this, EventArgs.Empty);
                        break;

                    case EventType.Disconnect:
                        Console.WriteLine("EnetClient.ClientLoop: Client disconnected from server");
                        Disconnected?.Invoke(this, EventArgs.Empty);
                        break;

                    case EventType.Timeout:
                        Console.WriteLine("EnetClient.ClientLoop: Client connection timeout");
                        Timeout?.Invoke(this, EventArgs.Empty);
                        break;

                    case EventType.Receive:
                        Console.WriteLine($"EnetClient.ClientLoop: Packet received from server - Channel ID: {netEvent.ChannelID}, Data length: {netEvent.Packet.Length}");
                        PacketReceived?.Invoke(this, IPacket.FromEnetPacket(netEvent.Packet));
                        
                        /* var p = ;
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

        _client.Flush();
    }
}
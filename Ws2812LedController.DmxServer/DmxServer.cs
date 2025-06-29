using Ws2812LedController.Core;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Color = System.Drawing.Color;
using Timer = System.Timers.Timer;

namespace Ws2812LedController.DmxServer;

public class DmxServer
{
    private readonly int _ledCount;
    private readonly ushort _port;
    private readonly ushort _startUniverse;
    private readonly Ref<LedManager> _mgr;
    private readonly Task _loop;
    private readonly CancellationTokenSource _cancelSource = new();
    private readonly Timer _resetTimer = new(8000);
    
    private const ushort MaxLedsPerUniverse = 170;

    public bool DropPackets { get; set; }
    
    public DmxServer(Ref<LedManager> mgr, ushort port = E131.DEFAULT_PORT, ushort startUniverse = 1)
    {
        _ledCount = mgr.Value.GetFull().SegmentGroup.Width;
        _port = port;
        _startUniverse = startUniverse;
        _mgr = mgr;
        _loop = Task.Run(ServiceLoop);
        
        _resetTimer.Elapsed += (_, _) =>
        {
            // Reset the layer to the original transparent state
            _mgr.Value.GetFull().SegmentGroup.Clear(LayerId.ExclusiveEnetLayer);
        };
    }

    public async Task CancelAsync()
    {
        await _cancelSource.CancelAsync();
        _loop.Wait();
    }
    
    private unsafe void ServiceLoop()
    {
        using var sock = E131.CreateSocket();
        E131.Bind(sock, _port);
        var universeCount = Math.Ceiling(_ledCount / (float)MaxLedsPerUniverse);
        for (var i = 0; i < universeCount; i++)
        {
            E131.JoinMulticast(sock, (ushort)(_startUniverse + i));
        }
        Console.WriteLine($"Listening for E1.31/DMX packets on port {_port} in universe {_startUniverse} to {_startUniverse + universeCount - 1}...");
        
        // ReSharper disable once TooWideLocalVariableScope
        E131.E131Packet packet;
        byte lastSeq = 0;
        
        while (!_cancelSource.IsCancellationRequested)
        {
            // Receive
            var received = E131.Receive(sock, &packet);
            if (received < 0)
                throw new InvalidOperationException("e131_recv failed");

            // Validate
            var err = E131.Validate(&packet);
            if (err != E131Error.None)
            {
                Console.Error.WriteLine($"DMX: validation failed: {err.ToErrorString()}");
                continue;
            }

            if (lastSeq == packet.Frame.SequenceNumber)
            {
                continue;
            }
            
            // Discard out-of-order
            if (E131.ShouldDiscard(&packet, lastSeq))
            {
                Console.Error.WriteLine("DMX: packet out of order received");
                lastSeq = packet.Frame.SequenceNumber;
                continue;
            }
            
            lastSeq = packet.Frame.SequenceNumber;
            
            // If the size is not a multiple of 3, drop the extra bytes by rounding down
            var size = (packet.Dmp.PropertyValueCount.ToHostOrder() - 1) / 3;
            
            // Layer may be used by ENET, drop packets if requested
            if (DropPackets)
            {
                _resetTimer.Stop();
                continue;
            }
            
            var sector = packet.Frame.Universe.ToHostOrder() - _startUniverse;
            
            // Process DMX data
            for (var i = 1; i < Math.Min(size, _mgr.Value.GetFull().SegmentGroup.Width - 1); i++)
            {
                var r = packet.Dmp.PropertyValues[i * 3];
                var g = packet.Dmp.PropertyValues[i * 3 + 1];
                var b = packet.Dmp.PropertyValues[i * 3 + 2];
                var index = (i - 1) + sector * MaxLedsPerUniverse;
                
                try
                {
                    _mgr.Value.GetFull().SegmentGroup.SetPixel(index, Color.FromArgb(r, g, b), LayerId.ExclusiveEnetLayer);
                }
                catch (IndexOutOfRangeException e)
                {
                    Console.Error.WriteLine($"DMX: Pixel index out of range {e.Message}; sector: {sector}, i: {i}, resolvedIndex: {index}");
                }
            }

            if (_resetTimer.Enabled)
                // Reset the timer by re-assigning the interval
                _resetTimer.Interval = _resetTimer.Interval;
            else
                // Launch the timer if inactive
                _resetTimer.Start();
        }
    }
}
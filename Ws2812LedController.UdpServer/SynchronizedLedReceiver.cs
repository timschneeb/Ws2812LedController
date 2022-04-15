using System.Collections.Concurrent;
using System.Diagnostics;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.UdpServer.Packets;

namespace Ws2812LedController.UdpServer;

public class SynchronizedLedReceiver
{
    private readonly Ref<LedStrip> _strip;
    private readonly Ref<LedManager> _mgr;
    private Task _loop;
    private readonly CancellationTokenSource _cancelSource = new();
    private readonly ConcurrentQueue<PaintInstructionPacket> _instructionQueue = new();

    public SynchronizedLedReceiver(Ref<LedStrip> strip, Ref<LedManager> mgr)
    {
        _strip = strip;
        _mgr = mgr;
        _loop = Task.Run(ServiceLoop);
    }

    public void EnqueuePacket(PaintInstructionPacket packet)
    {
        if (_instructionQueue.Count > 5)
        {
            Console.WriteLine("SynchronizedLedReceiver: Can't keep up with remote, clearing queued instructions");
            _instructionQueue.Clear();
        }
        
        _instructionQueue.Enqueue(packet);
    }

    public async void ServiceLoop()
    {
        while (!_cancelSource.IsCancellationRequested)
        {
            if (_instructionQueue.TryDequeue(out var packet))
            {
                _strip.Value.Canvas.ExclusiveMode = packet.RenderMode == RenderMode.Direct;

                switch (packet.PaintInstructionMode)
                {
                    case PaintInstructionMode.Full:
                        for (var i = 0; i < packet.Colors.Length; i++)
                        {
                            if (packet.RenderMode == RenderMode.Direct)
                            {
                                _strip.Value.Canvas.SetPixel(i, packet.Colors[i].ToColor(), isExclusive: true);
                            }
                            else
                            {
                                _mgr.Value.Get("full")!.SegmentGroup.SetPixel(i, packet.Colors[i].ToColor(), packet.Layer);
                            }
                        }
                        break;
                    case PaintInstructionMode.Selective:
                        for (var i = 0; i < packet.Indices.Length; i++)
                        {
                            if (packet.RenderMode == RenderMode.Direct)
                            {
                                _strip.Value.Canvas.SetPixel(packet.Indices[i], packet.Colors[i].ToColor(), isExclusive: true);
                            }
                            else
                            {
                                _mgr.Value.Get("full")!.SegmentGroup.SetPixel(packet.Indices[i], packet.Colors[i].ToColor(), packet.Layer);
                            }
                        }
                        break;
                }
            }
            else
            {
                await Task.Delay(4);
            }
        }
    }
}
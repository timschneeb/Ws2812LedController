using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using Iot.Device.Amg88xx;
using Ws2812AudioReactiveClient.Dsp;
using Ws2812AudioReactiveClient.Effects;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Effects;
using Ws2812LedController.Core.Effects.Chase;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.UdpServer;
using Ws2812LedController.UdpServer.Packets;

namespace Ws2812AudioReactiveClient;

public static class Entrypoint
{
    private const int FrameRate = 60;
    
    public static async Task Main()
    {
        
        var canvas = new RemoteLedCanvas(LayerId.ExclusiveEnetLayer, 0, 363, RenderMode.ManagedTask);

        var mgr = new LedManager();
        var remote = new LedStrip(new RemoteLedStrip(canvas));

        var segmentBed = remote.CreateSegment(0, 124);
        var segmentDeskL = remote.CreateSegment(124, 79);
        var segmentDeskR = remote.CreateSegment(124+79, 79);
        var segmentHeater = remote.CreateSegment(124+79*2, 81);
        
        mgr.RegisterSegment("full", remote.FullSegment);
        mgr.RegisterSegment("bed", segmentBed);
        mgr.RegisterSegment("desk_left", segmentDeskL);
        mgr.RegisterSegment("desk_right", segmentDeskR);
        mgr.RegisterSegment("heater", segmentHeater);


        var client = new EnetClient("192.168.178.56");
        canvas.NewPacketAvailable += (_, packet) => client.Queue.Enqueue(packet); 
        client.Connect();
        
        await Task.Delay(100);

        canvas.Bitmap.Clear();
        canvas.Render();
        
        //await mgr.Get("bed")!.SetEffectAsync(new MeterRainbowReactiveEffect());

        var color = Color.FromArgb(0xFF, 0x20, 0x05, 0x00);
        
        /*await mgr.Get("desk_left")!.SetEffectAsync(new MeterRainbowReactiveEffect()
        {
            Multiplier = 2,
        });*/
        await mgr.Get("bed")!.SetEffectAsync(new FreqMapReactiveEffect()
        {
            Speed = 1000/FrameRate,
            //FadeStrength = 15,
            //StartFromEdge = Edge.None
        });
        await mgr.Get("heater")!.SetEffectAsync(new FreqMapReactiveEffect()
        {
            //FftBinSelector = new FftCBinSelector(0,3)
            //Multiplier = 0.3
            //FluentRainbow = true
        });
        //mgr.MirrorTo("desk_left", "bed");
        //mgr.MirrorTo("desk_left", "heater");
        //mgr.MirrorTo("desk_left", "desk_right");
        //mgr.Get("desk_left")!.SourceSegment.InvertX = true;
        //mgr.Get("bed")!.SourceSegment.InvertX = true;


        while (true)
        {
            await Task.Delay(1000);
        }
    }
}
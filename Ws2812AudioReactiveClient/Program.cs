using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using Ws2812AudioReactiveClient.Effects;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Effects.Chase;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.UdpServer;

namespace Ws2812AudioReactiveClient;

public static class Entrypoint
{

    
    public static async Task Main()
    {
        
        var canvas = new RemoteLedCanvas(LayerId.ExclusiveEnetLayer, 0, 363);

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
        
        await Task.Delay(500);
        
        //canvasFull.Bitmap.Clear();
        // canvasFull.Render();
        canvas.Bitmap.Clear();
        canvas.Render();
        //await mgr.Get("bed")!.SetEffectAsync(new MeterRainbowReactiveEffect());

        var color = Color.FromArgb(0xFF, 0x20, 0x05, 0x00);
        
        /*await mgr.Get("desk_left")!.SetEffectAsync(new MeterRainbowReactiveEffect()
        {
            AutomaticRender = false,
            Multiplier = 1.2
        });*/
        await mgr.Get("bed")!.SetEffectAsync(new NoiseCenteredReactiveEffect()
        {
            AutomaticRender = false,
            Speed = 1000/60
            //FluentRainbow = true
        }); 
        await mgr.Get("heater")!.SetEffectAsync(new MeterRainbowReactiveEffect()
        {
            AutomaticRender = false
            //Multiplier = 0.3
            //FluentRainbow = true
        });
        //mgr.MirrorTo("desk_left", "bed");
        //mgr.MirrorTo("desk_left", "heater");
        mgr.MirrorTo("desk_left", "desk_right");
        mgr.Get("desk_left")!.SourceSegment.InvertX = true;
        mgr.Get("bed")!.SourceSegment.InvertX = true;
        
        while (true)
        {
            //AudioProviderService.Instance.InjectSamples(a);
            
            canvas.Render();
            await Task.Delay(1000 / 60);
        }
    }
}
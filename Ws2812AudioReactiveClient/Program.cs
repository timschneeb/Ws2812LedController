using System.Drawing;
using Ws2812LedController.AudioReactive.Effects.Fft;
using Ws2812LedController.Core;
using Ws2812LedController.Core.FastLedCompatibility;
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
        var canvas = new RemoteLedCanvas(LayerId.ExclusiveEnetLayer, 0, /*124*/ 363, RenderMode.ManagedTask);
        var remote = new LedStrip(new RemoteLedDevice(canvas));
        var mgr = new LedManager(new Ref<LedStrip>(() => remote));
        
        var segmentBed = remote.CreateSegment(0, 124);
        var segmentDeskL = remote.CreateSegment(124, 79);
        var segmentDeskR = remote.CreateSegment(124+79, 79);
        var segmentHeater = remote.CreateSegment(124+79*2, 81);
        
        mgr.RegisterSegment("bed", segmentBed);
        mgr.RegisterSegment("desk_left", segmentDeskL);
        mgr.RegisterSegment("desk_right", segmentDeskR);
        mgr.RegisterSegment("heater", segmentHeater);

        var client = new EnetClient("192.168.178.56");
        canvas.NewPacketAvailable += (_, packet) => client.Enqueue(packet); 
        client.Connect();
        
        await Task.Delay(100);

        canvas.Bitmap.Clear();
        canvas.Render();
        
        await mgr.Get("bed")!.SetEffectAsync(new RipplePeakReactiveEffect()
        {
            Speed = 1000/FrameRate,
            FftCBinSelector = new(0),
            Threshold = 15,
            Palette = new CRGBPalette16(Color.Red, Color.ForestGreen, Color.DarkOrange, Color.DodgerBlue)

            // FftCBinSelector = new(4),
           // Threshold = 15
            //Palette = new(Color.SlateBlue),
            /*AvgSmoothingStrength = 5,
            Intensity = 3,
            EndFrequency = 120*/
            //FftBinSelector = new(0)
            //FadeStrength = 15,
            //StartFromEdge = Edge.None
        });
        await mgr.Get("heater")!.SetEffectAsync(new RipplePeakReactiveEffect()
        {
            Speed = 1000/FrameRate,
            FftCBinSelector = new(0),
            Threshold = 15,
            Palette = new CRGBPalette16(Color.Red, Color.ForestGreen, Color.DarkOrange, Color.DodgerBlue)
            
            //FadeStrength = 15,
            //StartFromEdge = Edge.None
        });
        await mgr.Get("desk_left")!.SetEffectAsync(new RipplePeakReactiveEffect()
        {
            Speed = 1000/FrameRate,
            FftCBinSelector = new(0),
            Threshold = 15,
            Palette = new CRGBPalette16(Color.Red, Color.ForestGreen, Color.DarkOrange, Color.DodgerBlue)
            //FftBinSelector = new FftCBinSelector(0,3)
            //Multiplier = 0.3
            //FluentRainbow = true
        });
        
        //mgr.MirrorTo("desk_left", "bed");
        //mgr.MirrorTo("desk_left", "heater");
        mgr.MirrorTo("desk_left", "desk_right");
        mgr.Get("desk_left")!.SourceSegment.InvertX = true;
        //mgr.Get("bed")!.SourceSegment.InvertX = true;

        while (true)
        {
            await Task.Delay(1000);
        }
    }
}
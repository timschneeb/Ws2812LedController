using System.Device.Spi;
using System.Diagnostics;
using System.Drawing;
using Iot.Device.Ws28xx;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core;

public class LedStrip
{
    public LedSegment FullSegment { get; }
    public List<LedSegment> SubSegments { get; } = new();

    public event EventHandler<Color[]>? ActiveCanvasChanged;

    public BitmapWrapper Canvas { get; }

    public int Framerate { set; get; } = 60;
    
    private readonly Ws28xx? _device;
    private readonly ICustomStrip? _customDevice;
    private readonly Task _renderTask;
    private readonly CancellationTokenSource _tokenSource = new();

    public LedStrip(int width, bool virtualized = false)
    {
        if (virtualized)
        {
            Canvas = new BitmapWrapper(width);
        }
        else
        {
            var settings = new SpiConnectionSettings(0, 0)
            {
                ClockFrequency = 2_400_000,
                Mode = SpiMode.Mode0,
                DataBitLength = 8
            };

            var spi = SpiDevice.Create(settings);
            _device = new Ws2812b(spi, width);
            Canvas = new BitmapWrapper(_device.Image);
        }

        _renderTask = Task.Run(RenderTask);
        FullSegment = new LedSegment(0, width, this);
    }

    public LedStrip(ICustomStrip customStrip)
    {
        _customDevice = customStrip;
        _renderTask = Task.Run(RenderTask);
        Canvas = customStrip.Canvas;
        FullSegment = new LedSegment(0, customStrip.Canvas.Width, this);
    }

    public LedSegment CreateSegment(int start, int length)
    {
        Debug.Assert(start + length <= Canvas.Width, "Start/length are out of range");
        var segment = new LedSegment(start, length, this);
        SubSegments.Add(segment);
        return segment;
    }

    public void RemoveSegment(LedSegment segment)
    {
        segment.Enabled = false;
        SubSegments.Remove(segment);
    }

    public bool HasPixelConflict(LedSegment source, int index)
    {
        foreach (var sub in SubSegments)
        {
            if (sub == source) continue;
            if (sub.AbsStart >= index && index <= sub.AbsEnd)
            {
                return true;
            }
        }

        return false;
    }

    private int _cnt = 0;

    private async void RenderTask()
    {
        var stopwatch = new Stopwatch();
        
        while (!_tokenSource.IsCancellationRequested)
        {
            stopwatch.Restart();
            Render();
            
            var millis = stopwatch.ElapsedMilliseconds;
            var wait = (int)(1000.0 / Framerate - millis);
            // Console.WriteLine($"DRIFT: {millis}ms ->\t{wait}");
            if (wait > 0)
            {
                await Task.Delay(wait, _tokenSource.Token);
            }
        }
    }
    
    private static readonly int _layerCount = typeof(LayerId).GetEnumValues().Length;

    protected void Render()
    {
        //lock (this)
        {
            _cnt++;
            var self = _cnt;
            //Console.WriteLine($"[Task {self}] Entered -------------------------------------");
            
            void PrintDebug(int layer, LedSegment segment, int segmentIdx, int pixel, Color cA, Color cB, Color cC)
            {
                 var msg =
                        $"[Task {self}] Layer={(LayerId)layer}\tSegment=({segmentIdx})->{segment.Id}\tPixel={pixel} \tColorOld={cA}\tColorCur={cB}\tA={cB.A} ->\t{cC}";
                    Console.WriteLine(msg);
            }
            
            for (var pxlIdx = 0; pxlIdx < Canvas.Width; pxlIdx++)
            {
                var finalPixel = Color.Black;
                for (var layer = 0; layer < _layerCount; layer++)
                {
                    var pixel = FullSegment.Layers[layer].IsActive
                        ? FullSegment.Layers[layer].LayerState[pxlIdx]
                        : Color.FromArgb(0, 0, 0, 0);
                    for (var segIdx = 0; segIdx < SubSegments.Count; segIdx++)
                    {
                        var seg = SubSegments[segIdx];
                        if (!seg.Layers[layer].IsActive || !seg.ContainsAbsolutePixel(pxlIdx))
                        {
                            continue;
                        }

                        var relPxlIdx = seg.ToRelativeIndex(pxlIdx);

                        //Console.WriteLine($"Segment.Id={seg.Id};\tAbsPixelIdx={pxlIdx} ->\tRelPixelIdx={relPxlIdx}");
                        var next = seg.Layers[layer].LayerState[relPxlIdx];
                        pixel = ColorBlend.Blend(pixel, next, next.A, false);

                        // PrintDebug(layer, seg, segIdx, pxlIdx, pixel, next, pixel);
                    }

                    finalPixel = ColorBlend.Blend(finalPixel, pixel, pixel.A, true);
                }
                Canvas.SetPixel(pxlIdx, finalPixel);
 
                //Console.WriteLine($"[Task {self}] Layer {(LayerId)layer} done -------------------------------------");
            }

            //Console.WriteLine($"[Task {self}] Exited -------------------------------------");
        }

        /* foreach (var seg in SubSegments)
         {
             for (var i = 0; i < Width; i++)
             {
                 var pixel = Layers[0].LayerState[i];
                 for (var j = 1; j < Layers.Length; j++)
                 {
                     if (!Layers[j].IsActive)
                     {
                         continue;
                     }
                 
                     var next = Layers[j].LayerState[i];
                     pixel = ColorBlend.Blend(pixel, next, next.A, true);
                 }
             
                 Strip.Canvas.SetPixel(RelStart + i, pixel, MaxBrightness, UseGammaCorrection);
             }
             //Canvas.CopyFrom(seg.Canvas, seg.RelStart, seg.Width, true);
         }*/
        _device?.Update();
        _customDevice?.Render();
        ActiveCanvasChanged?.Invoke(this, Canvas.State);
    }
}
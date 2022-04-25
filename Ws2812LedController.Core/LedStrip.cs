using System.Device.Spi;
using System.Diagnostics;
using System.Drawing;
using Iot.Device.Ws28xx;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core;

public class LedStrip : IDisposable
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

        FullSegment = new LedSegment(0, width, this);
        _renderTask = Task.Run(RenderTask);
    }

    public LedStrip(ICustomStrip customStrip)
    {
        _customDevice = customStrip;
        FullSegment = new LedSegment(0, customStrip.Canvas.Width, this);
        Canvas = customStrip.Canvas;
        _renderTask = Task.Run(RenderTask);
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

    private async void RenderTask()
    {
        var stopwatch = new Stopwatch();
        try
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                stopwatch.Restart();
                Render();

                var drift = stopwatch.ElapsedMilliseconds;
                var wait = (int)(1000.0 / Framerate - drift);
                // Console.WriteLine($"DRIFT: {drift}ms ->\t{wait}");
                if (wait > 0)
                {
                    try
                    {
                        await Task.Delay(wait, _tokenSource.Token);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
    
    private readonly int _layerCount = typeof(LayerId).GetEnumValues().Length;

    protected void Render()
    {
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
                    var next = seg.Layers[layer].LayerState[relPxlIdx];
                    pixel = ColorBlend.Blend(pixel, next, next.A, false);
                }

                finalPixel = ColorBlend.Blend(finalPixel, pixel, pixel.A, true);
            }

            Canvas.SetPixel(pxlIdx, finalPixel, FullSegment.MaxBrightness /* TODO brightness */);
        }

        _device?.Update();
        _customDevice?.Render();
        ActiveCanvasChanged?.Invoke(this, Canvas.State);
    }

    public async Task CancelAsync()
    {
        _tokenSource.Cancel();
        await _tokenSource.Token.WaitHandle.WaitOneAsync(1000, CancellationToken.None);
    }
    
    public void Dispose()
    {
        _tokenSource.Cancel();
        _renderTask.Dispose();
        _tokenSource.Dispose();
    }
}
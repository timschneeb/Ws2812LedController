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
    public BitmapWrapper Canvas => LedDevice.Canvas;
    public ILedDevice LedDevice { get; }
    public int Framerate { set; get; } = 60;

    private readonly Task _renderTask;
    private readonly CancellationTokenSource _tokenSource = new();

    public LedStrip(ILedDevice ledDevice)
    {
        LedDevice = ledDevice;
        FullSegment = new LedSegment(0, ledDevice.Canvas.Width, this);
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
                
                // Console.Clear();
                // Console.WriteLine(Math.Round(PowerConsumption, 2) + "W");
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
    
    private readonly int _layerCount = typeof(LayerId).GetEnumValues().Length;
    
    protected void Render()
    {
        Color GetMaskedPixel(LedSegment seg, int layer, int pxlIdx)
        {
            var c = seg.Layers[layer].LayerState[pxlIdx];
            var mask = seg.Layers[layer].Mask;
            return mask == null ? c : mask.Condition(c, pxlIdx, seg.Layers[layer].Width);
        }
        
        for (var pxlIdx = 0; pxlIdx < Canvas.Width; pxlIdx++)
        {
            LedSegment? primaryPixelOwner = null;
            var finalPixel = Color.Black;
            for (var layer = 0; layer < _layerCount; layer++)
            {
                var pixel = FullSegment.Layers[layer].IsActive
                    ? GetMaskedPixel(FullSegment, layer, pxlIdx)
                    : Color.FromArgb(0, 0, 0, 0);
                for (var segIdx = 0; segIdx < SubSegments.Count; segIdx++)
                {
                    var seg = SubSegments[segIdx];
                    if (!seg.Layers[layer].IsActive || !seg.ContainsAbsolutePixel(pxlIdx))
                    {
                        continue;
                    }
                    
                    primaryPixelOwner ??= seg;

                    var relPxlIdx = seg.ToRelativeIndex(pxlIdx);
                    var next = GetMaskedPixel(seg, layer, relPxlIdx);
                    pixel = ColorBlend.Blend(pixel, next, next.A, false);
                }

                finalPixel = ColorBlend.Blend(finalPixel, pixel, pixel.A, true);
            }

            Canvas.SetPixel(pxlIdx, finalPixel, primaryPixelOwner?.MaxBrightness ?? FullSegment.MaxBrightness);
        }
        
        LedDevice?.Render();
    }

    public async Task CancelAsync()
    {
        _tokenSource.Cancel();
        await _tokenSource.Token.WaitHandle.WaitOneAsync(1000, CancellationToken.None);
    }

    // TODO move to ICustomStrip class
    public double Voltage => 5.0;
    public double PowerConsumption /* in Watt */ => Voltage * Amperage;
    public double Amperage
    {
        get
        {
            const double ampsPerPixel = 0.02;
            var wattage = 0.0;
            for (var i = 0; i < Canvas.Width; i++)
            {
                var color = Canvas.PixelAt(i);
                wattage += ((color.R / 255.0) * ampsPerPixel) * (color.A / 255.0);
                wattage += ((color.G / 255.0) * ampsPerPixel) * (color.A / 255.0);
                wattage += ((color.B / 255.0) * ampsPerPixel) * (color.A / 255.0);
            }
            return wattage;
        }
    }

    public void Dispose()
    {
        _tokenSource.Cancel();
        _renderTask.Dispose();
        _tokenSource.Dispose();
    }
}
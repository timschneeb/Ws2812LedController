using System.Collections.Concurrent;
using ScreenCapture;
using ScreenCapture.Base;
using Color = System.Drawing.Color;

namespace Ws2812LedController.Ambilight;

public class ImageProcessingUnit : IDisposable
{
    public IScreenCapture ScreenCapture { get; } = ScreenCaptureFactory.Build();
    public int Framerate { set; get; } = 60;

    public IReadOnlyList<LedZone> Zones => _zones;

    public Processor Processor { get; } = new();
    
    public event Action<LedZone,Color[]>? DataReady;

    private readonly Task _task;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    private readonly ushort[] _lut = new ushort[256];
    private List<LedZone> _zones = new();

    public ImageProcessingUnit()
    {
        for (var i = 0; i < 256; i++)
        {
            _lut[i] = (ushort)(i * i);
        }
        _task = Task.Run(ProcessImageTask);
    }

    public void AddZone(LedZone zone)
    {
        lock (_zones)
        {
            _zones.Add(zone);
        }
    }
    
    public int RemoveZone(string name)
    {
        lock (_zones)
        {
            return _zones.RemoveAll(x => x.Name == name);
        }
    }

    private async Task ProcessImageTask()
    {
        try
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var image = ScreenCapture.GetImage();

                lock (_zones)
                {
                    foreach (var zone in _zones.ToList())
                    {
                        if (image.Height < zone.Height + zone.OffsetY || image.Width < zone.Width + zone.OffsetX)
                        {
                            Console.WriteLine("Zone out-of-range");
                            continue;
                        }

                        var colors = Processor.Process(image, zone, _lut);
                        DataReady?.Invoke(zone, colors);
                    }
                }

                image.Dispose();

                await Task.Delay(1000 / Framerate, _cancellationTokenSource.Token);
            }
        }
        catch(TaskCanceledException) {}
    }
    
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
    }
}
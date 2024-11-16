using System.Device.Spi;
using Iot.Device.Ws28xx;

namespace Ws2812LedController.Core.Devices;

public class Ws2812Device : ILedDevice
{
    private readonly Ws28xx _device;
    public Ws2812Device(int ledCount, SpiConnectionSettings? spiConnectionSettings = null)
    {
        var defaultSettings = new SpiConnectionSettings(0, 0)
        {
            ClockFrequency = 2_400_000,
            Mode = SpiMode.Mode0,
            DataBitLength = 8
        };

        var spi = SpiDevice.Create(spiConnectionSettings ?? defaultSettings);
        _device = new Ws2812b(spi, ledCount);
        Canvas = new BitmapWrapper(_device.Image);
    }
    
    public BitmapWrapper Canvas { get; }
    public void Render()
    {
        _device.Update();
    }
    
    public double Voltage => 5;
    public double AmpsPerSubpixel => 0.02;
}
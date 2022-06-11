using System.Device.Spi;
using Iot.Device.Ws28xx;

namespace Ws2812LedController.Core.Strips;

public class Ws2812Device : ILedDevice
{
    private readonly Ws28xx _device;
    public Ws2812Device(int ledCount)
    {
        var settings = new SpiConnectionSettings(0, 0)
        {
            ClockFrequency = 2_400_000,
            Mode = SpiMode.Mode0,
            DataBitLength = 8
        };

        var spi = SpiDevice.Create(settings);
        _device = new Ws2812b(spi, ledCount);
        Canvas = new BitmapWrapper(_device.Image);
    }
    
    public BitmapWrapper Canvas { get; }
    public void Render()
    {
        _device.Update();
    }
    
    public double Voltage => 5;
    public double AmpsPerPixel => 0.02;
}
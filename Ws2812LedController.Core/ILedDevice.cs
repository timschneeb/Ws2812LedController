namespace Ws2812LedController.Core;

public interface ILedDevice
{
    public BitmapWrapper Canvas { get; }
    public void Render();
    
    public double Voltage { get; }
    public double AmpsPerPixel { get; }
}
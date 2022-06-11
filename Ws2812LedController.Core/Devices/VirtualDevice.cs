using System.Drawing;

namespace Ws2812LedController.Core.Devices;

public class VirtualDevice : ILedDevice
{
    public event EventHandler<Color[]>? RenderEvent;
    
    public VirtualDevice(int ledCount)
    {
        Canvas = new BitmapWrapper(ledCount);
    }
    
    public BitmapWrapper Canvas { get; }
    public void Render()
    {
        RenderEvent?.Invoke(this, Canvas.State);
    }

    public double Voltage => 5;
    public double AmpsPerSubpixel => 0.02;
}
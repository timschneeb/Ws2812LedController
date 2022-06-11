using System.Drawing;

namespace Ws2812LedController.Core.Strips;

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
    public double AmpsPerPixel => 0.02;
}
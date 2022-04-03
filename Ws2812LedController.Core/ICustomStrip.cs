namespace Ws2812LedController.Core;

public interface ICustomStrip
{
    public BitmapWrapper Canvas { get; }
    public void Render();
}
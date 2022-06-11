namespace Ws2812LedController.Core;

public interface ILedDevice
{
    /**
     * <summary>Reference to the canvas implementation of the LED device</summary>
     */
    public BitmapWrapper Canvas { get; }
    /**
     * <summary>Commit the current state of the canvas to the LED device</summary>
     */
    public void Render();
    
    /**
     * <summary>LED strip voltage</summary>
     */
    public double Voltage { get; }
    /**
     * <summary>Amperage per subpixel</summary>
     * <remarks>This describes the amperage of one subpixel (R, G or B), not a whole pixel</remarks>
     */
    public double AmpsPerSubpixel { get; }
}
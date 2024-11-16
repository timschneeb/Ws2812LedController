using System.Drawing;

namespace Ws2812LedController.Core;

public class LedMask
{
    /**
     * <summary>Custom logic that modifies colors for specific pixels</summary>
     * <param name="original">Original color (Color)</param>
     * <param name="index">Index of current pixel (int)</param>
     * <param name="width">Width of current segment (int)</param>
     * <returns>Final color</returns>
     */
    public Func<Color, int /* index */, int /* width */, Color> Condition { set; get; }

    public LedMask(Func<Color, int, int, Color> condition)
    {
        Condition = condition;
    }
}
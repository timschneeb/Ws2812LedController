using System.Drawing;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812LedController.Core.Effects.Sparkle;

public class FlashSparkle : BaseSparkle
{
    public override string Description => "Lights all LEDs in the color. Flashes white pixels randomly.";

    public Color BackgroundColor
    {
        get => _colorBackground;
        set => _colorBackground = value;
    }

    public FlashSparkle()
    {
        // Set appropriate default value
        _colorBackground = Color.BlueViolet;
    }
}
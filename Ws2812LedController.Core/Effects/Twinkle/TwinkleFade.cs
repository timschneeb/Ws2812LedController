using System.Drawing;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812LedController.Core.Effects.Twinkle;

public class TwinkleFade : BaseTwinkleFade
{
    public override string FriendlyName => "Twinkle (fade)";
    public override string Description => "Blink several LEDs on, fading out";

    public Color ForegroundColor
    {
        get => _colorForeground;
        set => _colorForeground = value;
    }
    
    public Color BackgroundColor
    {
        get => _colorBackground;
        set => _colorBackground = value;
    }
}
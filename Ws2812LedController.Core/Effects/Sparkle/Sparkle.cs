using System.Drawing;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812LedController.Core.Effects.Sparkle;

public class Sparkle : BaseSparkle
{
    public override string FriendlyName => "Sparkle";

    public override string Description => "Blinks one LED at a time";

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
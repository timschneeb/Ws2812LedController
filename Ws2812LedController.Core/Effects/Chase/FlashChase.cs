using System.Drawing;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812LedController.Core.Effects.Chase;

public class FlashChase : BaseFlashChase
{
    public override string Description => "Flashes running on custom background";
    
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
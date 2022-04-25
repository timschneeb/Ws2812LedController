using System.Drawing;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812LedController.Core.Effects.Chase;

public class BlackoutChase : BaseChase
{
    public override string FriendlyName => "Blackout chase";
    public override string Description => "Black running on custom background";

    public BlackoutChase()
    {
        _colorA = _colorB = Color.Black;
        
        // Set appropriate default value
        _colorBackground = Color.CornflowerBlue;
    }
    
    public Color BackgroundColor
    {
        get => _colorBackground;
        set => _colorBackground = value;
    }
}
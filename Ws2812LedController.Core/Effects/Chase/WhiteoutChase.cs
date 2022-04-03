using System.Drawing;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812LedController.Core.Effects.Chase;

public class WhiteoutChase : BaseChase
{
    public override string Description => "White running on custom background";

    public WhiteoutChase()
    {
        _colorA = _colorB = Color.White;
        
        // Set appropriate default value
        _colorBackground = Color.CornflowerBlue;
    }
    
    public Color BackgroundColor
    {
        get => _colorBackground;
        set => _colorBackground = value;
    }
}
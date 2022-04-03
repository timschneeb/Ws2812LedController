using System.Drawing;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812LedController.Core.Effects.Chase;

public class BicolorChase : BaseChase
{
    public override string Description => "Bicolor chase mode";
    
    public Color ColorA
    {
        get => _colorA;
        set => _colorA = value;
    }

    public Color ColorB
    {
        get => _colorB;
        set => _colorB = value;
    }
    
    public Color BackgroundColor
    {
        get => _colorBackground;
        set => _colorBackground = value;
    }
}
using System.Drawing;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812LedController.Core.Effects.Chase;

public class TricolorChase : BaseTricolorChase
{
    public override string Description => "Tricolor chase mode";

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
    
    public Color ColorC
    {
        get => _colorC;
        set => _colorC = value;
    }
}
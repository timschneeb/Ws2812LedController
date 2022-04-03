using System.Drawing;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812LedController.Core.Effects.Firework;

public class Firework : BaseFirework
{
    public override string Description => "Firework sparks";
    
    public Color BackgroundColor
    {
        get => _colorBackground;
        set => _colorBackground = value;
    }
    
    public Color[] Colors
    {
        get => _colors;
        set => _colors = value;
    }
}
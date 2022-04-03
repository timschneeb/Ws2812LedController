using System.Drawing;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;

namespace Ws2812LedController.Core.Effects.Alternate;

public class Alternate : BaseAlternate
{
    public override string Description => "Alternating color pixels running";
    
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

    public void LoadPreset(AlternatePreset preset)
    {
        switch (preset)
        {
            case AlternatePreset.Halloween:
                _colorA = Color.Purple;
                _colorB = Color.Orange;
                break;
            case AlternatePreset.Christmas:
                _colorA = Color.Red;
                _colorB = Color.Green;
                break;
            case AlternatePreset.RedBlue:
                _colorA = Color.Red;
                _colorB = Color.Blue;
                break;
            case AlternatePreset.BlackWhite:
                _colorA = Color.Black;
                _colorB = Color.White;
                break;
        }
    }
}
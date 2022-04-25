using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Chase;

public class RainbowChase : BaseChase
{
    public override string FriendlyName => "Rainbow chase";
    public override string Description => "Custom color running on rainbow background";
    
    private Color _color = Color.White;
    public Color Color
    {
        set => _color = value;
        get => _color;
    }
    public bool IsInverted { set; get; }
    
    protected override Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var colorSep = 256 / segment.Width;
        var colorIndex = (byte)(Frame & 0xFF);
        var color = ColorWheel.ColorAtIndex((byte)(((StepCounter * colorSep) + colorIndex) & 0xFF));
        if (IsInverted)
        {
            _colorA = _colorB = color;
            _colorBackground = _color;
        }
        else
        {
            _colorA = _colorB = _color;
            _colorBackground = color;
        }
        
        return base.PerformFrameAsync(segment, layer);
    }
}
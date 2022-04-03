using System.Diagnostics;
using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Base;

public abstract class BaseFirework : IEffect
{
    public override int Speed { get; set; } = 1000;
    
    public SizeOption Size { get; set; } = SizeOption.Small;
    public FadeRate FadeRate { get; set; } = FadeRate.None;

    protected Color[] _colors = { 
        Color.Red,
        Color.Orange,
        Color.DodgerBlue,
        Color.ForestGreen
    };
    
    protected Color _colorBackground = Color.Black;

    private bool _triggered = false;

    public override void Reset()
    {
        _triggered = false;
        base.Reset();
    }

    public void Trigger()
    {
        _triggered = true;
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        Fireworks.DoFrame(segment, _colors, _colorBackground, FadeRate, Size, _triggered, layer);

        CancellationMethod?.NextCycle();
        
        _triggered = false;

        return (Speed / segment.Width);
    }
}
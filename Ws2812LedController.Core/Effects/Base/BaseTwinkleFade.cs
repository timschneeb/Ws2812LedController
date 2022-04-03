using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Base;

public abstract class BaseTwinkleFade : IEffect
{
    public override int Speed { get; set; } = 200;
    public SizeOption Size { get; set; } = SizeOption.Small;
    public FadeRate FadeRate { get; set; } = FadeRate.Slow;

    protected Color _colorForeground = Color.White;
    protected Color _colorBackground = Color.Black;

    private int _stepCounter = 0;

    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        FadeOut.DoFrame(segment, _colorBackground, FadeRate, layer);

        if(Random.Shared.Next(3) == 0) 
        {
            var index = Random.Shared.Next(segment.Width - (byte)Size + 1);
            segment.Fill(index, (byte)Size, _colorForeground, layer);
            CancellationMethod?.NextCycle();
        }
        return (Speed / 8);
    }
}
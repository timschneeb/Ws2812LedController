using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Base;

public abstract class BaseAlternate : BaseEffect
{
    public override int Speed { get; set; } = 1000;
    
    public bool Reverse { get; set; } = false;
    public SizeOption Size { get; set; } = SizeOption.Small;

    protected Color _colorA = Color.Blue;
    protected Color _colorB = Color.BlueViolet;

    protected int StepCounter = 0;

    public override void Reset()
    {
        StepCounter = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var size = (byte)(1 << (int)Size);
        var color = (StepCounter & size) != 0 ? _colorA : _colorB;

        if (Reverse)
        {
            segment.CopyPixels(0, 1, segment.Width - 1, layer);
            segment.SetPixel(segment.RelEnd, color, layer);
        }
        else
        {
            segment.CopyPixels(1, 0, segment.Width - 1, layer);
            segment.SetPixel(0, color, layer);
        }

        StepCounter = (StepCounter + 1) % segment.Width;
        if (StepCounter == 0)
        {
            CancellationMethod?.NextCycle();
        }

        return (Speed / segment.Width);
    }
}
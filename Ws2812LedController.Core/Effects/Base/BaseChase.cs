using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Base;

public abstract class BaseChase : BaseEffect
{
    public override int Speed { get; set; } = 500;
    
    public bool Reverse { get; set; } = false;
    public SizeOption Size { get; set; } = SizeOption.Large;

    protected Color _colorA = Color.Red;
    protected Color _colorB = Color.Yellow;
    protected Color _colorBackground = Color.Black;

    protected int StepCounter = 0;

    public override void Reset()
    {
        StepCounter = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var size = (byte)Size;
        for (byte i = 0; i < size; i++)
        {
            var a = (StepCounter + i) % segment.Width;
            var b = (a + size) % segment.Width;
            var c = (b + size) % segment.Width;
            if (Reverse)
            {
                segment.SetPixel(segment.RelEnd - a, _colorBackground, layer);
                segment.SetPixel(segment.RelEnd - b, _colorA, layer);
                segment.SetPixel(segment.RelEnd - c, _colorB, layer);
            }
            else
            {
                segment.SetPixel(a, _colorBackground, layer);
                segment.SetPixel(b, _colorA, layer);
                segment.SetPixel(c, _colorB, layer);
            }
        }

        if (StepCounter + (size * 3) == segment.Width)
        {
            CancellationMethod?.NextCycle();
        }

        StepCounter = (StepCounter + 1) % segment.Width;
        return (Speed / segment.Width);
    }
}
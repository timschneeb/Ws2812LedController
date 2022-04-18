using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class ColorWipe : BaseEffect
{
    public override string Description => "Lights all LEDs one after another.";
    public override int Speed { get; set; } = 2000;
    
    public bool Reverse { get; set; } = false;
    public bool Alternate { get; set; } = false;
    public Color StartColor { get; set; } = Color.White;
    public Color EndColor { get; set; } = Color.Black;

    protected int StepCounter = 0;
    
    public override void Reset()
    {
        StepCounter = 0;
        base.Reset();
    }
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (StepCounter < segment.Width)
        {
            var ledOffset = StepCounter;
            if (Reverse)
            {
                segment.SetPixel(segment.RelEnd - ledOffset, StartColor, layer);
            }
            else
            {
                segment.SetPixel(ledOffset, StartColor, layer);
            }
        }
        else
        {
            var ledOffset = StepCounter - segment.Width;
            if ((Reverse && !Alternate) || (!Reverse && Alternate))
            {
                segment.SetPixel(segment.RelEnd - ledOffset, EndColor, layer);
            }
            else
            {
                segment.SetPixel(ledOffset, EndColor, layer);
            }
        }

        StepCounter = (StepCounter + 1) % (segment.Width * 2);

        if ((StepCounter % segment.Width) == 0)
        {
            CancellationMethod?.NextCycle();
        }

        return (Speed / (segment.Width * 2));
    }
}
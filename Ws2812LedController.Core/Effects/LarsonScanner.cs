using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class LarsonScanner : IEffect
{
    public override string Description => "K.I.T.T.";
    public override int Speed { get; set; } = 1000;
    
    public bool Reverse { get; set; } = false;
    public FadeRate FadeRate { get; set; } = FadeRate.XXSlow;
    public Color ForegroundColor { get; set; } = Color.White;
    public Color BackgroundColor { get; set; } = Color.Black;

    private int _stepCounter = 0;
    
    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        FadeOut.DoFrame(segment, BackgroundColor, FadeRate, layer);

        if (_stepCounter < segment.Width)
        {
            if (Reverse)
            {
                segment.SetPixel(segment.RelEnd - _stepCounter, ForegroundColor, layer);
            }
            else
            {
                segment.SetPixel(_stepCounter, ForegroundColor, layer);
            }
        }
        else
        {
            var index = (segment.Width * 2) - _stepCounter - 2;
            if (Reverse)
            {
                segment.SetPixel(segment.RelEnd - index, ForegroundColor, layer);
            }
            else
            {
                segment.SetPixel(index, ForegroundColor, layer);
            }
        }

        _stepCounter++;
        if (_stepCounter >= (ushort)((segment.Width * 2) - 2))
        {
            _stepCounter = 0;
            CancellationMethod?.NextCycle();
        }

        return (Speed / (segment.Width * 2));
    }
}
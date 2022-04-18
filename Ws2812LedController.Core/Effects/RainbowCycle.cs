using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class RainbowCycle : BaseEffect
{
    public override string Description => "Cycles a rainbow over the entire string of LEDs.";
    public override int Speed { get; set; } = 10000;
    
    public bool Reverse { get; set; } = false;

    private byte _stepCounter = 0;

    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        for (ushort i = 0; i < segment.Width; i++)
        {
            var color = ColorWheel.ColorAtIndex((byte)(((i * 256 / segment.Width) + _stepCounter) & 0xFF));
            if (Reverse)
            {
                segment.SetPixel(segment.RelEnd - i, color, layer);
            }
            else
            {
                segment.SetPixel(i, color, layer);
            }
        }
        
        _stepCounter = (byte)((_stepCounter + 1) & 0xFF);

        if (_stepCounter == 0)
        {
            CancellationMethod?.NextCycle();
        }
        
        return Speed / 256;
    }
}
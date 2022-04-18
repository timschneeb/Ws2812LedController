using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core.Effects;

public class RunningLights : BaseEffect
{
    public override string Description => "Running lights effect with smooth sine transition.";
    public override int Speed { get; set; } = 500;
    
    public bool Reverse { get; set; } = false;
    public SizeOption Size { get; set; } = SizeOption.Small;
    public Color ColorA { get; set; } = Color.Blue;
    public Color ColorB { get; set; } = Color.DarkViolet;

    private byte _stepCounter = 0;

    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var size = (byte)Size;
        var sineIncr = (byte)Math.Max(1, (256 / segment.Width) * size);
        for (ushort i = 0; i < segment.Width; i++)
        {
            var lum = Sine.Sine8((byte)((i + _stepCounter) * sineIncr));
            var color = ColorBlend.Blend(ColorA, ColorB, lum);
            if (Reverse)
            {
                segment.SetPixel(i, color, layer);
            }
            else
            {
                segment.SetPixel(segment.RelEnd - i, color, layer);
            }
        }
        
        _stepCounter = (byte)((_stepCounter + 1) % 256);
        if (_stepCounter == 0)
        {
            CancellationMethod?.NextCycle();
        }
        return (Speed / segment.Width);
    }
}
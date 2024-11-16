using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Base;

public abstract class BaseTricolorChase : BaseEffect
{
    public override int Speed { get; set; } = 500;
    
    public bool Reverse { get; set; } = false;
    public SizeOption Size { get; set; } = SizeOption.Small;

    protected Color _colorA = Color.Red;
    protected Color _colorB = Color.Yellow;
    protected Color _colorC = Color.DarkOrange;

    private int _stepCounter = 0;

    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var sizeCnt = (byte)Size;
        var sizeCnt2 = (byte)(sizeCnt + sizeCnt);
        var sizeCnt3 = (byte)(sizeCnt2 + sizeCnt);
        var index = _stepCounter % sizeCnt3;

        if (index == 0)
        {
            _stepCounter = 0;
            CancellationMethod?.NextCycle();
        }
        
        for (ushort i = 0; i < segment.Width; i++, index++)
        {
            index = (ushort)(index % sizeCnt3);

            var color = _colorC;
            if (index < sizeCnt)
            {
                color = _colorA;
            }
            else if (index < sizeCnt2)
            {
                color = _colorB;
            }

            if (Reverse)
            {
                segment.SetPixel(i, color, layer);
            }
            else
            {
                segment.SetPixel(segment.RelEnd - i, color, layer);
            }
        }

        _stepCounter++;

        return (Speed / segment.Width);
    }
}
using System.Drawing;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class Scan : IEffect
{
    public override string Description => "Runs a block of pixels back and forth.";
    public override int Speed { get; set; } = 500;
    public bool Dual { get; set; } = false;
    public SizeOption Size { get; set; } = SizeOption.Small;
    public Color ScanColor { set; get; } = Color.White;
    public Color BackgroundColor { set; get; } = Color.Black;
    
    private int _stepCounter = 0;
    private int _direction = 1;
    
    public override void Reset()
    {
        _stepCounter = 0;
        _direction = 1;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        segment.Clear(BackgroundColor, layer);

        for (var i = 0; i < (byte)Size; i++)
        {
            if (_direction == -1 || Dual)
            {
                segment.SetPixel(segment.Width - _stepCounter - i - 1, ScanColor, layer);
            }

            if (_direction == 1 || Dual)
            {
                segment.SetPixel(_stepCounter + i, ScanColor, layer);
            }
        }

        _stepCounter += _direction;
        if (_stepCounter == 0)
        {
            _direction = 1;
            CancellationMethod?.NextCycle();
        }
        if (_stepCounter >= (ushort)(segment.Width - Size))
        {
            _direction = -1;
        }
    
        return Speed / (segment.Width * 2);
    }
}
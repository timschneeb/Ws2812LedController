using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Base;

public abstract class BaseSparkle : BaseEffect
{
    public override int Speed { get; set; } = 1500;
    
    public SizeOption Size { get; set; } = SizeOption.Small;

    protected Color _colorForeground = Color.White;
    protected Color _colorBackground = Color.Black;

    private int _stepCounter = 0;
    private int _randomLedIndex = 0;

    public override void Reset()
    {
        _stepCounter = 0;
        _randomLedIndex = 0;
        base.Reset();
    }

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if(_stepCounter == 0) {
            segment.Clear(_colorBackground, layer);
        }
        
        segment.Fill(_randomLedIndex, (byte)Size, _colorBackground, layer);

        _randomLedIndex = Random.Shared.Next(segment.Width - (byte)Size + 1); // aux_param3 stores the random led index
        segment.Fill(_randomLedIndex, (byte)Size, _colorForeground, layer);

        _stepCounter = 1;
        CancellationMethod?.NextCycle();
        return (Speed / 32);
    }
}
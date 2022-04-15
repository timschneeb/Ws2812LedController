using System.Drawing;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.PowerEffects;

public class WipePowerEffect : BasePowerEffect
{
    public override string Description => "Wipe LEDs on/off when toggling power states";
    public override int Speed { get; set; } = 1000;
    public override bool IsSingleShot => true;
    public bool Invert { set; get; }
    public bool Alternate { set; get; } = true;

    private int _stepCounter = 0;
    
    public override void Reset()
    {
        _stepCounter = 0;
        base.Reset();
    }
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var color = TargetState == PowerState.On ? Color.FromArgb(0,0,0,0) : Color.Black;

        if (_stepCounter < segment.Width)
        {
            var ledOffset = _stepCounter;
            if (!Invert && TargetState == PowerState.On || Invert && TargetState == PowerState.Off)
            {
                segment.SetPixel(Alternate ? segment.RelEnd - ledOffset : ledOffset, color, layer);
            }
            else
            {
                segment.SetPixel(ledOffset, color, layer);
            }
        }
 
        _stepCounter = (_stepCounter + 1) % (segment.Width);

        if ((_stepCounter % segment.Width) == 0)
        {
            CancellationMethod?.NextCycle();
            End();
        }
        
        return (Speed / (segment.Width * 2));
    }
}
using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class Comet : IEffect
{
    public override string Description => "Firing comets from one end";
    public override int Speed { get; set; } = 1000;
    
    public bool Reverse { get; set; } = false;
    public FadeRate FadeRate { get; set; } = FadeRate.XSlow;
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

        if(Reverse) 
        {
            segment.SetPixel(segment.AbsEnd - _stepCounter, ForegroundColor, layer);
        } 
        else 
        {
            segment.SetPixel(_stepCounter, ForegroundColor, layer);
        }
        
        _stepCounter = (_stepCounter + 1) % segment.Width;
        if (_stepCounter == 0)
        {
            CancellationMethod?.NextCycle();
        }
        
        return (Speed / segment.Width);
    }
}
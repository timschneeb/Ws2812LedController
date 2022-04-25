using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects;

public class ColorWipeRandom : ColorWipe
{
    public override string FriendlyName => "Color wipe (random)";
    public override string Description => "Turns all LEDs after each other to a random color.";
    public override int Speed { get; set; } = 2000;

    private byte _currentColor = 0;

    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if ((StepCounter % segment.Width) == 0)
        {
            _currentColor = ColorWheel.NextRandomIndex(_currentColor);
        }

        StartColor = ColorWheel.ColorAtIndex(_currentColor);
        EndColor = ColorWheel.ColorAtIndex(_currentColor);
        
        return await base.PerformFrameAsync(segment, layer) * 2;
    }
}
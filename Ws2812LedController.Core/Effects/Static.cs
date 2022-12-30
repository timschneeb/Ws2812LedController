using System.Drawing;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core.Effects;

public class Static : BaseEffect
{
    public override string FriendlyName => "Static";
    public override string Description => "Static color";
    public override int Speed { get; set; } = 6;
    public Color Color { get; set; } = Color.White;
    public float[] StepLevel { get; set; } = new float[3];

    public byte[] CurrentColor;
    // private Color? _previousFrame;

    public Static()
    {
        CurrentColor = Color.ToArray<byte>();
    }
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        if (Color.R != CurrentColor[0] || Color.G != CurrentColor[1] || Color.B != CurrentColor[2])
        { 
            // if not all RGB channels of the light are at desired level
            for (byte k = 0; k < 3; k++)
            {
                float channel = CurrentColor[k];
                // loop with every RGB channel of the light
                if (Color.ByIndex(k) != CurrentColor[k])
                {
                    channel += StepLevel[k]; // move RGB channel on step closer to desired level
                }
                if ((StepLevel[k] > 0.0F && channel > Color.ByIndex(k)) || (StepLevel[k] < 0.0F && channel < Color.ByIndex(k)))
                {
                    channel = Color.ByIndex(k); // if the current level go below desired level apply directly the desired level.
                }

                CurrentColor[k] = (byte)channel.Clamp(0, 255);
            }
        }
 
        segment.Clear(Color.FromArgb(CurrentColor[0], CurrentColor[1], CurrentColor[2]), layer);
        CancellationMethod.NextCycle();

        return Speed;
        
        /* Frequent redraws are unnecessary */
        /*if((Frame % 100) != 0 && _previousFrame == Color)
        {
            return Speed;
        }
        
        segment.Clear(Color, layer);
        CancellationMethod.NextCycle();

        _previousFrame = Color;
        return Speed;*/
    }
}
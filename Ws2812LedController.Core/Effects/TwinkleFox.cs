using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core.Effects;

public class TwinkleFox : BaseEffect
{
    public override string Description => "An adaptation of the FastLED twinkleFOX effect";
    public override int Speed { get; set; } = 1000;
    
    public bool Reverse { get; set; } = false;
    public SizeOption Size { get; set; } = SizeOption.Small;
    public FadeRate FadeRate { get; set; } = FadeRate.None;
    
    public Color ColorA { get; set; } = Color.Red;
    public Color ColorB { get; set; } = Color.Gold;
    public Color ColorC { get; set; } = Color.DodgerBlue;
    
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        var mySeed = 0;
        var size = (byte)Size;
        
        var color0 = ColorA;
        var color1 = ColorB;
        var color2 = ColorC;

        for (ushort i = 0; i <= segment.RelEnd; i += size)
        {
            // Use Mark Kriegsman's clever idea of using pseudo-random numbers to determine
            // each LED's initial and increment blend values
            mySeed = (ushort)((mySeed * 2053) + 13849); // a random, but deterministic, number
            var initValue = (byte)((mySeed + (mySeed >> 8)) & 0xff); // the LED's initial blend index (0-255)
            mySeed = (ushort)((mySeed * 2053) + 13849); // another random, but deterministic, number
            var incrValue = (ushort)((((mySeed + (mySeed >> 8)) & 0x07) + 1) * 2); // blend index increment (2,4,6,8,10,12,14,16)

            // Use the counter_mode_call var as a clock "tick" counter and calc the blend index
            var blendIndex = (byte)((initValue + (Frame * incrValue)) & 0xff); // 0-255
            var blendAmt = Sine.Sine8(blendIndex); // 0-255

            // If colors[0] is BLACK, blend random colors
            uint blendedColor;
            if (color0 == Color.Black)
            {
                blendedColor = ColorBlend.Blend(ColorWheel.ColorAtIndex(initValue), color1, blendAmt).ToUInt32();

            }
            // If colors[2] isn't BLACK, choose to blend colors[0]/colors[1] or colors[1]/colors[2]
            // (which color pair to blend is picked randomly)
            else if ((color2 != Color.Black) && ((initValue < 128) == false))
            {
                blendedColor = ColorBlend.Blend(color2, color1, blendAmt).ToUInt32();
            }
            // Otherwise always blend colors[0]/colors[1]
            else
            {
                blendedColor = ColorBlend.Blend(color0, color1, blendAmt).ToUInt32();
            }

            // Assign the new color to the number of LEDs specified by the SIZE option
            for (byte j = 0; j < size; j++)
            {
                if ((i + j) <= segment.RelEnd)
                {
                    segment.SetPixel(i + j, blendedColor.ToColor(), layer);
                }
            }
        }
        
        CancellationMethod?.NextCycle();
        return Speed / 32;
    }
}
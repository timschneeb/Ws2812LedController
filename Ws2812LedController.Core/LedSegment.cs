using System.Drawing;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core;

public class LedSegment
{
    /* Start index relative to the complete strip */
    public int RelStart { get; }
    /* AbsEnd index in relation to the complete strip */
    public int RelEnd => RelStart + Width;
    
    /* Absolute end index */
    public int AbsEnd => Width - 1;
    public int Width { get; }
    public string Id => $"{RelStart}..{RelEnd}";
    public Color[] State => Strip.Canvas.State[RelStart..RelEnd];

    public LedLayer[] Layers { get; } = new LedLayer[typeof(LayerId).GetEnumNames().Length];

    /* Configurable properties */
    public bool UseGammaCorrection { set; get; }
    public bool InvertX
    {
        set
        {
            foreach (var layer in Layers)
            {
                layer.InvertX = value;
            }
        }
        get => Layers.All(l => l.InvertX);
    }
    public byte MaxBrightness
    {
        set
        {
            _maxBrightness = value;
            Strip.Canvas.RedrawBuffer(RelStart, Width, value);
            Strip.Render();
        }
        get => _maxBrightness;
    }

    public LedStrip Strip { get; }

    private byte _maxBrightness = 255;
    private Color[] _colorMergeBuffer;

    public LedSegment(int relStart, int length, LedStrip strip)
    {
        RelStart = relStart;
        Width = length;
        Strip = strip;
        _colorMergeBuffer = new Color[length];

        for (var i = 0; i < Layers.Length; i++)
        {
            Layers[i] = new LedLayer(length);
        }
    }

    public void ProcessLayers()
    {
        for (var i = 0; i < Width; i++)
        {
            var pixel = Layers[0].LayerState[i];
            for (var j = 1; j < Layers.Length; j++)
            {
                if (!Layers[j].IsActive)
                {
                    continue;
                }
                
                var next = Layers[j].LayerState[i];
                pixel = ColorBlend.Blend(pixel, next, next.A, true);
            }

            Strip.Canvas.SetPixel(RelStart + i, pixel, MaxBrightness, UseGammaCorrection);
        }
    }

    public void Render()
    {
        Strip.Render();
    }
}
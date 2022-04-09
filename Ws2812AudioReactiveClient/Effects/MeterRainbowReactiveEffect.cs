using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Colors;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;

namespace Ws2812AudioReactiveClient.Effects;

public class MeterRainbowReactiveEffect : BaseAudioReactiveEffect
{
    public override string Description => "Expand LEDs based on volume peaks";
    public override int Speed { set; get; } = 1000 / 60;
    /** Only non-fluent rainbow */
    public int ColorWheelSpeed { set; get; } = 3;
    public int DecayFrameTimeout { set; get; } = 0;
    public bool FluentRainbow { set; get; } = false;
    
    private int _width = 0;
    private int _colorWheelPos = 255;
    private int _decayCheck = 0;
    private int _stepCounter = 0;
    private long _preReact = 0; // NEW SPIKE CONVERSION
    private long _react = 0; // NUMBER OF LEDs BEING LIT

    public override void Reset()
    {
        _react = 0;
        _preReact = 0;
        _decayCheck = 0;
        _colorWheelPos = 0;
        _maxSampleEver = 0;
        _stepCounter = 0;
        base.Reset();
    }
    
    private double _maxSampleEver;
    
    private static Color Scroll(int pos) {
        var color = Color.Black;
        color = pos switch
        {
            < 85 => Color.FromArgb((int)(pos / 85.0f * 255.0f), 0, 255 - color.R),
            < 170 => Color.FromArgb(255 - color.G, (int)((pos - 85) / 85.0f * 255.0f), 0),
            < 256 => Color.FromArgb(1, 255 - color.B, (int)((pos - 170) / 85.0f * 255.0f)),
            _ => color
        };
        return color;
    }
    
    private void Rainbow(LedSegmentGroup segment, LayerId layer)
    {
        for(var i = _width - 1; i >= 0; i--) 
        {
            if (i < _react)
            {
                if (FluentRainbow)
                {
                    var color = ColorWheel.ColorAtIndex((byte)(((i * 256 / segment.Width) + _stepCounter) & 0xFF));
                    segment.SetPixel(i, color, layer);
                }
                else
                {
                    segment.SetPixel(i, Scroll((i * 256 / 50 + _colorWheelPos) % 256), layer);
                }
            }
            else
            {
                segment.SetPixel(i, Color.Black, layer);
            }
        }
    }

    private double[] _proc = new double[1024];
    protected override async Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer)
    {
        _width = segment.Width;

        var count = NextSample(ref _proc);
        if (count < 1)
        {
            goto NEXT_FRAME;
        }
        
        var maxSample = FindMaxSample(_proc);
        if (maxSample > _maxSampleEver)
        {
            _maxSampleEver = maxSample;
        }
        
        if (maxSample > 0)
        {
            _preReact = (long)(segment.Width * maxSample);
            if (_preReact > _react) 
                _react = _preReact;
        }

        Rainbow(segment, layer);

        _colorWheelPos = _colorWheelPos - ColorWheelSpeed;
        if (_colorWheelPos < 0)
        {
            _colorWheelPos = 255;
        }
        
        _decayCheck++;
        if (_decayCheck > DecayFrameTimeout)
        {
            _decayCheck = 0;
            if (_react > 0)
                _react--;
        }
        
        _stepCounter = (byte)((_stepCounter + 1) & 0xFF);
        CancellationMethod.NextCycle();
        
        NEXT_FRAME:
        return Speed;
    }
}
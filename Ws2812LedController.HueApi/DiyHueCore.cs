using System.Drawing;
using System.Net.NetworkInformation;
using Ws2812LedController.Core;
using Ws2812LedController.Core.Effects;
using Ws2812LedController.Core.Model;
using Ws2812LedController.Core.Utils;
using Ws2812LedController.HueApi.Serializable;
using static System.String;

namespace Ws2812LedController.HueApi;

public class DiyHueCore
{
    public string DeviceName { init; get; } = "WS2812";
    public string Mac { get; }
    public byte[] RgbMultiplier { set; get; } = { 100, 100, 100 };
    public HueState[] LastStates { get; }
    
    private readonly Ref<LedManager> _mgr;
    
    public DiyHueCore(Ref<LedManager> manager)
    {
        _mgr = manager;
        
        Mac = "00:00:00:00:00:00";
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.NetworkInterfaceType is NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet && nic.OperationalStatus == OperationalStatus.Up)
            {
                Mac = Join(":", Enumerable.Range(0, 6).Select(i => nic.GetPhysicalAddress().ToString().Substring(i * 2, 2)));
                break;
            }
        }

        LastStates = new HueState[_mgr.Value.Segments.Count];
        Array.Fill(LastStates, new HueState());
        for (var i = 0; i < _mgr.Value.Segments.Count; i++)
        {
            LastStates[i] = DetermineState(i);
        }
    }

    public async Task<HueState> ApplyState(int light, HueState state)
    {
        var target = _mgr.Value.Segments[light];

        if (state.Brightness != null)
        {
            target.SourceSegment.MaxBrightness = state.Brightness.Value;
        }
        
        if (state.BrightnessIncrease != null)
        {
            var newBrightness = target.SourceSegment.MaxBrightness + state.BrightnessIncrease.Value;
            target.SourceSegment.MaxBrightness = newBrightness switch
            {
                > 255 => 255,
                < 0 => 0,
                _ => (byte)newBrightness
            };
        }

        await ProcessLightData(light, state);
        
        if (state.IsPowered != null)
        {
            await target.PowerAsync(state.IsPowered.Value);
        }
        
        return state;
    } 
    
    public HueState DetermineState(int light)
    {
        var target = _mgr.Value.Segments[light];
        var currentEffect = target.CurrentEffects[(int)LayerId.BaseLayer];
        var state = new HueState
        {
            IsPowered = target.CurrentState is PowerState.On or PowerState.PoweringOn,
            Brightness = target.SourceSegment.MaxBrightness
        };
        
        if (currentEffect is Static staticEffect)
        {
            ColorUtils.ToHsb(staticEffect.Color, out var hue, out var sat, out var bri);
            state.ColorMode = ColorMode.hs;
            state.Hue = hue;
            state.Saturation = sat;
            // state.Brightness = (byte)bri; 
        }
        else
        {
            state.ColorMode = LastStates[light].ColorMode;
            state.Xy = LastStates[light].Xy;
            state.Hue = LastStates[light].Hue;
            state.Saturation = LastStates[light].Saturation;
            state.ColorTemperature = LastStates[light].ColorTemperature;
        }

        return state;
    }

    private int PixelCount(int light)
    {
        return _mgr.Value.Segments[light].SourceSegment.Width;
    }
    
    public async Task ProcessLightData(int light, HueState state)
    { 
        var target = _mgr.Value.Segments[light];
        var bright = target.SourceSegment.MaxBrightness;
        var color = Color.Black;

        var transitionTime = 4;
        if (state.TransitionTime != null)
        {
            transitionTime = state.TransitionTime.Value;
        }
        
        if (state.Xy is { Length: >= 2 })
        {
            // colorMode = 1
            color = ColorUtils.FromXy(state.Xy[0], state.Xy[1], bright, RgbMultiplier);
        }
        else if (state.ColorTemperature != null)
        {
            // colorMode = 2
            color = ColorUtils.FromColorTemperature(state.ColorTemperature.Value, bright, RgbMultiplier);
        }
        else if (state.Hue != null && state.Saturation != null)
        {
            // colorMode = 3
            color = ColorUtils.FromHsb(state.Hue ?? 0, state.Saturation ?? 0, bright);
        }
        else
        {
            return;
        }

        Static targetEffect;
        if (target.CurrentEffects[(int)LayerId.BaseLayer] is Static staticEffect)
        {
            targetEffect = staticEffect;
        }
        else
        {
            targetEffect = new Static()
            {
                Color = color
            };

            await target.SetEffectAsync(targetEffect, blockUntilConsumed: true);
        }

        if (state.Alert == "select")
        {
            if (target.IsPowered)
            { targetEffect.CurrentColor[0] = 0;
                targetEffect.CurrentColor[1] = 0;
                targetEffect.CurrentColor[2] = 0;
            }
            else
            {
                targetEffect.CurrentColor[1] = 126;
                targetEffect.CurrentColor[2] = 126;
            }
        }
        
        // calculate the step level of every RGB channel for a smooth transition in requested transition time
        transitionTime *= (17 - (PixelCount(light) / 40)); // every extra led add a small delay that need to be counted for transition time match

        targetEffect.Color = color;
        
        for (byte i = 0; i < 3; i++)
        {
            if (target.IsPowered)
            {
                targetEffect.StepLevel[i] = ((float)targetEffect.Color.ByIndex(i) - targetEffect.CurrentColor[i]) / transitionTime;
            }
            else
            {
                targetEffect.StepLevel[i] = (float)targetEffect.CurrentColor[i] / transitionTime;
            }
        }
    }
}
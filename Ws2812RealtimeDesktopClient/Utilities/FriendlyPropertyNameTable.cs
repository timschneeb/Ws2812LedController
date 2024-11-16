namespace Ws2812LedController.AudioReactive.Utils;

public static class FriendlyPropertyNameTable
{
    static FriendlyPropertyNameTable()
    {
        Dictionary = new Dictionary<string, string>
        {
            { "Speed", "Speed" },
            { "MinVolume", "Minimum volume limit (dB)" },
            { "Multiplier", "Multiplier" },
            { "AvgSmoothingStrength", "Smoothing strength" },
            
            { "FadeSpeed", "Fade speed" },
            { "Intensity", "Intensity" },
            { "ColorWheelSpeed", "Color wheel speed" },
            { "DecayFrameTimeout", "Decay timeout (frames)" },
            { "FluentRainbow", "Smooth rainbow" },
            { "FftCBinSelector", "FFT bin selection" },
            { "Color", "Color" },
            { "Threshold", "Peak threshold" },
            { "Centered", "Centered" },
            { "ColorNoise", "Color noise" },
            { "AnimationSpeed", "Animation speed" },
            { "VolumeAnalysisOptions", "Volume analysis options" },
            { "Palette", "Color palette" },
            { "Count", "Count" },
            { "FramesPerStep", "FramesPerStep" },
            { "FirstBin", "First FFT bin" },
            { "LastBin", "Last FFT bin" },
            { "SoundSquelch", "Sound squelch" },
            { "BlurIntensity", "Blur intensity" },
            { "StartFrequency", "Start frequency (Hz)" },
            { "EndFrequency", "End frequency (Hz)" },
            { "StartFromEdge", "Edge position" },
            { "Sensitivity", "Sensitivity" },
            { "MinFftPeakMagnitude", "Minimum peak magnitude" },
            { "MaxFftPeakMagnitude", "Maximum peak magnitude" },
            { "MaxRipples", "Maximum ripples" },
            { "MaxSteps", "Maximum steps" },
            { "RainbowColors", "Rainbow colors" },
            { "ColorBasedOnHz", "Derive colors from frequency" },
            { "MinimumMagnitude", "Minimum magnitude" },
            { "MaximumMagnitude", "Maximum magnitude" },
            { "WrapStrip", "Wrap strip edges" },
        };
    }
    
    public static readonly Dictionary<string, string> Dictionary;
    
    public static string? Lookup(string key)
    {
        return Dictionary.ContainsKey(key) ? Dictionary[key] : null;
    }
}
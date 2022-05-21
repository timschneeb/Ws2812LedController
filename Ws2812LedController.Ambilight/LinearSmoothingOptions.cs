using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ws2812LedController.Ambilight.Annotations;

namespace Ws2812LedController.Ambilight;

public class LinearSmoothingOptions : INotifyPropertyChanged
{
    private int _updateIntervalHz = 25;
    private int _settlingTime = 200;
    private LinearSmoothing.SmoothingType _smoothingMode;
    private int _antiFlickeringThreshold;
    private int _antiFlickeringStep;
    private int _antiFlickeringTimeout;
    private bool _continuousOutput = true;
    private bool _isEnabled = true;

    public int UpdateIntervalHz
    {
        set => SetField(ref _updateIntervalHz, value);
        get => _updateIntervalHz;
    }

    public int SettlingTime
    {
        set => SetField(ref _settlingTime, value);
        get => _settlingTime;
    }

    public LinearSmoothing.SmoothingType SmoothingMode
    {
        set => SetField(ref _smoothingMode, value);
        get => _smoothingMode;
    }

    public int AntiFlickeringThreshold
    {
        set => SetField(ref _antiFlickeringThreshold, value);
        get => _antiFlickeringThreshold;
    }

    public int AntiFlickeringStep
    {
        set => SetField(ref _antiFlickeringStep, value);
        get => _antiFlickeringStep;
    }

    public int AntiFlickeringTimeout
    {
        set => SetField(ref _antiFlickeringTimeout, value);
        get => _antiFlickeringTimeout;
    }

    public bool ContinuousOutput
    {
        set => SetField(ref _continuousOutput, value);
        get => _continuousOutput;
    }

    public bool IsEnabled
    {
        set => SetField(ref _isEnabled, value);
        get => _isEnabled;
    }


    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected void OnPropertyChanged(string? propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
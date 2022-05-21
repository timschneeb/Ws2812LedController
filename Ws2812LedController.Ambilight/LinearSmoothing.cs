using System.Diagnostics;
using System.Drawing;
using Ws2812LedController.Core.Utils;
using Timer = System.Timers.Timer;

namespace Ws2812LedController.Ambilight;

/// Linear Smoothing class
///
/// This class processes the requested led values and forwards them to the device after applying
/// a linear smoothing effect. This class can be handled as a generic LedDevice.
public class LinearSmoothing
{
    public LinearSmoothing()
    {
        _updateInterval = 1000 / 25;
        _settlingTime = 200;
        _continuousOutput = false;
        _antiFlickeringThreshold = 0;
        _antiFlickeringStep = 0;
        _antiFlickeringTimeout = 0;
        _flushFrame = false;
        _targetTime = 0;
        _previousTime = 0;
        _directMode = false;
        _smoothingType = SmoothingType.Linear;
        _continuousOutput = true;
        
        _timer.AutoReset = true;
        _timer.Elapsed += (_, _) => LinearSmoothingProcessing(_smoothingType == SmoothingType.Alternative);
        
        Enabled = true;
    }

    public event Action<IReadOnlyList<Color>>? DataReady;
    
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            ClearQueuedColors(_enabled);
        }
    }

    public void ApplySettings(LinearSmoothingOptions opt)
    {
        _settlingTime = opt.SettlingTime;
        _directMode = false;
        _antiFlickeringThreshold = opt.AntiFlickeringThreshold;
        _antiFlickeringStep = opt.AntiFlickeringStep;
        _antiFlickeringTimeout = opt.AntiFlickeringTimeout;
        _continuousOutput = opt.ContinuousOutput;
        
        Enabled = opt.IsEnabled;

        var newUpdateInterval = Math.Max(1000 / opt.UpdateIntervalHz, (long)5);
        if (newUpdateInterval != _updateInterval || opt.SmoothingMode != _smoothingType)
        {
            _updateInterval = newUpdateInterval;
            _smoothingType = opt.SmoothingMode;
            ClearQueuedColors(Enabled, true);
        }
    }
    
    public void PushColors(List<Color> ledValues)
    {
        if (!Enabled)
        {
            return;
        }

        if (_directMode)
        {
            if (_watch.ElapsedMilliseconds - _updateInterval >= 0)
            {
                ClearQueuedColors();
            }

            if (ledValues.Count == 0)
            {
                return;
            }

            DataReady?.Invoke(ledValues.AsReadOnly());
            return;
        }

        LinearSetup(ledValues);
    }

    private void NotifyConsumer(List<Color> ledColors)
    {
        DataReady?.Invoke(ledColors.AsReadOnly());
    }

    private void ClearQueuedColors(bool deviceEnabled = false, bool restarting = false)
    {
        if (!deviceEnabled || restarting)
        {
            _watch.Reset();
            _timer.Stop();
        }
            
        _timer.Interval = _updateInterval;
            
        _previousValues.Clear();
        lock (_previousTimeouts)
        {
            _previousTimeouts.Clear();
        }
        _previousTime = 0;
        _targetValues.Clear();
        _targetTime = 0;
        _flushFrame = false;

        if (deviceEnabled)
        {
            _watch.Start();
            _timer.Start();
        }
    }

    private static byte ComputeColor(long k, int color)
    {
        var delta = Math.Abs(color);
        var step = Math.Min(Math.Max((int)((k * delta) >> 8), 1), delta);
        return (byte)step;
    }

    private void SetupAdvColor(long deltaTime, ref double kOrg, ref double kMin, ref double kMid, ref double kAbove, ref double kMax)
    {
        kOrg = Math.Max(1.0f - 1.0f * deltaTime / (_targetTime - _previousTime), 0.0001f);

        kMin = Math.Min(Math.Pow(kOrg, 1.0f), 1.0f);
        kMid = Math.Min(Math.Pow(kOrg, 0.9f), 1.0f);
        kAbove = Math.Min(Math.Pow(kOrg, 0.75f), 1.0f);
        kMax = Math.Min(Math.Pow(kOrg, 0.6f), 1.0f);
    }

    private static byte ComputeAdvColor(int limitMin, int limitAverage, int limitMax, double kMin, double kMid, double kAbove, double kMax, int color)
    {
        var val = Math.Abs(color);
        if (val < limitMin)
        {
            return (byte)Math.Ceiling(kMax * val);
        }
        if (val < limitAverage)
        {
            return (byte)Math.Ceiling(kAbove * val);
        }
        if (val < limitMax)
        {
            return (byte)Math.Ceiling(kMid * val);
        }
        return (byte)Math.Ceiling(kMin * val);
    }
    
    private static byte Clamp(int x)
    {
        return (byte)(x < 0 ? 0 : x > 255 ? 255 : x);
    }

    private void DoAntiFlickering()
    {
        lock (_previousTimeouts)
        {
            if (_antiFlickeringThreshold > 0 && _antiFlickeringStep > 0 &&
                _previousValues.Count == _targetValues.Count && _previousValues.Count == _previousTimeouts.Count)
            {
                long now = Time.Millis();

                for (var i = 0; i < _previousValues.Count; ++i)
                {
                    var newColor = _targetValues[i];
                    var oldColor = _previousValues[i];

                    var avVal = (Math.Min(newColor.R, Math.Min(newColor.G, newColor.B)) +
                                 Math.Max(newColor.R, Math.Max(newColor.G, newColor.B))) / 2;
                    if (avVal < _antiFlickeringThreshold)
                    {
                        var minR = Math.Abs(newColor.R - oldColor.R);
                        var minG = Math.Abs(newColor.G - oldColor.G);
                        var minB = Math.Abs(newColor.B - oldColor.B);
                        var select = Math.Max(Math.Max(minR, minG), minB);

                        if (select < _antiFlickeringStep && (newColor.R != 0 || newColor.G != 0 || newColor.B != 0) &&
                            (oldColor.R != 0 || oldColor.G != 0 || oldColor.B != 0))
                        {
                            if (_antiFlickeringTimeout <= 0 || now - _previousTimeouts[i] < _antiFlickeringTimeout)
                            {
                                _targetValues[i] = _previousValues[i];
                            }
                            else
                            {
                                _previousTimeouts[i] = now;
                            }
                        }
                        else
                        {
                            _previousTimeouts[i] = now;
                        }
                    }
                    else
                    {
                        _previousTimeouts[i] = now;
                    }
                }
            }
        }
    }

    private void LinearSetup(List<Color> ledValues)
    {
        _targetTime = Time.Millis() + _settlingTime;
        _targetValues = ledValues;

        if (_previousValues.Count > 0 && (_previousValues.Count != _targetValues.Count))
        {
            _previousValues.Clear();
            lock (_previousTimeouts)
            {
                _previousTimeouts.Clear();
            }
        }

        if (_previousValues.Count == 0)
        {
            _previousTime = Time.Millis();
            _previousValues = new List<Color>(ledValues);
            lock (_previousTimeouts)
            {
                _previousTimeouts.Clear();
                for (var i = 0; i < _previousValues.Count; i++)
                {
                    _previousTimeouts.Add(_previousTime);
                }
            }
        }

        DoAntiFlickering();
    }

    private void LinearSmoothingProcessing(bool correction)
    {
        var kOrg = 0.0;
        var kMin = 0.0;
        var kMid = 0.0;
        var kAbove = 0.0;
        var kMax = 0.0;
        var now = Time.Millis();
        var deltaTime = _targetTime - now;

        const int aspectLow = 16;
        const int aspectMid = 32;
        const int aspectHigh = 60;

        if (deltaTime <= 0 || _targetTime <= _previousTime)
        {
            _previousValues = new List<Color>(_targetValues);
            lock (_previousTimeouts)
            {
                _previousTimeouts.Clear();
                for (var i = 0; i < _previousValues.Count; i++)
                {
                    _previousTimeouts.Add(now);
                }
            }
            
            _previousTime = now;

            if (_flushFrame)
            {
                NotifyConsumer(_previousValues);
            }

            _flushFrame = _continuousOutput;
        }
        else
        {
            _flushFrame = true;

            long k = 0;
            if (correction)
            {
                SetupAdvColor(deltaTime, ref kOrg, ref kMin, ref kMid, ref kAbove, ref kMax);
            }
            else
            {
                k = Math.Max((1 << 8) - (deltaTime << 8) / (_targetTime - _previousTime), 1);
            }

            if (_previousValues.Count != _targetValues.Count)
            {
                Console.WriteLine("LinearSmoothing: _previousValues.Count != _targetValues.Count");
            }
            else
            {
                for (var i = 0; i < _previousValues.Count; i++)
                {
                    var prev = _previousValues[i];
                    var target = _targetValues[i];

                    var redDiff = target.R - prev.R;
                    var greenDiff = target.G - prev.G;
                    var blueDiff = target.B - prev.B;

                    var r = prev.R;
                    var g = prev.G;
                    var b = prev.B;
                    if (redDiff != 0)
                    {
                        r += Clamp((redDiff < 0 ? -1 : 1) * ((correction) ? ComputeAdvColor(aspectLow, aspectMid, aspectHigh, kMin, kMid, kAbove, kMax, redDiff) : ComputeColor(k, redDiff)));
                    }
                    if (greenDiff != 0)
                    {
                        g += Clamp((greenDiff < 0 ? -1 : 1) * ((correction) ? ComputeAdvColor(aspectLow, aspectMid, aspectHigh, kMin, kMid, kAbove, kMax, greenDiff) : ComputeColor(k, greenDiff)));
                    }
                    if (blueDiff != 0)
                    {
                        b += Clamp((blueDiff < 0 ? -1 : 1) * ((correction) ? ComputeAdvColor(aspectLow, aspectMid, aspectHigh, kMin, kMid, kAbove, kMax, blueDiff) : ComputeColor(k, blueDiff)));
                    }
                    _previousValues[i] = Color.FromArgb(r,g,b);
                }
            }
            _previousTime = now;

            NotifyConsumer(_previousValues);
        }
    }

    /// The interval at which to update the leds (msec)
    private long _updateInterval;

    /// The time after which the updated led values have been fully applied (msec)
    private long _settlingTime;

    /// The Qt timer object
    private readonly Stopwatch _watch = new();
    private readonly Timer _timer = new();

    /// The target led data
    private List<Color> _targetValues = new();

    /// The previously written led data
    private List<Color> _previousValues = new();
    private readonly List<long> _previousTimeouts = new();

    /// Flag for dis/enable continuous output to led device regardless there is new data or not
    private bool _continuousOutput;
    private int _antiFlickeringThreshold;
    private int _antiFlickeringStep;
    private long _antiFlickeringTimeout;
    private bool _flushFrame;
    private long _targetTime;
    private long _previousTime;

    public enum SmoothingType
    {
        Linear = 0,
        Alternative = 1
    }
    
    /// smooth config list
    private bool _enabled;
    private bool _directMode;
    private SmoothingType _smoothingType;
}
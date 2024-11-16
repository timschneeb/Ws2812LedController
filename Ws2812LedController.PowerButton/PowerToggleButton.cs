using System.Device.Gpio;
using System.Diagnostics;

namespace Ws2812LedController.PowerButton;

public class PowerToggleButton : IDisposable
{  
    private Task? _loop;
    private CancellationTokenSource _cancelSource = new();
    private readonly int _pinNumber;
    private readonly GpioController _controller;
    private bool _lastState { set; get; }
    private int _duplicateEvents = 0;

    public bool IsPowered { set; get; }

    public event EventHandler<bool>? PowerStateChanged;
    
    public PowerToggleButton(int pinNumber = 27)
    {
        _pinNumber = pinNumber;
        _controller = new GpioController();
        Open();
    }

    public void Open()
    {
        _controller.OpenPin(_pinNumber, PinMode.InputPullUp);
        IsPowered = _lastState = _controller.Read(_pinNumber) == PinValue.High;
        
        _cancelSource.Cancel();
        _cancelSource = new CancellationTokenSource();
        _loop = Task.Run(ReceiverLoop);
    }

    public void Close()
    {
        _controller.ClosePin(_pinNumber);
        _cancelSource.Cancel();
    }

    public void Dispose()
    {
        _controller.Dispose();
    }

    public async void ReceiverLoop()
    {
        while (true)
        {
            if (_cancelSource.Token.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(20);
            
                    
            var isHigh = _controller.Read(_pinNumber) == PinValue.High;
 
            if (isHigh == IsPowered && _duplicateEvents <= 30)
            {
                _duplicateEvents++;
                continue;
            }
            
            if(_duplicateEvents <= 30)
            {
                _duplicateEvents = 0;
                continue;
            }

            IsPowered = isHigh;

            if (/* new */ IsPowered != /* old */ _lastState)
            {
                _duplicateEvents = 0;
                
                PowerStateChanged?.Invoke(this, IsPowered);
                
                _lastState = IsPowered;
            }
        }
    }
}
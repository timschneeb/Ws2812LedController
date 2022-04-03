using System.Timers;
using System.Diagnostics;
using Timer = System.Timers.Timer;

namespace Ws2812LedController.Core.CancellationMethod;

public class CancellationTimeoutMethod : ICancellationMethod
{
    private readonly Timer _timer = new();
    
    public CancellationTimeoutMethod(int timeout)
    {
        Debug.Assert(timeout >= 500);

        Timeout = timeout;
        _timer.Interval = timeout;
        _timer.AutoReset = false;
        _timer.Elapsed += OnElapsed;
    }

    private void OnElapsed(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("CancellationTimeoutMethod.OnElapsed: Cancelled");
        _tokenSource.Cancel();
    }

    internal override void Begin()
    {
        _timer.Start();
        base.Begin();
    }
    
    /* Timeout in milliseconds */
    public int Timeout { get; }
    
}
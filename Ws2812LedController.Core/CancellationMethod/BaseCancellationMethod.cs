using Ws2812LedController.Core.Utils;

namespace Ws2812LedController.Core.CancellationMethod;

public abstract class BaseCancellationMethod : IDisposable
{
    protected CancellationTokenSource _tokenSource = new();
    private bool _cancelNextCycle = false;
    public CancellationToken Token { private set; get; }
    
    protected BaseCancellationMethod()
    {
        Token = _tokenSource.Token;
    }

    public void Reset()
    {
        _tokenSource.Dispose();
        _tokenSource = new CancellationTokenSource();
        Token = _tokenSource.Token;
    }

    public void Cancel()
    {
        Console.WriteLine("BaseCancellationMethod.Cancel: Cancelled");
        if (_tokenSource.IsCancellationRequested)
        {
            return;
        }
        _tokenSource.Cancel();
    }
    
    public async Task<bool> CancelAsync(int timeout)
    {
        if (_tokenSource.IsCancellationRequested)
        {
            return true;
        }
        Cancel();
        return await _tokenSource.Token.WaitHandle.WaitOneAsync(timeout, CancellationToken.None);
    }

    public async Task<bool> CancelNextCycleAsync(int timeout)
    {
        if (_tokenSource.IsCancellationRequested)
        {
            return true;
        }
        _cancelNextCycle = true;
        return await _tokenSource.Token.WaitHandle.WaitOneAsync(timeout, CancellationToken.None);
    }
    
    public void CancelNextCycleNonBlocking()
    {
        _cancelNextCycle = true;
    }
    
    public void Dispose()
    {
        _tokenSource.Dispose();
    }
    
    internal virtual void Begin()
    {
    }

    public virtual void NextFrame()
    {
    }
    
    public virtual void NextCycle()
    {
        if (_cancelNextCycle)
        {
            Console.WriteLine("BaseCancellationMethod.NextCycle: Cancelled");
            _tokenSource.Cancel();
            _cancelNextCycle = false;
        }
    }
}
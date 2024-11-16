namespace Ws2812LedController.Core.CancellationMethod;

public class CancellationFrameMethod : BaseCancellationMethod
{
    public CancellationFrameMethod(long frameLimit)
    {
        FrameLimit = frameLimit;
    }

    public override void NextFrame()
    {
        CurrentIteration++;
        if (CurrentIteration >= FrameLimit)
        {
            Console.WriteLine("CancellationFrameMethod.NextFrame: Cancelled");
            _tokenSource.Cancel();
        }
        base.NextFrame();
    }
    
    public long CurrentIteration { private set; get; }
    public long FrameLimit { get; }
}
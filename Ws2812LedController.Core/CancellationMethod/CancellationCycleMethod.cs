namespace Ws2812LedController.Core.CancellationMethod;

public class CancellationCycleMethod : BaseCancellationMethod
{
    public CancellationCycleMethod(long cycleLimit)
    {
        CycleLimit = cycleLimit;
    }
    
    public override void NextCycle()
    {
        CurrentCycle++;
        if (CurrentCycle >= CycleLimit)
        {
            Console.WriteLine("CancellationCycleMethod.NextCycle: Cancelled");
            _tokenSource.Cancel();
        }
        base.NextCycle();
    }
    
    public long CurrentCycle { private set; get; }
    public long CycleLimit { get; }
}
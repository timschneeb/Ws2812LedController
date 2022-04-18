using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Base;

public abstract class BaseEffect
{
    [NonEffectParameter]
    public abstract string Description { get; }

    [NonEffectParameter]
    public BaseCancellationMethod CancellationMethod { init; get; } = new CancellationTokenMethod();
    [NonEffectParameter]
    public virtual bool IsSingleShot => false;
    public abstract int Speed { set; get; }
    public bool IsFrozen { set; get; }
    
    public event EventHandler<bool>? Finished;
    protected uint Frame { private set; get; }

    public virtual void Reset()
    {
        _animationDone = false;
        CancellationMethod.Reset();
        Frame = 0;
    }

    protected virtual void Begin()
    {
        CancellationMethod.Begin();
    }
    
    protected virtual void End()
    {
        _animationDone = true;
    }
    
    protected abstract Task<int> PerformFrameAsync(LedSegmentGroup segment, LayerId layer);

    public Task PerformAsync(LedSegment segment, LayerId layer)
    {
        return PerformAsync(new LedSegmentGroup(segment), layer);
    }

    private bool _animationDone = false;
    public async Task PerformAsync(LedSegmentGroup segment, LayerId layer)
    {
        Begin();
        
        Frame = 0;

        try
        {
            while (!_animationDone)
            {
                while (IsFrozen)
                {
                    await Task.Delay(100, CancellationMethod.Token);
                }

                var timeout = await PerformFrameAsync(segment, layer);
                
                CancellationMethod.NextFrame();
                await Task.Delay(timeout, CancellationMethod.Token);
                
                Frame = (Frame >= uint.MaxValue) ? 0 : Frame + 1;
            }
            
            Finished?.Invoke(this, false);
        }
        catch (TaskCanceledException)
        {
            Finished?.Invoke(this, true);
        }
    }
}
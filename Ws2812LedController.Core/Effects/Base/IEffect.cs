using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.Base;

public abstract class IEffect
{
    [NonEffectParameter]
    public abstract string Description { get; }

    [NonEffectParameter]
    public ICancellationMethod CancellationMethod { init; get; } = new CancellationTokenMethod();
    [NonEffectParameter]
    public bool AutomaticRender { set; get; } = true;
    [NonEffectParameter]
    public virtual bool IsSingleShot => false;
    public abstract int Speed { set; get; }
    public bool IsFrozen { set; get; }
    
    public event EventHandler<bool>? Finished;
    public event EventHandler? RenderRequest;
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
               
                if (AutomaticRender)
                {
                    segment.Render();
                }
                else
                {
                    segment.Render(dry: true);
                    RenderRequest?.Invoke(this, EventArgs.Empty);
                }
                
                CancellationMethod.NextFrame();
                await Task.Delay(timeout, CancellationMethod.Token);
                
                Frame = (Frame >= uint.MaxValue) ? 0 : Frame + 1;
            }
            
            Finished?.Invoke(this, false);
        }
        catch (TaskCanceledException ex)
        {
            Finished?.Invoke(this, true);
            Console.WriteLine("IEffect.PerformAsync: TaskCanceledException received");
        }
    }
}
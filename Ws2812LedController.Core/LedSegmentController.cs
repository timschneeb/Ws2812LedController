using System.Collections.Concurrent;
using System.Drawing;
using Ws2812LedController.Core.CancellationMethod;
using Ws2812LedController.Core.Effects;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Effects.PowerEffects;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core;

public class LedSegmentController : IDisposable
{
    public string Name { internal set; get; }
    public LedSegmentGroup SegmentGroup { get; }
    public LedSegment SourceSegment => SegmentGroup.MasterSegment;
    public BaseEffect?[] CurrentEffects { get; }
    public BasePowerEffect PowerEffect { set; get; } = new NullPowerEffect();

    public PowerState CurrentState { private set; get; }

    private readonly Task[] _loop;
    private readonly ConcurrentQueue<BaseEffect>[] _queue;
    private readonly CancellationTokenSource _cancelSource = new();

    public LedSegmentController(string name, LedSegment segment) : this(name, new LedSegmentGroup(segment)) {}
    public LedSegmentController(string name, LedSegmentGroup segmentGroup)
    {
        Name = name;
        SegmentGroup = segmentGroup;
        
        var layerCount = segmentGroup.MasterSegment.Layers.Length;
        _loop = new Task[layerCount];
        _queue = new ConcurrentQueue<BaseEffect>[layerCount];
        CurrentEffects = new BaseEffect[layerCount];
        
        for (var i = 0; i < layerCount; i++)
        {
            var layer = (LayerId)i;
            _queue[i] = new ConcurrentQueue<BaseEffect>();
            _loop[i] = Task.Run(() => ProcessEvents(layer), _cancelSource.Token);
        }
    }

    public void MirrorTo(LedSegmentController controller)
    {
        SegmentGroup.AddMirror(controller.SegmentGroup.MasterSegment);
    }
    
    public void RemoveMirror(LedSegmentController controller)
    {
        SegmentGroup.RemoveMirror(controller.SegmentGroup.MasterSegment);
    }
    
    public void RemoveAllMirrors()
    {
        SegmentGroup.RemoveAllMirrors();
    }

    public async Task<bool> CancelLayerAsync(LayerId layer)
    {
        return await (CurrentEffects[(int)layer]?.CancellationMethod.CancelAsync(2000) ??
                      Task.FromResult(false));
    }
    
    public async Task PowerAsync(bool power)
    {
        switch (CurrentState)
        {
            case PowerState.PoweringOff when !power:
            case PowerState.PoweringOn when power:
                Console.WriteLine("LedSegmentController.PowerAsync: Already powering down/up. Skipping request");
                return;
            case PowerState.Off when !power:
            case PowerState.On when power:
                Console.WriteLine("LedSegmentController.PowerAsync: Already powered down/up. Re-render current state without animation");
                await SetEffectAsync(
                    new NullPowerEffect(){ TargetState = CurrentState }, CancelMode.Now, true, LayerId.PowerSwitchLayer);
                return;
        }

        CurrentState = power ? PowerState.PoweringOn : PowerState.PoweringOff;

        var target = power ? PowerState.On : PowerState.Off;

        await PowerEffect.CancellationMethod.CancelAsync(1000);
        PowerEffect.Reset();
        PowerEffect.TargetState = target;

        void OnFinished(object? sender, bool cancelled)
        {
            if (!cancelled)
            {
                CurrentState = target;
            }
            else
            {
                /* Revert state due to cancellation */
                CurrentState = !power ? PowerState.On : PowerState.Off;
            }
            
            PowerEffect.Finished -= OnFinished;
        }
        
        PowerEffect.Finished += OnFinished;
        await SetEffectAsync(PowerEffect, CancelMode.Now, true, LayerId.PowerSwitchLayer);
    }

    public async Task SetEffectAsync(BaseEffect effect, CancelMode cancelMode = CancelMode.Now, bool noPowerOn = false, LayerId layer = LayerId.BaseLayer)
    {
        SegmentGroup.MasterSegment.Strip.Canvas.ExclusiveMode = false;
        
        if (cancelMode != CancelMode.Enqueue)
        {
            lock (_queue)
            {
                _queue[(int)layer].Clear();
            }
        }

        if (CurrentState == PowerState.Off && !noPowerOn)
        {
            await PowerAsync(true);
        }
        
        if (CurrentEffects[(int)layer] != null)
        {
            var success = false;
            switch (cancelMode)
            {
                case CancelMode.Now:
                    success = await (CurrentEffects[(int)layer]?.CancellationMethod.CancelAsync(2000) ?? Task.FromResult(false));
                    break;
                case CancelMode.NextCycle:
                    success = await (CurrentEffects[(int)layer]?.CancellationMethod.CancelNextCycleAsync(10000) ?? Task.FromResult(false));
                    if (!success)
                    {
                        await (CurrentEffects[(int)layer]?.CancellationMethod.CancelAsync(2000) ?? Task.FromResult(false));
                    }
                    break;
            }

            if (!success)
            {
                Console.WriteLine("LedSegmentController: Failed to cancel previous effect");
            }
        }

        lock (_queue)
        {
            _queue[(int)layer].Enqueue(effect);
        }
    }
    
    public async void ProcessEvents(LayerId layer)
    {
        try
        {
            while (!_cancelSource.IsCancellationRequested)
            {
                var dataAvailable = false;
                lock (_queue)
                {
                    dataAvailable = _queue[(int)layer].TryDequeue(out var effect);
                    CurrentEffects[(int)layer] = effect;
                }

                if (!dataAvailable || CurrentEffects[(int)layer] == null)
                {
                    await Task.Delay(100).WaitAsync(_cancelSource.Token);
                    continue;
                }

                /* Leave rendering to PwrEffect while powering on/off */
                await CurrentEffects[(int)layer]!.PerformAsync(SegmentGroup, layer);
            }
        }
        catch(TaskCanceledException){}
    }

    internal async Task TerminateLoopAsync(int msTimeout)
    {
        _cancelSource.Cancel();

        var tasks = new Task[_loop.Length];
        for (var i = 0; i < _loop.Length; i++)
        {
            CurrentEffects[i]?.CancellationMethod.Cancel();
            tasks[i] = _loop[i].WaitAsync(TimeSpan.FromMilliseconds(msTimeout));
        }

        await Task.WhenAll(tasks);
    }
        
    public void Dispose()
    {    
        _cancelSource.Cancel();

        _cancelSource.Dispose();
        foreach (var t in _loop)
        {
            t.Dispose();
        }
    }
}
using System.Diagnostics;
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Effects.PowerEffects;

public abstract class BasePowerEffect : BaseEffect
{
    public PowerState TargetState
    {
        set
        {
            Debug.Assert(value is PowerState.Off or PowerState.On, "BasePowerEffect: Invalid target state");
            _targetState = value;
        }
        get => _targetState;
    }
    
    private PowerState _targetState = PowerState.Off;
}
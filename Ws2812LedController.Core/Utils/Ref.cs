using System.Diagnostics;

namespace Ws2812LedController.Core.Utils;

public sealed class Ref<T> 
{
    private readonly Func<T> _getter;
    private readonly Action<T>? _setter;
    
    public Ref(Func<T> getter, Action<T>? setter = null)
    {
        _getter = getter;
        _setter = setter;
    }
    
    public T Value
    {
        get => _getter();
        set
        {
            Debug.Assert(_setter == null, "Ref<T>.Value.set: Reference is read-only; setter is null");
            _setter?.Invoke(value);
        }
    }
}
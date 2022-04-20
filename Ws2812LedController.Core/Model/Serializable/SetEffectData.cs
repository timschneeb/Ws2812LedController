namespace Ws2812LedController.Core.Model.Serializable;

public class SetEffectData
{
    public string? Name { set; get; }
    public CancelMode? PrevCancelMode { set; get; }
    public CancellationMethodData? CancellationMethod { set; get; }
    public Property[]? Properties { set; get; }

    public class Property
    {
        public string? Name { set; get; }        
        public object? Value { set; get; }        
    }
}


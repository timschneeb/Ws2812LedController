using System.Text.Json.Serialization;

namespace Ws2812LedController.Core.Model;

public class EffectDescriptor
{
    public string Name { internal init; get; }
    public string Description { internal init; get; }
    public bool IsSingleShot { internal init; get; }
    public EffectProperty[] Properties { internal init; get; }
    [JsonIgnore] public Type InternalType { set; get; }
}

public class EffectProperty
{
    public string Name { internal init; get; }
    public string Type { internal init; get; }
    public object? DefaultValue { internal init; get; }
    public object? Value { internal set; get; }
    [JsonIgnore] public Type InternalType { set; get; }

}
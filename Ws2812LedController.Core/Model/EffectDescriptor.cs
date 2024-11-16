using System.Reflection;
using System.Text.Json.Serialization;

namespace Ws2812LedController.Core.Model;

public class EffectDescriptor
{
    public string Name { init; get; }
    public string FriendlyName { init; get; }
    public string Description { init; get; }
    public bool IsSingleShot { init; get; }
    public EffectType EffectType { set; get; } = EffectType.Normal;
    public EffectProperty[] Properties { init; get; }
    [JsonIgnore] public Type InternalType { set; get; }
}

public class EffectProperty
{
    public string Name { init; get; }
    public string Type { init; get; }
    public object? DefaultValue { init; get; }
    public object? Value { set; get; }
    [JsonIgnore] public Type InternalType { set; get; }
    [JsonIgnore] public PropertyInfo PropertyInfo { set; get; }
    [JsonIgnore] public Type DeclaringBaseClass { set; get; }

}
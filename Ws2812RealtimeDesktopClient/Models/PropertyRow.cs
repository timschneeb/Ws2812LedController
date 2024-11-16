using System.Reflection;
using Newtonsoft.Json;
using Ws2812LedController.AudioReactive.Utils;
using Ws2812LedController.Core.Model;
using Ws2812RealtimeDesktopClient.Utilities;

namespace Ws2812RealtimeDesktopClient.Models;

public class PropertyRow
{
    public string Name { set; get; }
    public object? Value { set; get; }
    public Type Type { set; get; }
    
    [JsonIgnore]
    public string FriendlyName { set; get; }
    [JsonIgnore]
    public bool IsNullable { set; get; }
    [JsonIgnore]
    public string Group { set; get; }
    [JsonIgnore]
    public IEnumerable<object> Attributes { set; get; }
    
    public PropertyRow(){}
    public PropertyRow(EffectProperty prop)
    {
        Update(prop, false);
    }

    public void Update(EffectProperty prop, bool onlyReflectionInfo)
    {
        if (!onlyReflectionInfo)
        {
            Name = prop.Name;
            Value = prop.Value ?? prop.DefaultValue;
        }

        FriendlyName = FriendlyPropertyNameTable.Lookup(prop.Name) ?? prop.Name + " (???)";
        Type = prop.InternalType;
        IsNullable = Nullable.GetUnderlyingType(prop.PropertyInfo.PropertyType) != null || prop.DefaultValue == null;
        Attributes = prop.PropertyInfo.GetCustomAttributes();

        var desc = ReactiveEffectDescriptorList.Instance.Descriptors
            .FirstOrDefault(x => x.Name == prop.DeclaringBaseClass.Name);
        if (desc == null)
        {
            var attributes = prop.DeclaringBaseClass.GetCustomAttributes(typeof(FriendlyNameAttribute), false);
            if (attributes.Length > 0 && attributes is FriendlyNameAttribute[] attrs)
            {
                Group = attrs.First().FriendlyName;
            }
            else
            {
                Group = prop.DeclaringBaseClass.Name;
            }
        }
        else
        {
            Group = desc.FriendlyName;
        }
    }
}
using Ws2812LedController.Core.Effects.Base;
using Ws2812LedController.Core.Model;

namespace Ws2812LedController.Core.Utils;

public static class EffectDescriptorList
{
    public static EffectDescriptor[] Descriptors { get; }

    static EffectDescriptorList()
    {
        Descriptors = Enumerate();
    }

    public static EffectDescriptor? Create(BaseEffect? effect)
    {
        if (effect == null)
        {
            return null;
        }
        
        var staticDesc = Descriptors.FirstOrDefault(x => x.Name == effect.GetType().Name);
        if (staticDesc == null)
        {
            return null;
        }
        
        // Fill with up-to-date information
        foreach (var property in staticDesc.Properties)
        {
            property.Value = effect.GetType().GetProperty(property.Name)?.GetValue(effect);
        }

        return staticDesc;
    }
    
    public static EffectDescriptor[] Enumerate()
    {
        var effectClassTypes = typeof(BaseEffect).Assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Ws2812LedController.Core.Effects") ?? false)
            .Where(t => !t.IsAbstract && t.IsClass && t.GetProperties().Length > 0)
            .ToArray();

        var descriptors = new List<EffectDescriptor>();
        foreach (var type in effectClassTypes)
        {
            var temp = Activator.CreateInstance(type);
            if (temp is BaseEffect effect)
            {
                var properties = new List<EffectProperty>();
                foreach (var property in type.GetProperties())
                {
                    if (!property.CanWrite || 
                        property.GetCustomAttributes(true)
                            .FirstOrDefault(x => x.GetType() == typeof(NonEffectParameterAttribute)) != null)
                    {
                        continue;
                    }
                    
                    properties.Add(new EffectProperty()
                    {
                        Name = property.Name,
                        DefaultValue = property.GetValue(effect),
                        Value = property.GetValue(effect),
                        Type = property.PropertyType.IsEnum
                            ? Enum.GetUnderlyingType(property.PropertyType).Name : property.PropertyType.Name,
                        InternalType = property.PropertyType
                    });
                }
                
                descriptors.Add(new EffectDescriptor()
                {
                    Name = type.Name,
                    Description = effect.Description,
                    IsSingleShot = effect.IsSingleShot,
                    Properties = properties.ToArray(),
                    InternalType = effect.GetType()
                });
            }
            else
            {
                Console.WriteLine($"EffectList.Enumerate: Failed to activate type '{type.Name}'");
            }
        }
        
        GC.Collect();
        return descriptors.ToArray();
    }
}
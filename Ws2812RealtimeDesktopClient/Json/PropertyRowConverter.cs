using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ws2812RealtimeDesktopClient.Models;
using Ws2812RealtimeDesktopClient.Utilities;

namespace Ws2812RealtimeDesktopClient.Json;

public class PropertyRowConverter : JsonConverter
{
    private readonly Type[] _types;

    public PropertyRowConverter ()
    {
        _types = new[] { typeof(PropertyRow) };
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var t = JToken.FromObject(value);
        if (t.Type != JTokenType.Object)
        {
            t.WriteTo(writer);
        }
        else if(value is PropertyRow p)
        {
            var o = (JObject)t;
            o.RemoveAll();
            
            var val = p.Value?.CastToReflected(p.Type);
            o.AddFirst(new JProperty("Type", p.Type.IsInterface ? p.Value?.GetType().AssemblyQualifiedName : p.Type.AssemblyQualifiedName ?? p.Type.ToString()));
            o.AddFirst(new JProperty("Value", val == null ? null : JToken.FromObject(val, serializer)));
            o.AddFirst(new JProperty("Name", p.Name));
            o.WriteTo(writer);
        }
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        var name = obj["Name"]?.ToString();
        var type = obj["Type"]?.ToString();
        var value = obj["Value"];
        if (name == null || type == null || value == null)
        {
            return null;
        }

        var realType = Type.GetType(type);
        if (realType == null)
        {
            Console.WriteLine($"PropertyRowConverter.ReadJson: Unknown type '{type}'. Cannot convert property '{name}'");
            return null;
        }

        try
        {
            return new PropertyRow()
            {
                Name = name,
                Type = realType,
                Value = value.ToObject(realType, serializer)
            };
        }
        catch (JsonSerializationException ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return _types.Any(t => t == objectType);
    }
}
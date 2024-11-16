using System.ComponentModel.DataAnnotations;

namespace Ws2812RealtimeDesktopClient.Utilities;


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class IpAddressAttribute : DataTypeAttribute
{
    public IpAddressAttribute()
        : base(DataType.Custom)
    {

    }

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true;
        }

        if (value is not string valueAsString)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(valueAsString))
        {
            return false;
        }

        var splitValues = valueAsString.Split('.');
        return splitValues.Length == 4 && splitValues.All(r => byte.TryParse(r, out _));
    }
}
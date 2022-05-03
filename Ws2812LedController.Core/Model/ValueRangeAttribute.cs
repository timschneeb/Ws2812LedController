namespace Ws2812LedController.Core.Model;

public class ValueRangeAttribute : Attribute
{
    public int? Minimum { get; }
    public int? Maximum { get; }

    public ValueRangeAttribute(int max)
    {
        Minimum = null;
        Maximum = max;
    }
    
    public ValueRangeAttribute(int min, int max)
    {
        Minimum = min;
        Maximum = max;
    }
}
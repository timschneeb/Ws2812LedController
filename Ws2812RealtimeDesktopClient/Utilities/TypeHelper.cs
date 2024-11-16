namespace Ws2812RealtimeDesktopClient.Utilities;

public static class TypeHelper
{
    private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
    {
        typeof(int),  typeof(double),  typeof(decimal),
        typeof(long), typeof(short),   typeof(sbyte),
        typeof(byte), typeof(ulong),   typeof(ushort),  
        typeof(uint), typeof(float)
    };
    
    public static bool IsNumeric(this Type myType)
    {
        return NumericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
    }

    public static dynamic CastToNumberType(this Type type, dynamic number)
    {
        if (type == typeof(int))
            return Convert.ToInt32(number);
        if (type == typeof(uint))
            return Convert.ToUInt32(number);
        if (type == typeof(short))
            return Convert.ToInt16(number);
        if (type == typeof(ushort))
            return Convert.ToUInt16(number);
        if (type == typeof(long))
            return Convert.ToInt64(number);
        if (type == typeof(ulong))
            return Convert.ToUInt64(number);
        
        if (type == typeof(byte))
            return Convert.ToByte(number);
        if (type == typeof(sbyte))
            return Convert.ToSByte(number);
        
        if (type == typeof(decimal))
            return Convert.ToDecimal(number);
        if (type == typeof(float))
            return Convert.ToDouble(number);
        if (type == typeof(double))
            return Convert.ToDouble(number);
        
        return number;
    }
}
namespace Ws2812RealtimeDesktopClient.Utilities;

public static class ReflectionUtils
{
    public static Type? FindType(string fullName)
    {
        return
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == fullName);
    }
}
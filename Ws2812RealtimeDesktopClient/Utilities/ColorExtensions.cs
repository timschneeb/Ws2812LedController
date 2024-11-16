using System.Drawing;

namespace Ws2812RealtimeDesktopClient.Utilities;

public static class ColorExtensions
{
    public static bool IsTransparent(this Color col)
    {
        return col.A == 0;
    }

    public static bool AreTransparent(this IEnumerable<Color> cols)
    {
        return cols.All(x => x.IsTransparent());
    }
}
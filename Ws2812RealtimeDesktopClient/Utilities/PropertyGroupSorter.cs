using System.Collections;
using Ws2812RealtimeDesktopClient.Models;

namespace Ws2812RealtimeDesktopClient.Utilities;

public class PropertyGroupSorter: IComparer
{
    public int Compare(object? x, object? y)
    {
        return (int) CompareXy(x as PropertyRow, y as PropertyRow);
    }
    private enum Xy
    {
        X = -1,
        Both = 0,
        Y = 1
    };

    private static Xy CompareXy(PropertyRow? x, PropertyRow? y)
    {
        if (x == null && y == null) return Xy.Both;

        // put any nulls at the end of the list
        if (x == null) return Xy.Y;
        if (y == null) return Xy.X;

        if (x.Group == y.Group) return Xy.Both;

        if (x.Group.Equals("Base effect options")) return Xy.Y;
        if (y.Group.Equals("Base effect options")) return Xy.X;
        
        if (!x.Group.StartsWith("Base")) return Xy.X;
        if (!y.Group.StartsWith("Base")) return Xy.Y;

        return (Xy)StringComparer.Ordinal.Compare(x, y);
    }
}

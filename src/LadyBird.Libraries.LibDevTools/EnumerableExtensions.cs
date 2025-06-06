namespace LadyBird.Libraries.LibDevTools;

using System.Collections.Generic;

public static class EnumerableExtensions
{
    public static T[] Concat<T>(this T[] first, params object[] args)
    {
        var list = new List<T>(first);
        foreach (var arg in args)
        {
            if (arg is T item)
                list.Add(item);
        }
        return list.ToArray();
    }
}

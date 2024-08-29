namespace Computers.Core;

public static class Utils {
    public static void ForEach<T>(
        this IEnumerable<T> source,
        Action<T> action
    ) {
        foreach (var element in source)
            action(element);
    }
}
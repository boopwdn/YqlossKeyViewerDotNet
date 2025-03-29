namespace YqlossKeyViewerDotNet.Utils;

public static class AntiGcUtil
{
    private static List<object?> Objects { get; } = [];

    public static int ObjectCount => Objects.Count;

    public static T AntiGc<T>(this T value)
    {
        Objects.Add(value);
        return value;
    }
}
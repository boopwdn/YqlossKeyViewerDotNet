namespace YqlossKeyViewerDotNet.Utils;

public static class SuppressUtil
{
    public static bool True => true;
    public static bool False => false;

    public static T LateInit<T>()
        where T : class
    {
        return null!;
    }
}
namespace YqlossKeyViewerDotNet.Utils;

// Make Kotlin Great Again!
public static class KotlinDsl
{
    public static TR Let<T, TR>(this T self, Func<T, TR> func)
    {
        return func(self);
    }

    public static T Also<T>(this T self, Action<T> func)
    {
        func(self);
        return self;
    }

    public static TR Run<TR>(Func<TR> func)
    {
        return func();
    }
}
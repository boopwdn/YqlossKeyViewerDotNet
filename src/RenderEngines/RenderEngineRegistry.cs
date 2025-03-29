using YqlossKeyViewerDotNet.Windows;

namespace YqlossKeyViewerDotNet.RenderEngines;

public class RenderEngineRegistry
{
    private delegate IRenderEngine RenderEngineFactory(Window window);

    static RenderEngineRegistry()
    {
        // Registry["GdiPlus"] = window => new GdiPlusRenderEngine(window);
        Registry["Direct2D"] = window => new Direct2DRenderEngine(window);
    }

    private static Dictionary<string, RenderEngineFactory> Registry { get; } = [];

    public static IRenderEngine CreateRenderEngine(string name, Window window)
    {
        return Registry[name](window);
    }
}
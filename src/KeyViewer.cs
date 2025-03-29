using Vanara.PInvoke;
using YqlossKeyViewerDotNet.RenderEngines;
using YqlossKeyViewerDotNet.Storages;
using YqlossKeyViewerDotNet.Utils;
using YqlossKeyViewerDotNet.Windows;

namespace YqlossKeyViewerDotNet;

public class KeyViewer
{
    public static class Initialized
    {
        public static bool Value { get; set; }
    }

    private KeyViewer()
    {
        KeyCounter = new KeyCounter(Config.CounterPaths[Config.CounterPath], Config.SaveCounterInterval);
        KeyManager = new KeyManager(this, Layout.Keys);
        WindowWidth = (int)(Layout.Width * Config.Scale);
        WindowHeight = (int)(Layout.Height * Config.Scale);
        InitializeDragThread();
        KeyViewerWindow = new KeyViewerWindow(Storage.WindowX, Storage.WindowY, WindowWidth, WindowHeight);
        RenderEngine = RenderEngineRegistry.CreateRenderEngine(Config.RenderEngine, KeyViewerWindow);
        Initialized.Value = true;
    }

    public static KeyViewer Instance { get; } = new();

    public Config Config { get; } = Config.ReadOrGenerate(Constants.ConfigurationPath);
    public Layout Layout { get; } = Layout.Read(Constants.LayoutPath);
    public Storage Storage { get; } = Storage.ReadAndSave(Constants.StoragePath);
    public KeyCounter KeyCounter { get; }
    public KeyManager KeyManager { get; }
    public int WindowWidth { get; }
    public int WindowHeight { get; }
    public DragWindow DragWindow { get; set; } = SuppressUtil.LateInit<DragWindow>();
    public KeyViewerWindow KeyViewerWindow { get; }
    public IRenderEngine RenderEngine { get; }

    public void Main()
    {
        User32.SetProcessDpiAwarenessContext(User32.DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        DaemonTimer.FromFrequency(Config.FrameRate, KeyViewerWindow.PostRedrawMessage).AntiGc();
        if (Config.PollKeys) DaemonTimer.FromFrequency(Config.PollRate, () => KeyManager.UpdateKeys()).AntiGc();
        KeyManager.SetHooks();
        KeyViewerWindow.MainLoop();
        KeyCounter.SaveCounter();
        SaveStorage();
    }

    private void InitializeDragThread()
    {
        var finishEvent = new ManualResetEvent(false);
        new Thread(() =>
        {
            DragWindow = new DragWindow(Storage.WindowX, Storage.WindowY, WindowWidth, WindowHeight);
            finishEvent.Set();
            DragWindow.MainLoop();
        }) { IsBackground = true }.AntiGc().Start();
        finishEvent.WaitOne();
    }

    public void UpdateWindowPositionIntoStorage()
    {
        User32.GetWindowRect(KeyViewerWindow.HWnd, out var rect);
        Storage.WindowX = rect.Left;
        Storage.WindowY = rect.Top;
    }

    public void SaveStorage()
    {
        Storage.Save(Constants.StoragePath);
    }
}
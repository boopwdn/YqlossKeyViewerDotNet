using Vanara.PInvoke;

namespace YqlossKeyViewerDotNet.Windows;

public class Window
{
    public Window(
        string className,
        string title,
        User32.WindowStylesEx exStyles,
        User32.WindowStyles styles,
        int x,
        int y,
        int width,
        int height,
        bool showWindow,
        User32.WindowProc windowProc
    )
    {
        var hInstance = Kernel32.GetModuleHandle();

        User32.RegisterClass(
            new User32.WNDCLASS
            {
                hInstance = hInstance,
                lpszClassName = className,
                lpfnWndProc = windowProc
            }
        );

        HWnd = User32.CreateWindowEx(
            exStyles,
            className,
            title,
            styles,
            x,
            y,
            width,
            height,
            0,
            0,
            hInstance
        );

        User32.ShowWindow(HWnd, showWindow ? ShowWindowCommand.SW_SHOW : ShowWindowCommand.SW_HIDE);
    }

    public User32.SafeHWND HWnd { get; }

    public void MainLoop()
    {
        while (User32.GetMessage(out var message, 0) != 0)
        {
            User32.TranslateMessage(message);
            User32.DispatchMessage(message);
        }
    }
}
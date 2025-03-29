using Vanara.PInvoke;

namespace YqlossKeyViewerDotNet.Windows;

public static class CustomMessage
{
    public const User32.WindowMessage Redraw = User32.WindowMessage.WM_USER + 0721 + 0;
    public const User32.WindowMessage Tray = User32.WindowMessage.WM_USER + 0721 + 1;
}
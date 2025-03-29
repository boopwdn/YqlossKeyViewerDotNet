using Vanara.PInvoke;

namespace YqlossKeyViewerDotNet.Windows;

public class KeyViewerWindow(
    int x,
    int y,
    int width,
    int height
) : Window(
    Constants.KeyViewerWindowClass,
    Constants.WindowTitle,
    User32.WindowStylesEx.WS_EX_LAYERED | User32.WindowStylesEx.WS_EX_TRANSPARENT | User32.WindowStylesEx.WS_EX_TOPMOST,
    User32.WindowStyles.WS_POPUP | User32.WindowStyles.WS_VISIBLE,
    x,
    y,
    width,
    height,
    true,
    WndProc
)
{
    public void PostRedrawMessage()
    {
        User32.PostMessage(HWnd, CustomMessage.Redraw);
    }

    private static nint WndProc(HWND hwnd, uint msg, nint wParam, nint lParam)
    {
        switch ((User32.WindowMessage)msg)
        {
            case CustomMessage.Redraw:
            {
                KeyViewer.Instance.RenderEngine.BeginDraw();
                foreach (var element in KeyViewer.Instance.Layout.Elements) element.Draw(KeyViewer.Instance);
                KeyViewer.Instance.RenderEngine.EndDraw();
                return 0;
            }

            case User32.WindowMessage.WM_ERASEBKGND:
            {
                return 1;
            }

            case User32.WindowMessage.WM_DESTROY:
            {
                User32.PostQuitMessage();
                return 0;
            }
        }

        return User32.DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
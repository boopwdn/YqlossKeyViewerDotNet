using System.Runtime.InteropServices;
using Vanara.PInvoke;
using YqlossKeyViewerDotNet.Utils;

namespace YqlossKeyViewerDotNet.Windows;

public class DragWindow : Window
{
    private const nint TrayExit = 0721 + 0;
    private const nint TrayToggleDraggable = 0721 + 1;

    private static Shell32.NOTIFYICONDATA NotifyIconData { get; set; }
    private static User32.SafeHCURSOR CursorMove { get; set; } = SuppressUtil.LateInit<User32.SafeHCURSOR>();

    private static bool Draggable { get; set; }
    private static bool Dragging { get; set; }
    private static int MouseOriginX { get; set; }
    private static int MouseOriginY { get; set; }
    private static int WindowOriginX { get; set; }
    private static int WindowOriginY { get; set; }

    public DragWindow(
        int x,
        int y,
        int width,
        int height
    ) : base(
        Constants.DragWindowClass,
        Constants.WindowTitle,
        User32.WindowStylesEx.WS_EX_TOPMOST,
        User32.WindowStyles.WS_POPUP,
        x,
        y,
        width,
        height,
        false,
        WndProc
    )
    {
        CursorMove = User32.LoadCursor(0, User32.IDC_SIZEALL);
        NotifyIconData = new Shell32.NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<Shell32.NOTIFYICONDATA>(),
            hwnd = HWnd,
            uID = 0,
            uFlags = Shell32.NIF.NIF_ICON | Shell32.NIF.NIF_MESSAGE | Shell32.NIF.NIF_TIP,
            uCallbackMessage = (uint)CustomMessage.Tray,
            hIcon = User32.LoadIcon(0, User32.IDI_APPLICATION),
            szTip = Constants.TrayTip
        };
        Shell32.Shell_NotifyIcon(Shell32.NIM.NIM_ADD, NotifyIconData);
    }

    private static void ShowContextMenu()
    {
        var menu = User32.CreatePopupMenu();
        User32.AppendMenu(
            menu, User32.MenuFlags.MF_STRING, TrayToggleDraggable,
            Draggable ? Constants.TrayTextDisableDragging : Constants.TrayTextEnableDragging
        );
        User32.AppendMenu(
            menu, User32.MenuFlags.MF_STRING, TrayExit,
            Constants.TrayTextExit
        );
        User32.SetForegroundWindow(KeyViewer.Instance.DragWindow.HWnd);
        User32.GetCursorPos(out var mouse);
        User32.TrackPopupMenu(
            menu,
            User32.TrackPopupMenuFlags.TPM_RIGHTBUTTON,
            mouse.x,
            mouse.y,
            0,
            KeyViewer.Instance.DragWindow.HWnd
        );
        User32.PostMessage(KeyViewer.Instance.DragWindow.HWnd, User32.WindowMessage.WM_NULL);
    }

    private static nint ProcessWmCommand(HWND hwnd, uint msg, nint wParam, nint lParam)
    {
        switch ((nint)BitUtil.LowWord((uint)wParam))
        {
            case TrayExit:
            {
                User32.PostMessage(KeyViewer.Instance.KeyViewerWindow.HWnd, User32.WindowMessage.WM_QUIT);
                return 0;
            }

            case TrayToggleDraggable:
            {
                Draggable = !Draggable;
                User32.ShowWindow(
                    KeyViewer.Instance.DragWindow.HWnd,
                    Draggable ? ShowWindowCommand.SW_SHOW : ShowWindowCommand.SW_HIDE
                );
                return 0;
            }
        }

        return User32.DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private static nint WndProc(HWND hwnd, uint msg, nint wParam, nint lParam)
    {
        if (!KeyViewer.Initialized.Value) return User32.DefWindowProc(hwnd, msg, wParam, lParam);

        switch ((User32.WindowMessage)msg)
        {
            case CustomMessage.Tray:
            {
                if (
                    (User32.WindowMessage)lParam
                    is User32.WindowMessage.WM_LBUTTONDOWN
                    or User32.WindowMessage.WM_RBUTTONDOWN
                ) ShowContextMenu();
                return 0;
            }

            case User32.WindowMessage.WM_COMMAND:
            {
                return ProcessWmCommand(hwnd, msg, wParam, lParam);
            }

            case User32.WindowMessage.WM_NCHITTEST:
            {
                var point = new POINT((short)BitUtil.LowWord((uint)lParam), (short)BitUtil.HighWord((uint)lParam));
                User32.ScreenToClient(hwnd, ref point);
                if (
                    0 <= point.x &&
                    point.x < KeyViewer.Instance.WindowWidth &&
                    0 <= point.y &&
                    point.y < KeyViewer.Instance.WindowHeight
                ) return (nint)User32.HitTestValues.HTCLIENT;
                return 0;
            }

            case User32.WindowMessage.WM_MOVE:
            {
                User32.MoveWindow(
                    KeyViewer.Instance.KeyViewerWindow.HWnd,
                    (short)BitUtil.LowWord((uint)lParam),
                    (short)BitUtil.HighWord((uint)lParam),
                    KeyViewer.Instance.WindowWidth,
                    KeyViewer.Instance.WindowHeight,
                    false
                );
                KeyViewer.Instance.UpdateWindowPositionIntoStorage();
                return 0;
            }

            case User32.WindowMessage.WM_LBUTTONDOWN:
            {
                if (Dragging) return 0;
                Dragging = true;
                var mouse = new POINT((short)BitUtil.LowWord((uint)lParam), (short)BitUtil.HighWord((uint)lParam));
                User32.ClientToScreen(hwnd, ref mouse);
                MouseOriginX = mouse.X;
                MouseOriginY = mouse.Y;
                User32.GetWindowRect(hwnd, out var rect);
                WindowOriginX = rect.Left;
                WindowOriginY = rect.Top;
                KeyViewer.Instance.UpdateWindowPositionIntoStorage();
                KeyViewer.Instance.SaveStorage();
                return 0;
            }

            case User32.WindowMessage.WM_LBUTTONUP:
            {
                Dragging = false;
                KeyViewer.Instance.UpdateWindowPositionIntoStorage();
                KeyViewer.Instance.SaveStorage();
                return 0;
            }

            case User32.WindowMessage.WM_MOUSEMOVE:
            {
                User32.SetCursor(CursorMove);
                if (Dragging && (wParam & (nint)MouseButtonState.MK_LBUTTON) == 0)
                {
                    Dragging = false;
                    KeyViewer.Instance.UpdateWindowPositionIntoStorage();
                    KeyViewer.Instance.SaveStorage();
                }

                if (!Dragging) return 0;
                var mouse = new POINT((short)BitUtil.LowWord((uint)lParam), (short)BitUtil.HighWord((uint)lParam));
                User32.ClientToScreen(hwnd, ref mouse);
                User32.MoveWindow(
                    hwnd,
                    mouse.x - MouseOriginX + WindowOriginX,
                    mouse.y - MouseOriginY + WindowOriginY,
                    KeyViewer.Instance.WindowWidth,
                    KeyViewer.Instance.WindowHeight,
                    false
                );
                return 0;
            }

            case User32.WindowMessage.WM_CLOSE:
            {
                return 0;
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
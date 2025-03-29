using Vanara.PInvoke;
using YqlossKeyViewerDotNet.Utils;

namespace YqlossKeyViewerDotNet;

public class KeyInfo
{
    public int Code { get; set; }
    public string Name { get; set; } = SuppressUtil.LateInit<string>();
}

public class KeyRain
{
    public required long From { get; init; }
    public required long Till { get; init; }
}

public class KeyData
{
    public bool Pressed { get; set; }
    public long PressTime { get; set; }
    public List<KeyRain> Rains { get; } = [];
    public List<KeyRain> RainsForRender { get; set; } = [];
}

public class KeyManager(KeyViewer keyViewer, List<KeyInfo> keysInfo)
{
    private List<KeyInfo> KeysInfo { get; } = [..keysInfo];

    private DefaultDictionary<int, int, KeyData> Keys { get; } = new()
    {
        KeyTransformer = it => it,
        DefaultValue = (_, _) => new KeyData()
    };

    public void UpdateKeys(int? keyToUpdate = null, bool updateAsPressed = false)
    {
        lock (this)
        {
            var time = TimeUtil.TickTime();

            foreach (var keyInfo in KeysInfo)
            {
                var pressed = keyInfo.Code == keyToUpdate
                    ? updateAsPressed
                    : (User32.GetAsyncKeyState(keyInfo.Code) & 0x8000) != 0;
                var keyData = Keys[keyInfo.Code];

                if (pressed && !keyData.Pressed)
                {
                    keyData.PressTime = time;
                    keyViewer.KeyCounter.AccumulateCounter(keyInfo.Code);
                    TriggerInstantKey(Constants.GlobalKeyCode, time);
                }
                else if (!pressed && keyData.Pressed)
                {
                    keyData.Rains.Add(
                        new KeyRain
                        {
                            From = keyData.PressTime,
                            Till = time
                        }
                    );
                }

                keyData.Pressed = pressed;
            }

            foreach (var keyData in Keys.Data.Values)
            {
                keyData.Rains.RemoveAll(
                    rain => TimeUtil.TickToNano(time - rain.Till) > keyViewer.Config.MaxDuration
                );

                keyData.RainsForRender = [..keyData.Rains];
            }
        }
    }

    public void TriggerInstantKey(int code, long time)
    {
        keyViewer.KeyCounter.AccumulateCounter(code);
        Keys[code].Rains.Add(new KeyRain { From = time, Till = time });
    }

    public KeyData GetKeyData(int keyIndex)
    {
        return keyIndex < 0 ? Keys[-keyIndex] : Keys[KeysInfo[keyIndex].Code];
    }

    public KeyInfo GetKeyInfo(int keyIndex)
    {
        return keyIndex < 0 ? new KeyInfo { Code = -keyIndex, Name = $"Key{-keyIndex}" } : KeysInfo[keyIndex];
    }

    public void SetHooks()
    {
        var hInstance = Kernel32.GetModuleHandle();
        if (keyViewer.Config.HookKeyboard)
            User32.SetWindowsHookEx(User32.HookType.WH_KEYBOARD_LL, KeyboardHookProc, hInstance).AntiGc();
        if (keyViewer.Config.HookMouse)
            User32.SetWindowsHookEx(User32.HookType.WH_MOUSE_LL, MouseHookProc, hInstance).AntiGc();
    }

    private static unsafe nint KeyboardHookProc(int code, nint wParam, nint lParam)
    {
        var data = *(User32.KBDLLHOOKSTRUCT*)lParam;
        KeyViewer.Instance.KeyManager.UpdateKeys((int)data.vkCode, (data.flags & 0x80) == 0);
        return User32.CallNextHookEx(0, code, wParam, lParam);
    }

    private static unsafe nint MouseHookProc(int code, nint wParam, nint lParam)
    {
        if (
            (User32.WindowMessage)wParam
            is User32.WindowMessage.WM_MOUSEMOVE
            or User32.WindowMessage.WM_MOUSEWHEEL
            or User32.WindowMessage.WM_MOUSEHWHEEL
        ) return User32.CallNextHookEx(0, code, wParam, lParam);

        var data = *(User32.MSLLHOOKSTRUCT*)lParam;
        int? keyCode;
        bool pressed;

        switch ((User32.WindowMessage)wParam)
        {
            case User32.WindowMessage.WM_LBUTTONDOWN:
            {
                keyCode = 1;
                pressed = true;
                break;
            }

            case User32.WindowMessage.WM_LBUTTONUP:
            {
                keyCode = 1;
                pressed = false;
                break;
            }

            case User32.WindowMessage.WM_RBUTTONDOWN:
            {
                keyCode = 2;
                pressed = true;
                break;
            }

            case User32.WindowMessage.WM_RBUTTONUP:
            {
                keyCode = 2;
                pressed = false;
                break;
            }

            case User32.WindowMessage.WM_MBUTTONDOWN:
            {
                keyCode = 4;
                pressed = true;
                break;
            }

            case User32.WindowMessage.WM_MBUTTONUP:
            {
                keyCode = 4;
                pressed = false;
                break;
            }

            case User32.WindowMessage.WM_XBUTTONDOWN:
            {
                var button = (User32.VK)BitUtil.HighWord(data.mouseData);
                keyCode = button == User32.VK.VK_XBUTTON2 ? 6 : 5;
                pressed = true;
                break;
            }

            case User32.WindowMessage.WM_XBUTTONUP:
            {
                var button = (User32.VK)BitUtil.HighWord(data.mouseData);
                keyCode = button == User32.VK.VK_XBUTTON2 ? 6 : 5;
                pressed = false;
                break;
            }

            default:
            {
                return User32.CallNextHookEx(0, code, wParam, lParam);
            }
        }

        KeyViewer.Instance.KeyManager.UpdateKeys(keyCode, pressed);
        return User32.CallNextHookEx(0, code, wParam, lParam);
    }
}
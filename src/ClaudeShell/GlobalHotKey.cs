using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ClaudeShell;

public sealed class GlobalHotKey : IDisposable
{
    const uint NoRepeat = 0x4000;
    const int WmHotKey = 0x0312;
    const int Id = 0x434C;

    readonly IntPtr _handle;
    readonly HwndSource _source;
    readonly Action _pressed;
    bool _registered;

    public GlobalHotKey(IntPtr handle, Action pressed)
    {
        _handle = handle;
        _pressed = pressed;
        _source = HwndSource.FromHwnd(handle) ?? throw new InvalidOperationException("No window source.");
        _source.AddHook(OnMessage);
    }

    public bool Register(uint modifiers, uint key)
    {
        if (_registered)
        {
            UnregisterHotKey(_handle, Id);
        }

        _registered = RegisterHotKey(_handle, Id, modifiers | NoRepeat, key);

        if (_registered)
        {
            Log.Info($"Hotkey registered (modifiers=0x{modifiers:X}, vk=0x{key:X}).");
        }
        else
        {
            Log.Warn($"Hotkey registration failed (modifiers=0x{modifiers:X}, vk=0x{key:X}, error={Marshal.GetLastWin32Error()}).");
        }

        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(_handle, Id);
            _registered = false;
            Log.Info("Hotkey unregistered.");
        }
    }

    public void Dispose()
    {
        _source.RemoveHook(OnMessage);
        Unregister();
    }

    IntPtr OnMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == Id)
        {
            handled = true;
            Log.Info("Hotkey pressed.");
            _pressed();
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

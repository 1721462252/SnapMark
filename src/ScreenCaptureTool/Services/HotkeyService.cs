using System.Windows.Forms;
using ScreenCaptureTool.Core;
using ScreenCaptureTool.Native;

namespace ScreenCaptureTool.Services;

public sealed class HotkeyService : NativeWindow, IDisposable
{
    private const int HotkeyId = 0x5343;
    private const int WmHotkey = 0x0312;
    private bool disposed;

    public HotkeyService()
    {
        CreateHandle(new CreateParams());
    }

    public event EventHandler? HotkeyPressed;

    public HotkeyGesture? Current { get; private set; }

    public bool Register(HotkeyGesture gesture)
    {
        Unregister();
        bool registered = NativeMethods.RegisterHotKey(Handle, HotkeyId, gesture.ToWin32Modifiers(), (uint)gesture.Key);
        Current = registered ? gesture : null;
        return registered;
    }

    public void Unregister()
    {
        if (Current is not null)
        {
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);
            Current = null;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Unregister();
        DestroyHandle();
        disposed = true;
        GC.SuppressFinalize(this);
    }
}

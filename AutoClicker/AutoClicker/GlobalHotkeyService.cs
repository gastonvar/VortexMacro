namespace AutoClicker;

internal sealed class GlobalHotkeyService : IDisposable
{
    private readonly IntPtr _windowHandle;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 1;

    public GlobalHotkeyService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public bool Register(HotkeyChord chord, Action handler)
    {
        if (!chord.IsValid)
        {
            return false;
        }

        int id = _nextId++;
        if (!NativeMethods.RegisterHotKey(_windowHandle, id, chord.Modifiers, (uint)chord.VirtualKey))
        {
            return false;
        }

        _handlers[id] = handler;
        return true;
    }

    public void HandleHotkey(int hotkeyId)
    {
        if (_handlers.TryGetValue(hotkeyId, out Action? handler))
        {
            handler();
        }
    }

    public void Dispose()
    {
        foreach (int id in _handlers.Keys)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, id);
        }

        _handlers.Clear();
    }
}

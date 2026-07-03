using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AutoClicker;

internal sealed class MacroRecorderService : IDisposable
{
    private readonly Stopwatch _stopwatch = new();
    private readonly List<RecordedMacroEvent> _events = [];
    private readonly HashSet<int> _suppressedKeys = [];
    private IntPtr _mouseHook;
    private IntPtr _keyboardHook;
    private NativeMethods.LowLevelMouseProc? _mouseProc;
    private NativeMethods.LowLevelKeyboardProc? _keyboardProc;
    private long _lastEventMs;
    private bool _isRecording;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler? EventsChanged;

    public bool IsRecording => _isRecording;
    public IReadOnlyList<RecordedMacroEvent> Events => _events;

    public void StartRecording(IEnumerable<int>? suppressVirtualKeys = null)
    {
        if (_isRecording)
        {
            return;
        }

        _events.Clear();
        _suppressedKeys.Clear();
        if (suppressVirtualKeys != null)
        {
            foreach (int key in suppressVirtualKeys)
            {
                _suppressedKeys.Add(key);
            }
        }

        _mouseProc = MouseHookCallback;
        _keyboardProc = KeyboardHookCallback;
        using Process process = Process.GetCurrentProcess();
        using ProcessModule? module = process.MainModule;
        IntPtr moduleHandle = NativeMethods.GetModuleHandle(module?.ModuleName ?? process.ProcessName);

        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WhMouseLl, _mouseProc, moduleHandle, 0);
        _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WhKeyboardLl, _keyboardProc, moduleHandle, 0);

        _lastEventMs = 0;
        _stopwatch.Restart();
        _isRecording = true;
        OnStatusChanged("Recording started. Perform clicks, moves, scrolls, and keystrokes.");
        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    public RecordedMacro StopRecording(string name)
    {
        if (_isRecording)
        {
            Unhook();
            _isRecording = false;
            _stopwatch.Stop();
            OnStatusChanged($"Recording stopped. Captured {_events.Count} events.");
        }

        return new RecordedMacro
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Untitled Macro" : name,
            CreatedAt = DateTime.UtcNow,
            Events = _events.Select(CloneEvent).ToList()
        };
    }

    public void LoadEvents(IEnumerable<RecordedMacroEvent> events)
    {
        _events.Clear();
        _events.AddRange(events.Select(CloneEvent));
        EventsChanged?.Invoke(this, EventArgs.Empty);
        OnStatusChanged($"Loaded {_events.Count} recorded events.");
    }

    public void Clear()
    {
        _events.Clear();
        EventsChanged?.Invoke(this, EventArgs.Empty);
        OnStatusChanged("Recorder cleared.");
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isRecording)
        {
            var hook = Marshal.PtrToStructure<NativeMethods.MsLlHookStruct>(lParam);
            int message = wParam.ToInt32();

            switch (message)
            {
                case NativeMethods.WmMouseMove:
                    AddEvent(new RecordedMacroEvent
                    {
                        Type = RecordedEventType.MouseMove,
                        X = hook.Pt.X,
                        Y = hook.Pt.Y
                    });
                    break;
                case NativeMethods.WmLButtonDown:
                case NativeMethods.WmLButtonUp:
                case NativeMethods.WmRButtonDown:
                case NativeMethods.WmRButtonUp:
                case NativeMethods.WmMButtonDown:
                case NativeMethods.WmMButtonUp:
                    AddEvent(new RecordedMacroEvent
                    {
                        Type = RecordedEventType.MouseClick,
                        X = hook.Pt.X,
                        Y = hook.Pt.Y,
                        Button = GetMouseButton(message),
                        Action = message is NativeMethods.WmLButtonUp or NativeMethods.WmRButtonUp or NativeMethods.WmMButtonUp ? "Up" : "Down"
                    });
                    break;
                case NativeMethods.WmMouseWheel:
                    AddEvent(new RecordedMacroEvent
                    {
                        Type = RecordedEventType.MouseScroll,
                        X = hook.Pt.X,
                        Y = hook.Pt.Y,
                        ScrollDelta = (short)((hook.MouseData >> 16) & 0xffff)
                    });
                    break;
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isRecording)
        {
            var hook = Marshal.PtrToStructure<NativeMethods.KeyboardLlHookStruct>(lParam);
            int virtualKey = (int)hook.VkCode;
            if (_suppressedKeys.Contains(virtualKey))
            {
                return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
            }

            bool isKeyUp = (hook.Flags & 0x80) != 0;
            AddEvent(new RecordedMacroEvent
            {
                Type = isKeyUp ? RecordedEventType.KeyUp : RecordedEventType.KeyDown,
                VirtualKey = virtualKey
            });
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private void AddEvent(RecordedMacroEvent recordedEvent)
    {
        long now = _stopwatch.ElapsedMilliseconds;
        recordedEvent.DelayMs = (int)Math.Max(0, now - _lastEventMs);
        _lastEventMs = now;
        _events.Add(recordedEvent);
        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string GetMouseButton(int message)
    {
        return message switch
        {
            NativeMethods.WmLButtonDown or NativeMethods.WmLButtonUp => "Left",
            NativeMethods.WmRButtonDown or NativeMethods.WmRButtonUp => "Right",
            _ => "Middle"
        };
    }

    private static RecordedMacroEvent CloneEvent(RecordedMacroEvent source) => new()
    {
        Type = source.Type,
        DelayMs = source.DelayMs,
        X = source.X,
        Y = source.Y,
        Button = source.Button,
        Action = source.Action,
        ScrollDelta = source.ScrollDelta,
        VirtualKey = source.VirtualKey
    };

    private void Unhook()
    {
        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }

        if (_keyboardHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
    }

    private void OnStatusChanged(string message) => StatusChanged?.Invoke(this, message);

    public void Dispose()
    {
        Unhook();
    }
}

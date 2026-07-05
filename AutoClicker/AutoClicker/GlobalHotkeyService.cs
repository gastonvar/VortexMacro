namespace AutoClicker;

internal sealed class GlobalHotkeyService : IDisposable
{
    private readonly IntPtr _windowHandle;
    private readonly Dictionary<int, Action> _handlers = new();
    private readonly Dictionary<string, List<string>> _chordOwners = new();
    private int _nextId = 1;

    public GlobalHotkeyService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public IReadOnlyList<string> Conflicts { get; private set; } = [];

    public bool Register(string owner, HotkeyChord chord, Action handler)
    {
        if (!chord.IsValid)
        {
            return false;
        }

        string key = ChordKey(chord);
        if (!_chordOwners.TryGetValue(key, out List<string>? owners))
        {
            owners = [];
            _chordOwners[key] = owners;
        }

        owners.Add(owner);

        int id = _nextId++;
        if (!NativeMethods.RegisterHotKey(_windowHandle, id, chord.Modifiers, (uint)chord.VirtualKey))
        {
            owners.Remove(owner);
            return false;
        }

        _handlers[id] = handler;
        RefreshConflicts();
        return true;
    }

    public void HandleHotkey(int hotkeyId)
    {
        if (_handlers.TryGetValue(hotkeyId, out Action? handler))
        {
            handler();
        }
    }

    private void RefreshConflicts()
    {
        Conflicts = _chordOwners
            .Where(pair => pair.Value.Count > 1)
            .Select(pair => $"{FormatChord(pair.Key)}: {string.Join(", ", pair.Value.Distinct())}")
            .ToList();
    }

    private static string ChordKey(HotkeyChord chord) => $"{chord.Modifiers}:{chord.VirtualKey}";

    private static string FormatChord(string key)
    {
        string[] parts = key.Split(':');
        if (parts.Length != 2)
        {
            return key;
        }

        var chord = new HotkeyChord
        {
            Modifiers = uint.Parse(parts[0]),
            VirtualKey = int.Parse(parts[1])
        };
        return HotkeyFormatting.Format(chord);
    }

    public void Dispose()
    {
        foreach (int id in _handlers.Keys)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, id);
        }

        _handlers.Clear();
        _chordOwners.Clear();
    }
}

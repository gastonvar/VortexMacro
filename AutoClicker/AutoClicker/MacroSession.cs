namespace AutoClicker;

internal enum MacroSessionKind
{
    None,
    Vortex,
    AutoClicker,
    Playback,
    Piano
}

internal sealed class MacroSession : IDisposable
{
    private MacroSessionKind _active = MacroSessionKind.None;

    public MacroSessionKind Active => _active;

    public bool TryBegin(MacroSessionKind kind)
    {
        if (_active != MacroSessionKind.None && _active != kind)
        {
            return false;
        }

        _active = kind;
        return true;
    }

    public void End(MacroSessionKind kind)
    {
        if (_active == kind)
        {
            _active = MacroSessionKind.None;
        }
    }

    public void ForceEnd() => _active = MacroSessionKind.None;

    public void Dispose() => _active = MacroSessionKind.None;
}

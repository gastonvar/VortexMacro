namespace AutoClicker;

internal sealed class HotkeyButtonTracker
{
    private readonly Dictionary<Button, string> _baseLabels = new();
    private readonly Dictionary<Button, Func<HotkeyChord>> _providers = new();
    private readonly HashSet<Button> _navButtons = new();

    public void Track(Button button, string baseLabel, Func<HotkeyChord> provider, bool isNav = false)
    {
        _baseLabels[button] = baseLabel;
        _providers[button] = provider;
        if (isNav)
        {
            _navButtons.Add(button);
        }
    }

    public void Refresh(Button button)
    {
        if (!_baseLabels.TryGetValue(button, out string? baseLabel) ||
            !_providers.TryGetValue(button, out Func<HotkeyChord>? provider))
        {
            return;
        }

        HotkeyChord chord = provider();
        button.Text = _navButtons.Contains(button)
            ? FormatNav(baseLabel, chord)
            : FormatAction(baseLabel, chord);
    }

    public void RefreshAll()
    {
        foreach (Button button in _baseLabels.Keys)
        {
            Refresh(button);
        }
    }

    public void SetBaseLabel(Button button, string baseLabel)
    {
        _baseLabels[button] = baseLabel;
        Refresh(button);
    }

    public IEnumerable<Button> Buttons => _baseLabels.Keys;

    private static string FormatAction(string label, HotkeyChord chord) =>
        chord.IsValid ? $"{label} ({HotkeyFormatting.Format(chord)})" : label;

    private static string FormatNav(string label, HotkeyChord chord) =>
        chord.IsValid ? $"{label}\n{HotkeyFormatting.Format(chord)}" : label;
}

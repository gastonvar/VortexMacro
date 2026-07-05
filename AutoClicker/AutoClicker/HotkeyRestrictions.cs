namespace AutoClicker;

internal static class HotkeyRestrictions
{
    public static readonly HotkeyChord NavVortex = HotkeyChord.FromKey(Keys.F1);
    public static readonly HotkeyChord NavAutoClicker = HotkeyChord.FromKey(Keys.F2);
    public static readonly HotkeyChord NavRecorder = HotkeyChord.FromKey(Keys.F3);
    public static readonly HotkeyChord NavPiano = HotkeyChord.FromKey(Keys.F4);

    public static bool IsReservedNavigationKey(Keys key) =>
        key is Keys.F1 or Keys.F2 or Keys.F3 or Keys.F4;
}

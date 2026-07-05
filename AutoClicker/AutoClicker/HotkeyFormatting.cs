namespace AutoClicker;

internal static class HotkeyFormatting
{
    public static string Format(HotkeyChord chord)
    {
        if (!chord.IsValid)
        {
            return "None";
        }

        var parts = new List<string>();
        if ((chord.Modifiers & NativeMethods.ModControl) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((chord.Modifiers & NativeMethods.ModAlt) != 0)
        {
            parts.Add("Alt");
        }

        if ((chord.Modifiers & NativeMethods.ModShift) != 0)
        {
            parts.Add("Shift");
        }

        parts.Add(chord.Key.ToString());
        return string.Join("+", parts);
    }
}

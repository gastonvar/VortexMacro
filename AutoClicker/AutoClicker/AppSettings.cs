using System.Text.Json;

namespace AutoClicker;

internal sealed class AppSettings
{
    public MacroSettings Vortex { get; set; } = MacroSettings.CreateDefault();
    public AutoClickerSettings AutoClicker { get; set; } = new();
    public HotkeySettings Hotkeys { get; set; } = new();

    public static AppSettings CreateDefault() => new();

    public static AppSettings Load(string filePath)
    {
        string legacyPath = Path.Combine(Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory, "macro-settings.json");
        if (!File.Exists(filePath) && File.Exists(legacyPath))
        {
            filePath = legacyPath;
        }

        if (!File.Exists(filePath))
        {
            return CreateDefault();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("Vortex", out _))
            {
                AppSettings settings = JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefault();
                if (document.RootElement.TryGetProperty("Hotkeys", out JsonElement hotkeysElement))
                {
                    HotkeySettings.MigrateLegacyHotkeys(settings.Hotkeys, hotkeysElement);
                }

                return settings;
            }

            MacroSettings legacy = JsonSerializer.Deserialize<MacroSettings>(json) ?? MacroSettings.CreateDefault();
            return new AppSettings { Vortex = legacy };
        }
        catch
        {
            return CreateDefault();
        }
    }

    public void Save(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(this, options));
    }
}

internal sealed class AutoClickerSettings
{
    public int IntervalMs { get; set; } = 500;
    public int ClickCount { get; set; } = 10;
    public string ClickType { get; set; } = "Left";
    public int StartDelayMs { get; set; } = 3000;
}

internal sealed class HotkeySettings
{
    public HotkeyChord Start { get; set; } = HotkeyChord.FromKey(Keys.F9);
    public HotkeyChord Stop { get; set; } = HotkeyChord.FromKey(Keys.F10);
    public HotkeyChord Play { get; set; } = HotkeyChord.FromKey(Keys.F7);
    public HotkeyChord Save { get; set; } = HotkeyChord.CtrlAlt(Keys.S);
    public HotkeyChord Reset { get; set; } = HotkeyChord.CtrlAlt(Keys.R);
    public HotkeyChord Import { get; set; } = HotkeyChord.CtrlAlt(Keys.I);
    public HotkeyChord Export { get; set; } = HotkeyChord.CtrlAlt(Keys.E);

    public HotkeyChord NavVortex { get; set; } = HotkeyChord.FromKey(Keys.F1);
    public HotkeyChord NavAutoClicker { get; set; } = HotkeyChord.FromKey(Keys.F2);
    public HotkeyChord NavRecorder { get; set; } = HotkeyChord.FromKey(Keys.F3);

    internal static void MigrateLegacyHotkeys(HotkeySettings hotkeys, JsonElement hotkeysElement)
    {
        static HotkeyChord ReadChord(JsonElement parent, string legacyName, string sharedName)
        {
            if (parent.TryGetProperty(sharedName, out JsonElement shared))
            {
                return JsonSerializer.Deserialize<HotkeyChord>(shared.GetRawText()) ?? new HotkeyChord();
            }

            if (parent.TryGetProperty(legacyName, out JsonElement legacy))
            {
                return JsonSerializer.Deserialize<HotkeyChord>(legacy.GetRawText()) ?? new HotkeyChord();
            }

            return new HotkeyChord();
        }

        HotkeyChord start = ReadChord(hotkeysElement, "VortexStart", "Start");
        if (start.IsValid)
        {
            hotkeys.Start = start;
        }

        HotkeyChord stop = ReadChord(hotkeysElement, "VortexStop", "Stop");
        if (stop.IsValid)
        {
            hotkeys.Stop = stop;
        }

        HotkeyChord play = ReadChord(hotkeysElement, "RecorderPlay", "Play");
        if (play.IsValid)
        {
            hotkeys.Play = play;
        }

        HotkeyChord save = ReadChord(hotkeysElement, "SaveSettings", "Save");
        if (save.IsValid)
        {
            hotkeys.Save = save;
        }

        HotkeyChord reset = ReadChord(hotkeysElement, "VortexReset", "Reset");
        if (reset.IsValid)
        {
            hotkeys.Reset = reset;
        }

        HotkeyChord import = ReadChord(hotkeysElement, "RecorderImport", "Import");
        if (import.IsValid)
        {
            hotkeys.Import = import;
        }

        HotkeyChord export = ReadChord(hotkeysElement, "RecorderExport", "Export");
        if (export.IsValid)
        {
            hotkeys.Export = export;
        }
    }
}

using System.Text.Json;

namespace AutoClicker;

internal sealed class AppSettings
{
    public MacroSettings Vortex { get; set; } = MacroSettings.CreateDefault();
    public List<VortexProfile> VortexProfiles { get; set; } = [new VortexProfile()];
    public string ActiveVortexProfile { get; set; } = "Default";
    public AutoClickerSettings AutoClicker { get; set; } = new();
    public RecorderSettings Recorder { get; set; } = new();
    public PianoPlayerSettings Piano { get; set; } = new();
    public HotkeySettings Hotkeys { get; set; } = new();
    public AppBehaviorSettings Behavior { get; set; } = new();

    public static AppSettings CreateDefault() => new();

    public void ApplyActiveProfile()
    {
        VortexProfile? profile = VortexProfiles.FirstOrDefault(p =>
            p.Name.Equals(ActiveVortexProfile, StringComparison.OrdinalIgnoreCase));
        if (profile != null)
        {
            Vortex = profile.Settings.Clone();
        }
    }

    public void SeedProfilesFromVortex()
    {
        string profileName = string.IsNullOrWhiteSpace(ActiveVortexProfile) ? "Default" : ActiveVortexProfile;
        VortexProfile? existing = VortexProfiles.FirstOrDefault(p =>
            p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.Settings = Vortex.Clone();
        }
        else
        {
            VortexProfiles.Clear();
            VortexProfiles.Add(new VortexProfile { Name = profileName, Settings = Vortex.Clone() });
        }

        ActiveVortexProfile = profileName;
    }

    public void SaveActiveProfile()
    {
        VortexProfile? profile = VortexProfiles.FirstOrDefault(p =>
            p.Name.Equals(ActiveVortexProfile, StringComparison.OrdinalIgnoreCase));
        if (profile == null)
        {
            profile = new VortexProfile { Name = ActiveVortexProfile };
            VortexProfiles.Add(profile);
        }

        profile.Settings = Vortex.Clone();
    }

    public static AppSettings Load(string filePath, Action<string>? logError = null)
    {
        string? resolved = SettingsPaths.TryResolveExistingSettingsFile(filePath);
        if (resolved == null)
        {
            return CreateDefault();
        }

        try
        {
            string json = File.ReadAllText(resolved);
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty("Vortex", out _))
            {
                AppSettings settings = JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefault();

                if (document.RootElement.TryGetProperty("Hotkeys", out JsonElement hotkeysElement))
                {
                    HotkeySettings.MigrateLegacyHotkeys(settings.Hotkeys, hotkeysElement);
                }

                bool hasProfilesInFile = document.RootElement.TryGetProperty("VortexProfiles", out JsonElement profilesElement)
                    && profilesElement.ValueKind == JsonValueKind.Array
                    && profilesElement.GetArrayLength() > 0;

                if (hasProfilesInFile)
                {
                    settings.ApplyActiveProfile();
                }
                else
                {
                    settings.SeedProfilesFromVortex();
                }

                return settings;
            }

            MacroSettings legacy = JsonSerializer.Deserialize<MacroSettings>(json) ?? MacroSettings.CreateDefault();
            AppSettings fromLegacy = new() { Vortex = legacy };
            fromLegacy.SeedProfilesFromVortex();
            return fromLegacy;
        }
        catch (Exception ex)
        {
            logError?.Invoke($"Failed to load settings from {resolved}: {ex.Message}. Using defaults.");
            TryBackupCorruptFile(resolved);
            return CreateDefault();
        }
    }

    public void Save(string filePath)
    {
        SaveActiveProfile();
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(this, options));
    }

    public static void ExportAll(string filePath, AppSettings settings)
    {
        settings.SaveActiveProfile();
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(settings, options));
    }

    public static AppSettings ImportAll(string filePath, Action<string>? logError = null)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            using var document = JsonDocument.Parse(json);
            AppSettings settings = JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefault();

            bool hasProfilesInFile = document.RootElement.TryGetProperty("VortexProfiles", out JsonElement profilesElement)
                && profilesElement.ValueKind == JsonValueKind.Array
                && profilesElement.GetArrayLength() > 0;

            if (hasProfilesInFile)
            {
                settings.ApplyActiveProfile();
            }
            else
            {
                settings.SeedProfilesFromVortex();
            }

            return settings;
        }
        catch (Exception ex)
        {
            logError?.Invoke($"Failed to import settings: {ex.Message}");
            return CreateDefault();
        }
    }

    private static void TryBackupCorruptFile(string filePath)
    {
        try
        {
            string backup = filePath + $".corrupt.{DateTime.Now:yyyyMMddHHmmss}.bak";
            File.Copy(filePath, backup, overwrite: true);
        }
        catch
        {
            // Best effort.
        }
    }
}

internal sealed class AutoClickerSettings
{
    public int IntervalMs { get; set; } = 500;
    public int ClickCount { get; set; } = 10;
    public string ClickType { get; set; } = "Left";
    public int StartDelayMs { get; set; } = 3000;
    public int JitterMs { get; set; } = 0;
    public bool InfiniteMode { get; set; }
}

internal sealed class RecorderSettings
{
    public int PlaybackLoops { get; set; } = 1;
    public int MoveThrottleMs { get; set; } = 30;
    public bool FilterRedundantMoves { get; set; } = true;
}

internal sealed class PianoPlayerSettings
{
    public string? LastSongPath { get; set; }
    public int NoteMs { get; set; } = 120;
    public int HoldMultiplierMs { get; set; } = 90;
    public int PauseMs { get; set; } = 80;
    public int LineGapMs { get; set; } = 400;
    public double CountdownSeconds { get; set; } = 3;
    public int RepeatLines { get; set; } = 1;
    public bool Lowercase { get; set; } = true;
}

internal sealed class AppBehaviorSettings
{
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public bool ShowOverlay { get; set; } = true;
    public bool StartWithWindows { get; set; }
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
        if (start.IsValid) hotkeys.Start = start;

        HotkeyChord stop = ReadChord(hotkeysElement, "VortexStop", "Stop");
        if (stop.IsValid) hotkeys.Stop = stop;

        HotkeyChord play = ReadChord(hotkeysElement, "RecorderPlay", "Play");
        if (play.IsValid) hotkeys.Play = play;

        HotkeyChord save = ReadChord(hotkeysElement, "SaveSettings", "Save");
        if (save.IsValid) hotkeys.Save = save;

        HotkeyChord reset = ReadChord(hotkeysElement, "VortexReset", "Reset");
        if (reset.IsValid) hotkeys.Reset = reset;

        HotkeyChord import = ReadChord(hotkeysElement, "RecorderImport", "Import");
        if (import.IsValid) hotkeys.Import = import;

        HotkeyChord export = ReadChord(hotkeysElement, "RecorderExport", "Export");
        if (export.IsValid) hotkeys.Export = export;
    }
}

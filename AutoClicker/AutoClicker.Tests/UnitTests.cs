using AutoClicker.Controls;
using System.Text.Json;
using Xunit;

namespace AutoClicker.Tests;

public class AppSettingsLoadTests
{
    [Fact]
    public void Load_keeps_vortex_when_profiles_missing_from_file()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GasvarMacroTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string path = Path.Combine(tempDir, "app-settings.json");

        var payload = new
        {
            Vortex = new { VortexXStart = 4242, CycleDelayMs = 777 },
            Hotkeys = new { Start = new { Modifiers = 0, VirtualKey = 120 } }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        try
        {
            AppSettings settings = AppSettings.Load(path);
            Assert.Equal(4242, settings.Vortex.VortexXStart);
            Assert.Equal(777, settings.Vortex.CycleDelayMs);
            Assert.Equal(4242, settings.VortexProfiles[0].Settings.VortexXStart);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}

public class VortexViewTests
{
    [Fact]
    public void LoadProfiles_does_not_raise_profile_changed_during_programmatic_load()
    {
        var view = new VortexView();
        string? changedProfile = null;
        view.ProfileChanged += (_, name) => changedProfile = name;

        view.LoadProfiles(["Default", "Alt"], "Default");
        view.LoadSettings(new MacroSettings { CycleDelayMs = 4242, VortexXStart = 1111 });

        Assert.Null(changedProfile);
        Assert.Equal(4242, view.GetInputValue(nameof(MacroSettings.CycleDelayMs)));
        Assert.Equal(1111, view.GetInputValue(nameof(MacroSettings.VortexXStart)));
    }
}

public class HotkeyChordTests
{
    [Fact]
    public void IsValid_returns_false_for_empty_key()
    {
        var chord = new HotkeyChord();
        Assert.False(chord.IsValid);
    }

    [Fact]
    public void FromKey_creates_valid_chord()
    {
        var chord = HotkeyChord.FromKey(Keys.F9);
        Assert.True(chord.IsValid);
        Assert.Equal(Keys.F9, chord.Key);
    }

    [Fact]
    public void Serializes_round_trip()
    {
        var original = HotkeyChord.CtrlAlt(Keys.S);
        string json = JsonSerializer.Serialize(original);
        HotkeyChord? restored = JsonSerializer.Deserialize<HotkeyChord>(json);
        Assert.NotNull(restored);
        Assert.Equal(original.Modifiers, restored.Modifiers);
        Assert.Equal(original.VirtualKey, restored.VirtualKey);
    }
}

public class HotkeyFormattingTests
{
    [Fact]
    public void Format_includes_modifiers()
    {
        string text = HotkeyFormatting.Format(HotkeyChord.CtrlAlt(Keys.S));
        Assert.Equal("Ctrl+Alt+S", text);
    }
}

public class RecordedMacroEventTests
{
    [Fact]
    public void Describe_formats_mouse_click()
    {
        var e = new RecordedMacroEvent
        {
            Type = RecordedEventType.MouseClick,
            X = 100,
            Y = 200,
            Button = "Left",
            Action = "Down",
            DelayMs = 50
        };

        Assert.Contains("Left Down", e.Describe());
        Assert.Contains("100", e.Describe());
    }
}

public class MacroSettingsTests
{
    [Fact]
    public void Clone_creates_independent_copy()
    {
        MacroSettings original = MacroSettings.CreateDefault();
        original.CycleDelayMs = 999;
        MacroSettings clone = original.Clone();
        clone.CycleDelayMs = 1;
        Assert.Equal(999, original.CycleDelayMs);
    }
}

public class PianoSongParserTests
{
    [Fact]
    public void ParseSong_reads_simple_melody_lines()
    {
        const string text = """
            # comment
            A A G G
            F F D D
            """;

        IReadOnlyList<IReadOnlyList<PianoToken>> song = PianoSongParser.ParseSong(text);
        Assert.Equal(2, song.Count);
        Assert.Equal(7, song[0].Count);
        Assert.Equal(PianoTokenKind.Note, song[0][0].Kind);
        Assert.Equal('A', song[0][0].NoteChar);
        Assert.Equal(PianoTokenKind.Pause, song[0][1].Kind);
    }

    [Fact]
    public void ParseLine_handles_chords_and_pauses()
    {
        IReadOnlyList<PianoToken> line = PianoSongParser.ParseLine("A-[fu]-G");
        Assert.Equal(PianoTokenKind.Note, line[0].Kind);
        Assert.Equal(PianoTokenKind.Pause, line[1].Kind);
        Assert.Equal(PianoTokenKind.Chord, line[2].Kind);
        Assert.NotNull(line[2].ChordKeys);
        Assert.Equal(["f", "u"], line[2].ChordKeys);
        Assert.Equal(PianoTokenKind.Pause, line[3].Kind);
        Assert.Equal(PianoTokenKind.Note, line[4].Kind);
    }

    [Fact]
    public void ParseHeaderSettings_reads_autoplayer_directives()
    {
        const string text = "# autoplayer: note_ms=45, lowercase=false\nA A";

        IReadOnlyDictionary<string, string> header = PianoSongParser.ParseHeaderSettings(text);
        Assert.Equal("45", header["note_ms"]);
        Assert.Equal("false", header["lowercase"]);
    }
}

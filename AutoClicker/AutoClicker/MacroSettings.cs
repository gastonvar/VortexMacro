using System.Text.Json;

namespace AutoClicker;

internal sealed class MacroSettings
{
    public int ToggleHotkeyVirtualKey { get; set; } = 0x26; // Up arrow
    public int CycleDelayMs { get; set; } = 500;
    public int MoveDelayMs { get; set; } = 100;
    public int VortexDelayMs { get; set; } = 4000;
    public int NexusAfterFirstClickDelayMs { get; set; } = 1000;
    public int NexusAfterSecondClickDelayMs { get; set; } = 1000;
    public int NexusDelayMs { get; set; } = 8000;
    public int ClearGoogleDelayMs { get; set; } = 3000;
    public int ClearGoogleEveryCycles { get; set; } = 10;
    public int ScrollDownSteps { get; set; } = 100;
    public int ScrollUpSteps { get; set; } = 3;

    public int VortexXStart { get; set; } = 983;
    public int VortexXEnd { get; set; } = 1242;
    public int VortexYStart { get; set; } = 397;
    public int VortexYEnd { get; set; } = 428;

    public int NexusXStart { get; set; } = -1293;
    public int NexusXEnd { get; set; } = -1010;
    public int NexusYStart { get; set; } = 555;
    public int NexusYEnd { get; set; } = 583;

    public int NexusLowerYStart { get; set; } = 470;
    public int NexusLowerYEnd { get; set; } = 500;

    public int NexusLowerPlusYStart { get; set; } = 738;
    public int NexusLowerPlusYEnd { get; set; } = 764;

    public int CloseNexusReminderX { get; set; } = 714;
    public int CloseNexusReminderY { get; set; } = 450;

    public int NexusPageX { get; set; } = -182;
    public int NexusPageY { get; set; } = 390;

    public int CloseGoogleX { get; set; } = -22;
    public int CloseGoogleY { get; set; } = 22;

    public static MacroSettings CreateDefault() => new();

    public static MacroSettings Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return CreateDefault();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<MacroSettings>(json) ?? CreateDefault();
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

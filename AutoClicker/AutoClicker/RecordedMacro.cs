using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoClicker;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum RecordedEventType
{
    Delay,
    MouseMove,
    MouseClick,
    MouseScroll,
    KeyDown,
    KeyUp
}

internal sealed class RecordedMacro
{
    public string Name { get; set; } = "Untitled Macro";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<RecordedMacroEvent> Events { get; set; } = [];

    public static RecordedMacro Load(string filePath)
    {
        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<RecordedMacro>(json) ?? new RecordedMacro();
    }

    public void Save(string filePath)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(filePath, JsonSerializer.Serialize(this, options));
    }
}

internal sealed class RecordedMacroEvent
{
    public RecordedEventType Type { get; set; }
    public int DelayMs { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Button { get; set; } = "Left";
    public string Action { get; set; } = "Down";
    public int ScrollDelta { get; set; }
    public int VirtualKey { get; set; }

    public string Describe()
    {
        return Type switch
        {
            RecordedEventType.Delay => $"Wait {DelayMs} ms",
            RecordedEventType.MouseMove => $"Move to ({X}, {Y}) after {DelayMs} ms",
            RecordedEventType.MouseClick => $"{Button} {Action} at ({X}, {Y}) after {DelayMs} ms",
            RecordedEventType.MouseScroll => $"Scroll {ScrollDelta} at ({X}, {Y}) after {DelayMs} ms",
            RecordedEventType.KeyDown => $"Key down {FormatKey(VirtualKey)} after {DelayMs} ms",
            RecordedEventType.KeyUp => $"Key up {FormatKey(VirtualKey)} after {DelayMs} ms",
            _ => Type.ToString()
        };
    }

    private static string FormatKey(int virtualKey)
    {
        if (Enum.IsDefined(typeof(Keys), virtualKey))
        {
            return ((Keys)virtualKey).ToString();
        }

        return $"VK_{virtualKey:X}";
    }
}

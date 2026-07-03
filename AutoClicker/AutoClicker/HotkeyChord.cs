using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoClicker;

[JsonConverter(typeof(HotkeyChordJsonConverter))]
internal sealed class HotkeyChord
{
    public uint Modifiers { get; set; }
    public int VirtualKey { get; set; }

    public bool IsValid => VirtualKey != 0 && Enum.IsDefined(typeof(Keys), VirtualKey);

    public Keys Key => (Keys)VirtualKey;

    public static HotkeyChord FromKey(Keys key) => new() { VirtualKey = (int)key };

    public static HotkeyChord CtrlAlt(Keys key) => new()
    {
        Modifiers = NativeMethods.ModControl | NativeMethods.ModAlt,
        VirtualKey = (int)key
    };
}

internal sealed class HotkeyChordJsonConverter : JsonConverter<HotkeyChord>
{
    public override HotkeyChord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new HotkeyChord { VirtualKey = reader.GetInt32() };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return new HotkeyChord();
        }

        uint modifiers = 0;
        int virtualKey = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            string? propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Modifiers":
                    modifiers = reader.GetUInt32();
                    break;
                case "VirtualKey":
                case "Key":
                    virtualKey = reader.GetInt32();
                    break;
            }
        }

        return new HotkeyChord { Modifiers = modifiers, VirtualKey = virtualKey };
    }

    public override void Write(Utf8JsonWriter writer, HotkeyChord value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("Modifiers", value.Modifiers);
        writer.WriteNumber("VirtualKey", value.VirtualKey);
        writer.WriteEndObject();
    }
}

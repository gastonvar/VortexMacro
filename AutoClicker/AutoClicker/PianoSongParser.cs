using System.Text.RegularExpressions;

namespace AutoClicker;

internal enum PianoTokenKind
{
    Pause,
    Note,
    Chord
}

internal readonly record struct PianoToken(PianoTokenKind Kind, char NoteChar = default, int RepeatCount = 1, string[]? ChordKeys = null);

internal static class PianoSongParser
{
    private const string KeyChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:;'!@#$%^&*()_+-=,./<>?";

    private static readonly HashSet<char> KeyCharSet = KeyChars.ToHashSet();

    public static string SanitizeForNormalKeys(string text)
    {
        var lines = new List<string>();
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                lines.Add(line);
                continue;
            }

            line = Regex.Replace(line, @"\[([^\]]*)\]", match =>
            {
                string cleaned = new string(match.Groups[1].Value.Where(char.IsLetterOrDigit).ToArray());
                return cleaned.Length > 0 ? $"[{cleaned}]" : string.Empty;
            });
            line = new string(line.Where(c => char.IsLetterOrDigit(c) || c is '-' or '[' or ']').ToArray());
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static IReadOnlyDictionary<string, string> ParseHeaderSettings(string text)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (!line.StartsWith('#'))
            {
                break;
            }

            Match match = Regex.Match(line, @"#\s*autoplayer:\s*(.+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                continue;
            }

            foreach (string part in match.Groups[1].Value.Split(','))
            {
                string piece = part.Trim();
                int eq = piece.IndexOf('=');
                if (eq > 0)
                {
                    settings[piece[..eq].Trim().ToLowerInvariant()] = piece[(eq + 1)..].Trim().ToLowerInvariant();
                }
            }
        }

        return settings;
    }

    public static IReadOnlyList<IReadOnlyList<PianoToken>> ParseSong(string text)
    {
        var lines = new List<IReadOnlyList<PianoToken>>();
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            if (!Regex.IsMatch(line, @"[A-Za-z0-9:\[\]';\-\$%\^&@!()]"))
            {
                continue;
            }

            lines.Add(ParseLine(line));
        }

        if (lines.Count == 0)
        {
            throw new InvalidOperationException("No playable lines found in the file.");
        }

        return lines;
    }

    public static IReadOnlyList<PianoToken> ParseLine(string line)
    {
        var tokens = new List<PianoToken>();
        int i = 0;
        int n = line.Length;

        while (i < n)
        {
            char ch = line[i];

            if (ch is ' ' or '-')
            {
                tokens.Add(new PianoToken(PianoTokenKind.Pause));
                i++;
                continue;
            }

            if (ch == '*')
            {
                i++;
                continue;
            }

            if (ch == '[')
            {
                int end = line.IndexOf(']', i);
                if (end < 0)
                {
                    throw new InvalidOperationException($"Unclosed chord bracket near: {line[Math.Min(i, line.Length - 1)..Math.Min(i + 20, line.Length)]}");
                }

                string[] keys = line[(i + 1)..end].Where(c => !char.IsWhiteSpace(c)).Select(c => c.ToString()).ToArray();
                if (keys.Length == 0)
                {
                    throw new InvalidOperationException($"Empty chord near: {line[Math.Min(i, line.Length - 1)..Math.Min(i + 20, line.Length)]}");
                }

                tokens.Add(new PianoToken(PianoTokenKind.Chord, ChordKeys: keys));
                i = end + 1;
                continue;
            }

            if (KeyCharSet.Contains(ch))
            {
                int count = 1;
                while (i + count < n && line[i + count] == ch)
                {
                    count++;
                }

                tokens.Add(new PianoToken(PianoTokenKind.Note, ch, count));
                i += count;
                continue;
            }

            i++;
        }

        return tokens;
    }

    public static bool IsKeyChar(char c) => KeyCharSet.Contains(c);
}

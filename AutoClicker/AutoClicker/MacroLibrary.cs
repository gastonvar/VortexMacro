using System.Text.Json;

namespace AutoClicker;

internal sealed class MacroLibrary
{
    private readonly string _directory;

    public MacroLibrary(string? directory = null)
    {
        _directory = directory ?? SettingsPaths.MacroLibraryDirectory;
        Directory.CreateDirectory(_directory);
    }

    public IReadOnlyList<string> ListMacroNames()
    {
        return Directory.EnumerateFiles(_directory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string GetPath(string name) => Path.Combine(_directory, Sanitize(name) + ".json");

    public void Save(RecordedMacro macro)
    {
        macro.Save(GetPath(macro.Name));
    }

    public RecordedMacro Load(string name) => RecordedMacro.Load(GetPath(name));

    public void Delete(string name)
    {
        string path = GetPath(name);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public bool Exists(string name) => File.Exists(GetPath(name));

    private static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "Untitled" : name.Trim();
    }
}

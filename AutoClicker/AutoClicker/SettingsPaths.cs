namespace AutoClicker;

internal static class SettingsPaths
{
    private const string AppFolderName = "GasvarMacro";
    private const string SettingsFileName = "app-settings.json";

    public static string AppDataDirectory
    {
        get
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppFolderName);
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string AppSettingsFile => Path.Combine(AppDataDirectory, SettingsFileName);

    public static string MacroLibraryDirectory
    {
        get
        {
            string path = Path.Combine(AppDataDirectory, "macros");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string SongsDirectory => Path.Combine(AppContext.BaseDirectory, "songs");

    public static string PrepareSettingsFile()
    {
        string target = AppSettingsFile;
        string? bestLegacy = FindNewestLegacySettingsFile();

        if (!File.Exists(target))
        {
            if (bestLegacy != null)
            {
                CopySettings(bestLegacy, target);
            }

            return target;
        }

        if (bestLegacy != null && ShouldPreferLegacy(target, bestLegacy))
        {
            string backup = target + $".replaced.{DateTime.Now:yyyyMMddHHmmss}.bak";
            try
            {
                File.Copy(target, backup, overwrite: true);
            }
            catch
            {
                // Best effort.
            }

            CopySettings(bestLegacy, target);
        }

        return target;
    }

    public static string? TryResolveExistingSettingsFile(string preferredPath)
    {
        if (File.Exists(preferredPath))
        {
            return preferredPath;
        }

        return FindNewestLegacySettingsFile();
    }

    public static IEnumerable<string> GetLegacySettingsCandidates()
    {
        yield return Path.Combine(AppContext.BaseDirectory, SettingsFileName);
        yield return Path.Combine(AppContext.BaseDirectory, "macro-settings.json");

        string? exeDir = Path.GetDirectoryName(Application.ExecutablePath);
        if (!string.IsNullOrWhiteSpace(exeDir))
        {
            yield return Path.Combine(exeDir, SettingsFileName);
            yield return Path.Combine(exeDir, "macro-settings.json");
        }
    }

    private static string? FindNewestLegacySettingsFile()
    {
        return GetLegacySettingsCandidates()
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static bool ShouldPreferLegacy(string appDataPath, string legacyPath)
    {
        DateTime appDataTime = File.GetLastWriteTimeUtc(appDataPath);
        DateTime legacyTime = File.GetLastWriteTimeUtc(legacyPath);
        if (legacyTime > appDataTime.AddSeconds(5))
        {
            return true;
        }

        try
        {
            long appDataSize = new FileInfo(appDataPath).Length;
            long legacySize = new FileInfo(legacyPath).Length;
            return legacySize > appDataSize + 32;
        }
        catch
        {
            return false;
        }
    }

    private static void CopySettings(string source, string target)
    {
        string? directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(source, target, overwrite: true);
    }
}

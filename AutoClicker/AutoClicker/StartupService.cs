using Microsoft.Win32;

namespace AutoClicker;

internal static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "GasvarMacro";

    public static bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string;
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key == null)
        {
            return;
        }

        if (enabled)
        {
            string exe = Application.ExecutablePath;
            key.SetValue(ValueName, $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}

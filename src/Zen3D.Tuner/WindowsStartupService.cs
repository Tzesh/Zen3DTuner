using Microsoft.Win32;
using System.Runtime.Versioning;

namespace Zen3D.Tuner;

/// <summary>Opt-in normal-user GUI start at logon. Never escalates privileges.</summary>
internal static class WindowsStartupService
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Zen3DTuner";

    [SupportedOSPlatformGuard("windows")]
    public static bool IsAvailable => OperatingSystem.IsWindows();

    public static bool IsEnabled()
    {
        if (!IsAvailable) return false;
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);
        return key?.GetValue(ValueName) is string s && !string.IsNullOrWhiteSpace(s);
    }

    public static void SetEnabled(bool enabled)
    {
        if (!IsAvailable) throw new PlatformNotSupportedException("Windows only.");
        if (!enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath) ||
            !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileName(exePath), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Publish the Windows .exe before enabling launch at login.");

        using var writeKey = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
        if (writeKey is null) throw new InvalidOperationException("Cannot open current-user Run registry key.");
        writeKey.SetValue(ValueName, $"\"{exePath}\"", RegistryValueKind.String);
    }
}

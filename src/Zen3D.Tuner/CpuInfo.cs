using Microsoft.Win32;

namespace Zen3D.Tuner;

internal static class CpuInfo
{
    public static string Detect()
    {
        if (!OperatingSystem.IsWindows())
            return "macOS / other — UI preview mode";
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
        var name = key?.GetValue("ProcessorNameString") as string;
        return string.IsNullOrWhiteSpace(name) ? "Windows CPU (unidentified)" : name.Trim();
    }
}

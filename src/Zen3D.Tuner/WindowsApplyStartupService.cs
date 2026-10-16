using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Zen3D.Core;
using Zen3D.Hardware;

namespace Zen3D.Tuner;

[SupportedOSPlatform("windows")]
internal static class WindowsApplyStartupService
{
    private static string TaskName => "Zen3DTuner-Apply-" + ProtectedStorage.UserSid;

    public static void Enable(CurveProfile profile)
    {
        CurveProfileValidator.Validate(profile);
        WindowsHardwareBackend.Diagnose();
        string executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Zen3DTuner", "Zen3D.Tuner.exe");
        if (!string.Equals(Path.GetFullPath(Environment.ProcessPath ?? ""), executable, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Install the Windows build in Program Files\\Zen3DTuner before enabling automatic application. See docs/WINDOWS-TESTING.md.");
        var installation = Path.GetDirectoryName(executable)!;
        ProtectedStorage.AssertProtected(installation);
        // Validate all loaded private dependencies as well as the executable.
        void CheckTree(string directory)
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                ProtectedStorage.AssertProtected(entry);
                if (Directory.Exists(entry)) CheckTree(entry);
            }
        }
        CheckTree(installation);

        string profilePath = ProtectedStorage.StableProfilePath;
        if (File.Exists(profilePath)) ProtectedStorage.AssertProtected(profilePath);
        // Update only when the old task is disabled, so logon cannot apply a half-updated enrollment.
        if (IsEnabled()) RunScheduler("/Change", "/TN", TaskName, "/Disable");
        if (File.Exists(profilePath + ".tmp")) ProtectedStorage.AssertProtected(profilePath + ".tmp");
        new ProfileStore(profilePath).Save(profile.Clone());
        ProtectedStorage.ProtectNewFile(profilePath);
        ProtectedStorage.AssertProtected(profilePath);
        string xmlPath = Path.Combine(ProtectedStorage.EnsureDirectory(), $"task-{Guid.NewGuid():N}.xml");
        try
        {
            File.WriteAllText(xmlPath, StartupTaskDefinition.Create(executable, profilePath, ProtectedStorage.UserSid), Encoding.Unicode);
            RunScheduler("/Create", "/TN", TaskName, "/XML", xmlPath, "/F");
        }
        finally { if (File.Exists(xmlPath)) File.Delete(xmlPath); }
    }

    public static void Disable() => RunScheduler("/Delete", "/TN", TaskName, "/F");

    public static bool IsEnabled()
    {
        // Query failure is not used to authorize writes; registration/deletion still checks exit codes.
        return RunSchedulerCore(["/Query", "/TN", TaskName], false) == 0;
    }

    private static void RunScheduler(params string[] args) => RunSchedulerCore(args, true);

    private static int RunSchedulerCore(string[] args, bool failOnError)
    {
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Windows Task Scheduler.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Task Scheduler did not finish within 30 seconds.");
        }
        Task.WaitAll(stdout, stderr);
        if (failOnError && process.ExitCode != 0)
            throw new InvalidOperationException($"Task Scheduler failed ({process.ExitCode}): {stderr.Result} {stdout.Result}");
        return process.ExitCode;
    }
}

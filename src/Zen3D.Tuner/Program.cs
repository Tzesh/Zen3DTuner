using Avalonia;
using Zen3D.Core;
using Zen3D.Hardware;

namespace Zen3D.Tuner;

internal static class Program
{
    internal static string? NotificationMessage { get; private set; }
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length == 0) return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        string? logPath = null;
        bool notify = false;
        try
        {
            var command = HeadlessCommand.Parse(args)!;
            notify = command.Notify;
            CurveProfile? profile = command.ProfilePath is string path ? new ProfileStore(Path.GetFullPath(path)).LoadRequired() : null;
            if (command.Action == HeadlessAction.Preview)
            {
                Console.WriteLine(new PreviewOnlyBackend().Preview(profile!));
                return 0;
            }
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Hardware and startup commands require Windows. Use --preview-profile FILE --no-gui on macOS.");
            if (command.ExpectedUserSid is string sid && sid != ProtectedStorage.UserSid)
                throw new UnauthorizedAccessException("Use UAC consent with the same Windows administrator account; switching to another account is not supported.");
            // Elevated writes require a protected, writable audit log before native calls.
            logPath = ProtectedStorage.LogPath;
            if (File.Exists(logPath)) ProtectedStorage.AssertProtected(logPath);
            AppendLog(logPath, $"START {command.Action}; profile={command.ProfilePath ?? "none"}");
            ProtectedStorage.ProtectNewFile(logPath);
            if (profile is not null) AppendLog(logPath, "REQUEST " + System.Text.Json.JsonSerializer.Serialize(profile));
            using var singleApply = new FileStream(Path.Combine(ProtectedStorage.EnsureDirectory(), "hardware.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            string result;
            switch (command.Action)
            {
                case HeadlessAction.Apply:
                    result = new WindowsHardwareBackend().ApplyWithReport(profile!);
                    try { AppendLog(logPath, "SUCCESS HARDWARE_SNAPSHOT " + System.Text.Json.JsonSerializer.Serialize(WindowsHardwareBackend.ReadCurrent())); }
                    catch (Exception ex) { AppendLog(logPath, "READBACK_UNAVAILABLE " + ex.Message); }
                    break;
                case HeadlessAction.Diagnose:
                    result = WindowsHardwareBackend.Diagnose();
                    break;
                case HeadlessAction.ReadCurrent:
                    result = "HARDWARE_SNAPSHOT " + System.Text.Json.JsonSerializer.Serialize(WindowsHardwareBackend.ReadCurrent());
                    break;
                case HeadlessAction.StartupEnable:
                    WindowsApplyStartupService.Enable(profile!);
                    result = "Stable profile snapshot saved. Headless application enabled at this user's next Windows logon (30 second delay).";
                    break;
                case HeadlessAction.StartupDisable:
                    WindowsApplyStartupService.Disable();
                    result = "Automatic profile application disabled. Current CPU settings were not reset.";
                    break;
                default: throw new ArgumentException(HeadlessCommand.Usage);
            }
            AppendLog(logPath, "SUCCESS " + result);
            Console.WriteLine(result);
            singleApply.Dispose();
            if (notify) ShowNotification($"Profile '{profile!.Name}' applied. SMU commands acknowledged; stability is not verified.");
            return 0;
        }
        catch (Exception ex)
        {
            string message = "FAILED: " + ex.Message;
            Console.Error.WriteLine(message);
            if (logPath is not null)
            {
                try { AppendLog(logPath, message); }
                catch (Exception logError) { Console.Error.WriteLine("Could not write result log: " + logError.Message); }
            }
            if (notify) ShowNotification("Automatic profile application failed. Check the activity log. " + ex.Message);
            return 1;
        }
    }

    private static void AppendLog(string path, string message) =>
        File.AppendAllText(path, $"{DateTimeOffset.Now:O} {message.Replace('\r', ' ').Replace('\n', ' ')}{Environment.NewLine}");

    private static void ShowNotification(string message)
    {
        NotificationMessage = message;
        try { BuildAvaloniaApp().StartWithClassicDesktopLifetime([]); }
        catch (Exception ex) { Console.Error.WriteLine("Notification unavailable: " + ex.Message); }
    }

    private static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}

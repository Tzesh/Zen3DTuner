namespace Zen3D.Core;

public enum HeadlessAction { Preview, Apply, ReadCurrent, Diagnose, StartupEnable, StartupDisable }

public sealed record HeadlessCommand(HeadlessAction Action, string? ProfilePath = null, string? ExpectedUserSid = null, bool Notify = false)
{
    public const string Usage = "--preview-profile FILE --no-gui | --apply-profile FILE --no-gui | --diagnose | --startup-enable FILE --confirm-stable | --startup-disable";

    public static HeadlessCommand? Parse(string[] args)
    {
        if (args.Length == 0) return null;
        string? expectedUser = null;
        bool noGui = false, confirmed = false, notify = false;
        HeadlessCommand? result = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--expected-user" && expectedUser is null)
            {
                if (++i >= args.Length || !args[i].StartsWith("S-1-")) throw new ArgumentException("Expected Windows user SID is required.");
                expectedUser = args[i];
                continue;
            }
            if (args[i] == "--no-gui" && !noGui) { noGui = true; continue; }
            if (args[i] == "--notify" && !notify) { notify = true; continue; }
            if (args[i] == "--confirm-stable" && !confirmed) { confirmed = true; continue; }
            var action = args[i] switch
            {
                "--preview-profile" => HeadlessAction.Preview,
                "--apply-profile" => HeadlessAction.Apply,
                "--diagnose" => HeadlessAction.Diagnose,
                "--read-current" => HeadlessAction.ReadCurrent,
                "--startup-enable" => HeadlessAction.StartupEnable,
                "--startup-disable" => HeadlessAction.StartupDisable,
                _ => throw new ArgumentException("Unknown or repeated argument. " + Usage)
            };
            if (result is not null) throw new ArgumentException("Choose exactly one action. " + Usage);
            string? path = null;
            if (action is HeadlessAction.Preview or HeadlessAction.Apply or HeadlessAction.StartupEnable)
            {
                if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i]) || args[i].StartsWith("--"))
                    throw new ArgumentException("An explicit profile file is required. " + Usage);
                path = args[i];
            }
            result = new HeadlessCommand(action, path);
        }
        if (result is null || confirmed != (result.Action == HeadlessAction.StartupEnable))
            throw new ArgumentException("Startup requires --confirm-stable; this flag is only valid for startup enrollment. " + Usage);
        if (notify && result.Action != HeadlessAction.Apply) throw new ArgumentException("--notify is only valid for apply.");
        return result with { ExpectedUserSid = expectedUser, Notify = notify };
    }
}

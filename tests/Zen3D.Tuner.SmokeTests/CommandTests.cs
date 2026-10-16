using System.Xml.Linq;
using Zen3D.Core;

internal static class CommandTests
{
    public static void Run()
    {
        static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
        Check(HeadlessCommand.Parse([]) is null, "No args opens GUI");
        var cmd = HeadlessCommand.Parse(["--preview-profile", "/tmp/profile file.json", "--no-gui"]);
        Check(cmd?.Action == HeadlessAction.Preview && cmd.ProfilePath == "/tmp/profile file.json", "Preserve quoted path");
        foreach (var bad in new[] {
            new[] { "--no-gui" }, new[] { "--apply-profile" }, new[] { "--unknown" },
            new[] { "--apply-profile", "a", "--preview-profile", "b" },
            new[] { "--startup-enable", "a" }, new[] { "--apply-profile", "a", "--confirm-stable" } })
        {
            try { HeadlessCommand.Parse(bad); throw new Exception("Invalid CLI accepted"); }
            catch (ArgumentException) { }
        }
        Check(HeadlessCommand.Parse(["--startup-enable", "a", "--confirm-stable"])?.Action == HeadlessAction.StartupEnable, "Explicit stability confirmation required");
        var xml = StartupTaskDefinition.Create(@"C:\Program Files\Zen3DTuner\Zen3D.Tuner.exe", @"C:\ProgramData\Zen3DTuner\A&B\stable.json", "S-1-5-21-123");
        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        Check(doc.Descendants(ns + "LogonTrigger").Count() == 1, "Only logon trigger");
        Check(!doc.Descendants(ns + "BootTrigger").Any(), "No SYSTEM boot task");
        Check(doc.Descendants(ns + "RunLevel").Single().Value == "HighestAvailable", "Elevated task");
        Check(doc.Descendants(ns + "UserId").All(e => e.Value == "S-1-5-21-123"), "Bind task to current SID");
        Check(doc.Descendants(ns + "Arguments").Single().Value.Contains("A&B"), "XML escaping preserves path");
        Check(doc.Descendants(ns + "Arguments").Single().Value.Contains("--no-gui"), "Task must not open GUI");
        Check(!doc.Descendants(ns + "RestartOnFailure").Any(), "Never retry unstable hardware settings automatically");
    }
}

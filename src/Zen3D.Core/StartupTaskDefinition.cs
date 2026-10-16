using System.Xml.Linq;

namespace Zen3D.Core;

public static class StartupTaskDefinition
{
    public static string Create(string executable, string profilePath, string userSid)
    {
        if (new[] { executable, profilePath, userSid }.Any(s => string.IsNullOrWhiteSpace(s) || s.Contains('"') || s.Contains('\r') || s.Contains('\n')))
            throw new ArgumentException("Invalid task path or identity.");
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        XElement E(string name, object content) => new(ns + name, content);
        return new XElement(ns + "Task", new XAttribute("version", "1.4"),
            E("RegistrationInfo", E("Description", "Apply the user-confirmed Zen3D Tuner profile without opening the GUI.")),
            E("Triggers", E("LogonTrigger", new object[] { E("Enabled", "true"), E("UserId", userSid), E("Delay", "PT30S") })),
            E("Principals", new XElement(ns + "Principal", new XAttribute("id", "User"), E("UserId", userSid), E("LogonType", "InteractiveToken"), E("RunLevel", "HighestAvailable"))),
            E("Settings", new object[] {
                E("MultipleInstancesPolicy", "IgnoreNew"), E("DisallowStartIfOnBatteries", "false"), E("StopIfGoingOnBatteries", "false"),
                E("AllowHardTerminate", "true"), E("StartWhenAvailable", "false"), E("Enabled", "true"), E("Hidden", "false"), E("ExecutionTimeLimit", "PT2M") }),
            new XElement(ns + "Actions", new XAttribute("Context", "User"), E("Exec", new object[] {
                E("Command", executable), E("Arguments", $"--apply-profile \"{profilePath}\" --no-gui --notify"), E("WorkingDirectory", Path.GetDirectoryName(executable) ?? ".") }))).ToString();
    }
}


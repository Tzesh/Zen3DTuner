using System.Text.RegularExpressions;

namespace Zen3D.Core;

public enum PowerLimitKind { Ppt, Tdc, Edc }

public interface IProcessorSession : IDisposable
{
    string CpuName { get; }
    int Family { get; }
    int Model { get; }
    int PhysicalCores { get; }
    bool SetPowerLimit(PowerLimitKind kind, int value);
    bool SetCoreOffset(int core, int offset);
}

public static class ProcessorEligibility
{
    public static bool IsSupported(string name, int family, int model, int cores) =>
        family == 0x19 && model == 0x21 && cores == 8 &&
        Regex.IsMatch(name, @"\bRyzen 7 (5700X3D|5800X3D)\b", RegexOptions.CultureInvariant);
}

public static class HardwareApply
{
    public static string Apply(CurveProfile profile, IProcessorSession session)
    {
        CurveProfileValidator.Validate(profile);
        if (!ProcessorEligibility.IsSupported(session.CpuName, session.Family, session.Model, session.PhysicalCores))
            throw new NotSupportedException("Only 8-core Ryzen 7 5700X3D / 5800X3D (Vermeer) are supported.");

        int acknowledged = 0;
        void Write(Func<bool> action, string label)
        {
            try
            {
                if (!action()) throw new InvalidOperationException("SMU rejected the command.");
                acknowledged++;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"{label} failed after {acknowledged} acknowledged command(s). Settings may be partially applied; no further writes were attempted. Restart to restore firmware defaults before retrying. {ex.Message}", ex);
            }
        }

        // Apply requested limits before offsets; null deliberately produces no command.
        if (profile.PptWatts is int ppt) Write(() => session.SetPowerLimit(PowerLimitKind.Ppt, ppt), "PPT");
        if (profile.TdcAmps is int tdc) Write(() => session.SetPowerLimit(PowerLimitKind.Tdc, tdc), "TDC");
        if (profile.EdcAmps is int edc) Write(() => session.SetPowerLimit(PowerLimitKind.Edc, edc), "EDC");
        for (int core = 0; core < profile.Offsets.Length; core++)
        {
            int index = core;
            Write(() => session.SetCoreOffset(index, profile.Offsets[index]), $"Core {index}");
        }
        return $"{acknowledged} SMU commands acknowledged on {session.CpuName}. Physical readback and stability are not verified; check telemetry and stress-test before enabling startup.";
    }
}

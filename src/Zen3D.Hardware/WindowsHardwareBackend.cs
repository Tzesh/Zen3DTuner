using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using System.Text;
using Zen3D.Core;
using ZenStates.Core;
using ZenStates.Core.Hardware;

namespace Zen3D.Hardware;

public sealed class WindowsHardwareBackend : IHardwareBackend
{
    public string Name => "Experimental ZenStates-Core 1.91 / PawnIO";
    public bool IsHardwareApplySupported => OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64;
    public string Preview(CurveProfile profile) => new PreviewOnlyBackend().Preview(profile);

    public void Apply(CurveProfile profile) => ApplyWithReport(profile);

    public static HardwareSnapshot ReadCurrent()
    {
        EnsurePrerequisites();
        using var session = new NativeSession();
        return session.ReadCurrent();
    }

    public string ApplyWithReport(CurveProfile profile)
    {
        CurveProfileValidator.Validate(profile);
        EnsurePrerequisites();
        using var session = new NativeSession();
        // All tuning writes are inside HardwareApply, after CPU and profile checks.
        return HardwareApply.Apply(profile, session);
    }

    public static string Diagnose()
    {
        EnsurePrerequisites();
        using var session = new NativeSession();
        return $"CPU: {session.CpuName}; family/model {session.Family:X}/{session.Model:X}; cores {session.PhysicalCores}. PawnIO initialized; SMU version {session.SmuVersion}. No tuning commands issued.";
    }

    public static void EnsurePrerequisites()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException("Hardware tuning requires Windows x64. macOS remains preview-only.");
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new UnauthorizedAccessException("Run Zen3D Tuner as administrator for PawnIO access.");
        var (name, family, model) = ReadCpuIdentity();
        if (!ProcessorEligibility.IsSupported(name, family, model, 8))
            throw new NotSupportedException($"Unsupported CPU: {name} (family/model {family:X}/{model:X}).");
    }

    private static (string Name, int Family, int Model) ReadCpuIdentity()
    {
        if (!X86Base.IsSupported) throw new PlatformNotSupportedException("x86 CPUID is required.");
        var id = X86Base.CpuId(1, 0).Eax;
        int family = (id >> 8) & 0xF;
        int model = (id >> 4) & 0xF;
        if (family is 6 or 15) model |= (id >> 12) & 0xF0;
        if (family == 15) family += (id >> 20) & 0xFF;
        var brand = new byte[48];
        for (int i = 0; i < 3; i++)
        {
            var regs = X86Base.CpuId(unchecked((int)0x80000002) + i, 0);
            int[] values = [regs.Eax, regs.Ebx, regs.Ecx, regs.Edx];
            for (int j = 0; j < 4; j++) BitConverter.GetBytes(values[j]).CopyTo(brand, i * 16 + j * 4);
        }
        return (Encoding.ASCII.GetString(brand).Trim('\0', ' '), family, model);
    }

    private sealed class NativeSession : IProcessorSession
    {
        private readonly Cpu _cpu;
        public NativeSession()
        {
            var disabled = new CoreOptions.ModuleSettings { Enabled = false, Autorefresh = false };
            _cpu = new Cpu(new CoreOptions { IoModule = disabled, Timings = disabled, Aod = disabled, Wmi = disabled, Sensors = disabled });
            if (!ProcessorEligibility.IsSupported(CpuName, Family, Model, PhysicalCores))
            {
                _cpu.Dispose();
                throw new NotSupportedException("Driver CPU identification/topology does not match the supported Vermeer CPUs.");
            }
            if (!_cpu.SendTestMessage())
            {
                _cpu.Dispose();
                throw new InvalidOperationException("SMU did not respond to the read-only connection test.");
            }
        }
        public string CpuName => _cpu.info.cpuName;
        public int Family => (int)_cpu.info.family;
        public int Model => (int)_cpu.info.model;
        public int PhysicalCores => (int)_cpu.info.topology.physicalCores;
        public string SmuVersion => _cpu.smu.Version.ToString();
        public HardwareSnapshot ReadCurrent()
        {
            var cores = new CoreReading[8];
            for (int core = 0; core < cores.Length; core++)
            {
                try
                {
                    uint? raw = _cpu.GetPsmMarginSingleCore((uint)core, 0, 0);
                    int? value = HardwareSnapshot.DecodeOffset(raw);
                    cores[core] = new(core, raw, value, value is null ? "Firmware did not return a supported CO value." : null);
                }
                catch (Exception e) { cores[core] = new(core, null, null, e.Message); }
            }
            PowerReading power;
            try
            {
                var status = _cpu.RefreshPowerTable();
                power = status == SMU.Status.OK
                    ? HardwareSnapshot.DecodePower(_cpu.smu.TableVersion, _cpu.powerTable?.Table)
                    : new(null, null, null, _cpu.smu.TableVersion, "Power table refresh failed: " + status);
            }
            catch (Exception e) { power = new(null, null, null, _cpu.smu.TableVersion, e.Message); }
            return new(DateTimeOffset.Now, cores, power);
        }
        public bool SetCoreOffset(int core, int offset) => _cpu.SetPsmMarginSingleCore((uint)core, 0, 0, offset);
        public bool SetPowerLimit(PowerLimitKind kind, int value) => (kind switch
        {
            PowerLimitKind.Ppt => _cpu.SetPPTLimit((uint)value),
            PowerLimitKind.Tdc => _cpu.SetTDCVDDLimit((uint)value),
            PowerLimitKind.Edc => _cpu.SetEDCVDDLimit((uint)value),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        }) == SMU.Status.OK;
        public void Dispose() => _cpu.Dispose();
    }
}

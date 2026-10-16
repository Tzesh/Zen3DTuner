using Zen3D.Core;

internal static class HardwareTests
{
    public static void Run()
    {
        static void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
        var profile = new CurveProfile { PptWatts = 100, TdcAmps = 65, EdcAmps = 90 };
        using var session = new FakeSession();
        var result = HardwareApply.Apply(profile, session);
        Check(session.Calls.Count == 11, "Eight CO and three limits must be applied");
        Check(session.Calls.Take(3).SequenceEqual(new[] { "Ppt:100", "Tdc:65", "Edc:90" }), "Limits use W/A without double conversion");
        Check(result.Contains("acknowledged") && result.Contains("not"), "Do not claim proven stability/readback");
        using var unchanged = new FakeSession();
        HardwareApply.Apply(new CurveProfile(), unchanged);
        Check(unchanged.Calls.Count == 8, "Unset power limits must not be written");
        using var unsupported = new FakeSession { CpuName = "AMD Ryzen 9 5950X" };
        try { HardwareApply.Apply(profile, unsupported); throw new Exception("Unsupported CPU accepted"); }
        catch (NotSupportedException) { }
        Check(unsupported.Calls.Count == 0, "Unsupported CPU must have no writes");
        using var invalid = new FakeSession();
        try { HardwareApply.Apply(new CurveProfile { PptWatts = 999 }, invalid); throw new Exception("Invalid profile accepted"); }
        catch (ArgumentOutOfRangeException) { }
        Check(invalid.Calls.Count == 0, "Validation must precede writes");
        using var failed = new FakeSession { FailOnCall = 2 };
        try { HardwareApply.Apply(profile, failed); throw new Exception("Failed write reported success"); }
        catch (InvalidOperationException e) { Check(e.Message.Contains("partially"), "Partial failure must be explicit"); }
        Check(failed.Calls.Count == 2, "Stop immediately on failure; never retry writes");
        Check(!ProcessorEligibility.IsSupported("AMD Ryzen 7 5700X3D", 0x19, 0x21, 6), "Reject wrong core count");
        Check(!ProcessorEligibility.IsSupported("AMD Ryzen 7 5700X3D", 0x17, 0x21, 8), "Reject wrong architecture");
    }

    private sealed class FakeSession : IProcessorSession
    {
        public string CpuName { get; set; } = "AMD Ryzen 7 5700X3D 8-Core Processor";
        public int Family => 0x19;
        public int Model => 0x21;
        public int PhysicalCores => 8;
        public int FailOnCall { get; set; }
        public List<string> Calls { get; } = new();
        private bool Record(string call) { Calls.Add(call); return Calls.Count != FailOnCall; }
        public bool SetPowerLimit(PowerLimitKind kind, int value) => Record($"{kind}:{value}");
        public bool SetCoreOffset(int core, int offset) => Record($"C{core}:{offset}");
        public void Dispose() { }
    }
}

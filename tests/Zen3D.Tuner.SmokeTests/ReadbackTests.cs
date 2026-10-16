using System.Text.Json;
using Zen3D.Core;

internal static class ReadbackTests
{
    public static void Run()
    {
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        foreach (int offset in Enumerable.Range(-30, 61))
        {
            Check(HardwareSnapshot.DecodeOffset(unchecked((uint)offset)) == offset, "Signed CO decoding");
            Check(HardwareSnapshot.DecodeOffset(unchecked((ushort)offset)) == offset, "16-bit CO decoding");
        }
        foreach (uint? raw in new uint?[] { null, 31, 65505, 0x1234FFFB, 0x80000000 })
            Check(HardwareSnapshot.DecodeOffset(raw) is null, "Invalid CO must remain unavailable");
        var table = new float[372]; table[0] = 142; table[2] = 95; table[8] = 140;
        Check(HardwareSnapshot.DecodePower(0x380905, table).PptWatts == 142, "Read limits, not consumption");
        Check(HardwareSnapshot.DecodePower(0x999999, table).PptWatts is null, "Unknown table rejected");
        Check(HardwareSnapshot.DecodePower(0x380905, table[..20]).PptWatts is null, "Truncated table rejected");
        var snapshot = new HardwareSnapshot(DateTimeOffset.Now,
            Enumerable.Range(0, 8).Select(i => new CoreReading(i, 65531, -5, null)).ToArray(), HardwareSnapshot.DecodePower(0x380905, table));
        Check(snapshot.Complete && snapshot.ToProfile("Readback").Offsets.All(v => v == -5), "Import complete snapshot");
        var roundtrip = JsonSerializer.Deserialize<HardwareSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Check(roundtrip.Complete && roundtrip.Cores[0].Raw == 65531, "Preserve raw responses in JSON");
        Check(!(snapshot with { Cores = snapshot.Cores[..7] }).Complete, "Missing core rejected");
        var duplicate = snapshot.Cores.ToArray(); duplicate[7] = duplicate[0];
        Check(!(snapshot with { Cores = duplicate }).Complete, "Duplicate core rejected");
        table[0] = float.NaN;
        Check(HardwareSnapshot.DecodePower(0x380905, table).PptWatts is null, "NaN rejected");
        Check(HeadlessCommand.Parse(["--read-current", "--no-gui"])!.Action == HeadlessAction.ReadCurrent, "Read requires no saved profile");
        Check(HeadlessCommand.Parse(["--apply-profile", "x", "--notify"])!.Notify, "Startup notification parsed");
        var profile = new CurveProfile { ApplyOnLaunch = true };
        Check(profile.Clone().ApplyOnLaunch && JsonSerializer.Deserialize<CurveProfile>(JsonSerializer.Serialize(profile))!.ApplyOnLaunch, "Auto-apply persists and clones");
    }
}

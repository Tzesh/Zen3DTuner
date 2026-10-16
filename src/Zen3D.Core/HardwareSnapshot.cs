namespace Zen3D.Core;

public sealed record CoreReading(int Core, uint? Raw, int? Value, string? Error);
public sealed record PowerReading(double? PptWatts, double? TdcAmps, double? EdcAmps, uint TableVersion, string? Error);
public sealed record HardwareSnapshot(DateTimeOffset CapturedAt, CoreReading[] Cores, PowerReading Power)
{
    public bool Complete => Cores.Length == 8 && Cores.Select((c, i) => c.Core == i && c.Value is >= -30 and <= 30 && c.Error is null).All(x => x)
        && Power.Error is null && Power.PptWatts is > 0 && Power.TdcAmps is > 0 && Power.EdcAmps is > 0;

    public static int? DecodeOffset(uint? raw)
    {
        if (raw is null) return null;
        int signed = unchecked((int)raw.Value);
        if (signed is >= -30 and <= 30) return signed;
        if (raw <= ushort.MaxValue)
        {
            signed = unchecked((short)raw.Value);
            if (signed is >= -30 and <= 30) return signed;
        }
        return null;
    }

    // Vermeer layouts documented by ryzen_monitor/src/pm_tables.c. Unknown versions fail closed.
    public static PowerReading DecodePower(uint version, float[]? table)
    {
        if (version is not (0x380904 or 0x380905))
            return new(null, null, null, version, $"Unsupported PM table 0x{version:X}.");
        int minimum = version == 0x380904 ? 361 : 372;
        if (table is null || table.Length < minimum)
            return new(null, null, null, version, "PM table is missing or truncated.");
        double? Read(int i) => float.IsFinite(table[i]) && table[i] > 0 && table[i] <= 1000 ? table[i] : null;
        var ppt = Read(0); var tdc = Read(2); var edc = Read(8);
        return new(ppt, tdc, edc, version, ppt is null || tdc is null || edc is null ? "Invalid power-limit telemetry." : null);
    }

    public CurveProfile ToProfile(string name)
    {
        if (!Complete) throw new InvalidOperationException("All eight CO values and power limits must be available before importing current settings.");
        static int Whole(double? value)
        {
            if (value is null || Math.Abs(value.Value - Math.Round(value.Value)) > 0.01)
                throw new InvalidOperationException("Current limits are fractional; enter whole-number settings explicitly.");
            return checked((int)Math.Round(value.Value));
        }
        var profile = new CurveProfile { Name = name, Offsets = Cores.Select(c => c.Value!.Value).ToArray(),
            PptWatts = Whole(Power.PptWatts), TdcAmps = Whole(Power.TdcAmps), EdcAmps = Whole(Power.EdcAmps) };
        CurveProfileValidator.Validate(profile);
        return profile;
    }
}

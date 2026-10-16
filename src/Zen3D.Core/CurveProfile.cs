namespace Zen3D.Core;

/// <summary>Curve Optimizer steps, not millivolts. v0.1 supports one 8-core CCD.</summary>
public sealed class CurveProfile
{
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "Balanced";
    public int[] Offsets { get; set; } = new int[8];

    public int? PptWatts { get; set; }
    public int? TdcAmps { get; set; }
    public int? EdcAmps { get; set; }
    public bool ApplyOnLaunch { get; set; }

    public CurveProfile Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        Name = Name,
        Offsets = (int[])Offsets.Clone(),
        PptWatts = PptWatts,
        TdcAmps = TdcAmps,
        EdcAmps = EdcAmps,
        ApplyOnLaunch = ApplyOnLaunch
    };
}

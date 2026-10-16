namespace Zen3D.Core;

public interface IHardwareBackend
{
    string Name { get; }
    bool IsHardwareApplySupported { get; }
    string Preview(CurveProfile profile);
    /// <summary>Must fail closed unless verified safe and supported on real hardware.</summary>
    void Apply(CurveProfile profile);
}

/// <summary>This backend never initializes a driver or issues hardware writes.</summary>
public sealed class PreviewOnlyBackend : IHardwareBackend
{
    public string Name => "Preview only (hardware control not connected)";
    public bool IsHardwareApplySupported => false;

    public string Preview(CurveProfile profile)
    {
        CurveProfileValidator.Validate(profile);
        return "DRY RUN — no CPU changes. Core offsets: " +
               string.Join("  ", profile.Offsets.Select((v, core) => $"C{core}:{v}")) +
               $" | PPT={Format(profile.PptWatts, "W")}  TDC={Format(profile.TdcAmps, "A")}  EDC={Format(profile.EdcAmps, "A")}";
    }

    private static string Format(int? value, string unit) => value.HasValue ? $"{value} {unit}" : "unchanged";

    public void Apply(CurveProfile profile) =>
        throw new NotSupportedException("PreviewOnlyBackend cannot apply hardware settings.");
}

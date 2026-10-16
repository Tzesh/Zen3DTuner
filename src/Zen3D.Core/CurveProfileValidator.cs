namespace Zen3D.Core;

public static class CurveProfileValidator
{
    public static void Validate(CurveProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.SchemaVersion != 1)
            throw new ArgumentException("Unsupported profile schema version.");
        if (string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 64)
            throw new ArgumentException("Profile name must have 1-64 characters.");
        if (profile.Offsets is not { Length: 8 })
            throw new ArgumentException("A Zen 3 Ryzen 7 X3D profile needs exactly 8 offsets.");
        // Application input caps; these do not guarantee hardware stability.
        ValidateLimit(profile.PptWatts, 142, "PPT (W)");
        ValidateLimit(profile.TdcAmps, 95, "TDC (A)");
        ValidateLimit(profile.EdcAmps, 140, "EDC (A)");
        for (int i = 0; i < profile.Offsets.Length; i++)
            if (profile.Offsets[i] < -30 || profile.Offsets[i] > 0)
                throw new ArgumentOutOfRangeException(nameof(profile),
                    $"Core {i}: value must be in [-30, 0] CO steps.");
    }

    private static void ValidateLimit(int? value, int maximum, string name)
    {
        if (value.HasValue && (value.Value < 1 || value.Value > maximum))
            throw new ArgumentOutOfRangeException(name, $"Use 1–{maximum}, or leave blank to keep the existing limit.");
    }
}

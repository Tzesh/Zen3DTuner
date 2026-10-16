using System.Text.Json;

namespace Zen3D.Core;

public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        MaxDepth = 16
    };

    public string FilePath { get; }

    public ProfileStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Zen3DTuner", "profile.json");
    }

    public CurveProfile Load()
    {
        if (!File.Exists(FilePath)) return new CurveProfile();
        return LoadRequired();
    }

    public CurveProfile LoadRequired()
    {
        using var stream = File.OpenRead(FilePath);
        if (stream.Length > 64 * 1024) throw new InvalidDataException("Profile exceeds the 64 KiB limit.");
        var profile = JsonSerializer.Deserialize<CurveProfile>(stream)
            ?? throw new InvalidDataException("Profile JSON is empty.");
        CurveProfileValidator.Validate(profile);
        return profile;
    }

    public void Save(CurveProfile profile)
    {
        CurveProfileValidator.Validate(profile);
        var parent = Path.GetDirectoryName(Path.GetFullPath(FilePath))!;
        Directory.CreateDirectory(parent);
        var temp = FilePath + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(profile, JsonOptions));
            File.Move(temp, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}

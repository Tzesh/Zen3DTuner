using Zen3D.Core;

internal static class SnapshotTests
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "zen3d-snapshot-" + Guid.NewGuid());
        try
        {
            var editor = new ProfileStore(Path.Combine(directory, "editor.json"));
            var startup = new ProfileStore(Path.Combine(directory, "stable.json"));
            var profile = new CurveProfile { PptWatts = 100, TdcAmps = 65, EdcAmps = 90 };
            startup.Save(profile.Clone());
            profile.PptWatts = 120;
            profile.Offsets[0] = -20;
            editor.Save(profile);
            var snapshot = startup.LoadRequired();
            if (snapshot.PptWatts != 100 || snapshot.Offsets[0] != 0)
                throw new Exception("Editing the normal profile changed the stable startup snapshot");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    }
}

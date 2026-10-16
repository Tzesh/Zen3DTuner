using Zen3D.Core;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception("Smoke test failed: " + message);
}

var zero = new CurveProfile();
CurveProfileValidator.Validate(zero);
Check(zero.Offsets.SequenceEqual(new int[8]), "Defaults should be zero");
var safe = zero.Clone();
safe.Offsets[0] = -10;
Check(zero.Offsets[0] == 0, "Clone must be deep");
var backend = new PreviewOnlyBackend();
Check(!backend.IsHardwareApplySupported, "Preview backend must disallow writes");
Check(backend.Preview(safe).Contains("C0:-10"), "Preview should show offset");
try { backend.Apply(safe); throw new Exception("Apply unexpectedly succeeded"); }
catch (NotSupportedException) { }
var bad = safe.Clone();
bad.Offsets[0] = -31;
try { CurveProfileValidator.Validate(bad); throw new Exception("Invalid offset accepted"); }
catch (ArgumentOutOfRangeException) { }
var path = Path.Combine(Path.GetTempPath(), "zen3d-test-" + Guid.NewGuid() + ".json");
try
{
    var store = new ProfileStore(path);
    store.Save(safe);
    Check(store.Load().Offsets.SequenceEqual(safe.Offsets), "Profile round-trip");
}
finally { if (File.Exists(path)) File.Delete(path); }
var oldJson = System.Text.Json.JsonSerializer.Deserialize<CurveProfile>("""{"SchemaVersion":1,"Name":"Old","Offsets":[0,0,0,0,0,0,0,0]}""")!;
Check(oldJson.PptWatts is null && oldJson.TdcAmps is null && oldJson.EdcAmps is null, "Old profiles leave power unchanged");
var limits = new CurveProfile { PptWatts = 100, TdcAmps = 65, EdcAmps = 90 };
CurveProfileValidator.Validate(limits);
Check(limits.Clone().PptWatts == 100 && limits.Clone().TdcAmps == 65 && limits.Clone().EdcAmps == 90, "Clone preserves limits");
Check(backend.Preview(limits).Contains("PPT=100 W") && backend.Preview(limits).Contains("TDC=65 A") && backend.Preview(limits).Contains("EDC=90 A"), "Preview labels units");
foreach (var invalid in new[] {
    new CurveProfile { PptWatts = 0 }, new CurveProfile { PptWatts = 143 },
    new CurveProfile { TdcAmps = -1 }, new CurveProfile { TdcAmps = 96 },
    new CurveProfile { EdcAmps = 141 } })
{
    try { CurveProfileValidator.Validate(invalid); throw new Exception("Invalid power limit accepted"); }
    catch (ArgumentOutOfRangeException) { }
}
var limitPath = Path.Combine(Path.GetTempPath(), "zen3d-limits-" + Guid.NewGuid() + ".json");
try
{
    var store = new ProfileStore(limitPath);
    try { store.LoadRequired(); throw new Exception("Missing explicit profile accepted"); }
    catch (FileNotFoundException) { }
    store.Save(limits);
    var loaded = store.LoadRequired();
    Check(loaded.PptWatts == 100 && loaded.TdcAmps == 65 && loaded.EdcAmps == 90, "Power limits round-trip");
}
finally { if (File.Exists(limitPath)) File.Delete(limitPath); }
HardwareTests.Run();
ReadbackTests.Run();
CommandTests.Run();
SnapshotTests.Run();
var oversizedPath = Path.Combine(Path.GetTempPath(), "zen3d-large-" + Guid.NewGuid() + ".json");
try
{
    File.WriteAllText(oversizedPath, new string(' ', 70_000));
    try { new ProfileStore(oversizedPath).LoadRequired(); throw new Exception("Oversized profile accepted"); }
    catch (InvalidDataException) { }
}
finally { File.Delete(oversizedPath); }
Console.WriteLine("All Zen3D Tuner smoke tests passed (no hardware writes).");

# Contributing

Bug reports, documentation improvements, and focused pull requests are welcome.

Use the .NET 10 SDK and run both test programs before submitting a change:

```sh
dotnet run --project tests/Zen3D.Tuner.SmokeTests -c Release
dotnet run --project tests/Zen3D.Tuner.UiTests -c Release
dotnet build src/Zen3D.Tuner -c Release
```

The tests use temporary profiles and never issue hardware writes. Do not add hardware operations to CI or test constructors. Keep profile validation and telemetry decoding in Zen3D.Core, native access in Zen3D.Hardware, and desktop behavior in Zen3D.Tuner.

For hardware reports include the CPU model, Windows version, motherboard, BIOS/AGESA, PawnIO version, PM table version, and relevant result lines. Remove account SIDs, personal paths, and unrelated log content before posting. Distinguish command acknowledgement, firmware readback, independent telemetry, and stability testing.

Support for another CPU or PM table requires evidence for its topology, message IDs, units, and layout. Unsupported responses must remain unavailable. Do not silently clamp telemetry or substitute a saved profile for live measurements.

Original application contributions use GPL-3.0-or-later. Retain third-party notices and document changes to vendored code in its ZEN3D-CHANGES.md file.

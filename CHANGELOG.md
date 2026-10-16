# Changelog

## 0.3.0 — 2026-10-16

- Read all eight Curve Optimizer offsets and current PPT/TDC/EDC limits when the app opens on Windows, before optional automatic application.
- Display firmware readings separately from requested profile values, with a timestamp and PM table version.
- Import complete current settings into the editor and save changes locally. A first launch without a saved profile initializes from readable, supported current settings.
- Add opt-in saved-profile application on app launch, with explicit confirmation when saving automatic settings.
- Show a desktop toast after application succeeds, fails, or is cancelled. The logon task also displays a toast without opening the editor.
- Add read-only `--read-current --no-gui` and optional `--notify` for apply commands.
- Decode signed 32-bit and zero-extended signed 16-bit CO replies; reject unsupported replies and power tables instead of substituting zeros or guessed defaults.
- Preserve saved profiles during refresh and manual application; retain isolated stable snapshots for scheduled logon application.
- Add readback and UI regression coverage, explicit NuGet configuration, Windows release packaging, and English documentation.

Validation: Release build, smoke tests, and headless UI tests pass on Windows. A read-only test on a Ryzen 7 5700X3D returned all eight CO values at -5 and PPT/TDC/EDC at 125 W / 95 A / 120 A (PM table 0x380905). These are observations, not recommended settings. The 5800X3D, reboot/logon execution, and long-term stability remain unverified.

## 0.2.1

- Keep Activity visible and distinguish this session's apply result from saved settings.
- Disable editing while the elevated helper runs and report only new log entries.

## 0.2.0

- Add experimental Windows tuning through PawnIO and ZenStates-Core.
- Add optional PPT/TDC/EDC limits and protected scheduled startup snapshots.

## 0.1.0

- Initial cross-platform profile editor and command preview.

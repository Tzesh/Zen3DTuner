# Windows installation, testing, and recovery

## Portable use

Extract the Windows release ZIP and run Zen3D.Tuner.exe. The self-contained release includes .NET; PawnIO must be installed separately from its official project. Opening the app attempts read-only CO and power-limit capture with same-account UAC consent. Cancelling leaves readings unavailable and the editor usable.

Save stores the editor profile locally. Apply sends the current editor values to an elevated helper via a temporary profile, then removes that temporary file. Apply does not change the saved auto-apply preference. Blank power values are skipped. Readback values and requested profile values are distinct.

## Protected installation for logon application

From an administrator PowerShell using the same Windows account:

```powershell
.\Install-Windows.ps1 -BuildDirectory .
& "$env:ProgramFiles\Zen3DTuner\Zen3D.Tuner.exe"
```

When building from source, the installer is at tools/Install-Windows.ps1; pass the published output directory as BuildDirectory. The installer refuses to overwrite an existing installation. Disable its startup task and back up or rename the old installation explicitly before installing an update. The installer does not install a driver, change CPU settings, or enable automatic application.

After testing your settings, enable hardware writes, tick the tested-settings confirmation, and choose **Save stable snapshot + enable at logon**. This captures a separate protected profile and registers a task for the current user, running once 30 seconds after logon. It requests highest available privileges through an interactive-token task and stores no password. A desktop toast reports the result. No editor opens, and no retry is scheduled.

For the separate app-open mode, select **Apply saved profile when app opens** and Save with the same confirmation. This mode uses the normal saved profile and UAC. Choose one automatic mode to avoid duplicate application. No automatic option is enabled by default.

## Manual validation

1. Inspect the startup readings, timestamp, and any unavailable values. Read-current does not require enabling hardware writes.
2. Edit and save your desired values. CO uses steps, PPT watts, and TDC/EDC amps. Input caps are not stability recommendations.
3. Preview, then Apply when ready. Acknowledgement is distinct from actual firmware readback.
4. Refresh current CPU settings. Compare against the requested settings and independent telemetry. Test workloads, idle behavior, and your own stability criteria before automatic application.
5. Test logon behavior only after enrollment is explicitly intended. Wake/resume is not supported.

A 2026-10-16 read-only run on a Ryzen 7 5700X3D successfully returned eight -5 offsets and 125 W / 95 A / 120 A using table 0x380905. Release build, smoke tests, and headless UI tests passed. Logon execution after reboot, 5800X3D, and long-term stability remain unverified.

## Logs and command line

From an administrator PowerShell:

```powershell
$exe = Join-Path $env:ProgramFiles 'Zen3DTuner\Zen3D.Tuner.exe'
Start-Process -FilePath $exe -ArgumentList '--read-current','--no-gui' -Wait
$sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
Get-Content -LiteralPath "$env:ProgramData\Zen3DTuner\apply-$sid.log" -Tail 5
```

A read operation can exit successfully with partial data: inspect Complete, per-core errors, raw responses, and the power error. Unknown values are never treated as zero. The UI shows command failures and cancellation separately from success. A partial write is not automatically rolled back.

## Disable and recover

- App-open auto-apply: uncheck **Apply saved profile when app opens** and Save. If you cannot use the UI, set `ApplyOnLaunch` to `false` in `%APPDATA%\Zen3DTuner\profile.json` before reopening.
- Logon snapshot: choose **Disable automatic application**, or remove its task from an administrator PowerShell:

```powershell
$sid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
schtasks.exe /Delete /TN "Zen3DTuner-Apply-$sid" /F
```

- GUI launch at login: uncheck **Launch GUI at Windows login**.

Disabling automation does not reset already applied CPU settings. Restart to return to the BIOS-configured baseline, which is not necessarily factory defaults. If unstable settings prevent normal login, use Windows Safe Mode to disable the task and the saved ApplyOnLaunch preference before restarting. Keep other tuning utilities from reapplying settings while diagnosing.

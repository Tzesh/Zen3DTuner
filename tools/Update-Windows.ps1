# Update an existing protected installation, retaining the previous directory for recovery.
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$BuildDirectory)
$ErrorActionPreference='Stop'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
if(-not ([Security.Principal.WindowsPrincipal]::new($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run this updater as administrator.' }
$source=(Resolve-Path -LiteralPath $BuildDirectory).Path
if(-not (Test-Path -LiteralPath (Join-Path $source 'Zen3D.Tuner.exe'))) { throw 'Select a published Windows build.' }
$programRoot=[IO.Path]::GetFullPath($env:ProgramFiles)
$destination=Join-Path $programRoot 'Zen3DTuner'
if(-not (Test-Path -LiteralPath $destination)) { & (Join-Path $PSScriptRoot 'Install-Windows.ps1') -BuildDirectory $source; exit }
if((Get-Item -LiteralPath $destination -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Installation cannot be a link or junction.' }
if($source.StartsWith($destination,[StringComparison]::OrdinalIgnoreCase)) { throw 'Extract the new version outside the installation folder.' }
$executable=Join-Path $destination 'Zen3D.Tuner.exe'
if(Get-Process -Name 'Zen3D.Tuner' -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $executable }) { throw 'Close the installed Zen3D Tuner before updating.' }
$taskName='Zen3DTuner-Apply-'+$identity.User.Value
$task=Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if($task -and $task.State -eq 'Running') { throw 'Wait for the current scheduled apply to finish before updating.' }
$wasEnabled=$task -and $task.State -ne 'Disabled'
$backup=$destination+'.backup-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8)
# Both move targets must remain direct children of the intended Program Files folder.
foreach($target in @($destination,$backup)) {
 if([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($target)) -ne $programRoot) { throw 'Unexpected installation target.' }
}
$restoreTask=$true
if($wasEnabled) { Disable-ScheduledTask -TaskName $taskName | Out-Null }
try {
 if($wasEnabled -and (Get-ScheduledTask -TaskName $taskName).State -eq 'Running') { throw 'A scheduled apply started; wait for it to finish.' }
 Move-Item -LiteralPath $destination -Destination $backup
 $restoreTask=$false
 try {
  & (Join-Path $PSScriptRoot 'Install-Windows.ps1') -BuildDirectory $source
  $restoreTask=$true
 } catch {
  if(Test-Path -LiteralPath $destination) { Move-Item -LiteralPath $destination -Destination ($backup+'.failed-update') }
  Move-Item -LiteralPath $backup -Destination $destination
  $restoreTask=$true
  throw
 }
} finally {
 if($wasEnabled -and $restoreTask) { Enable-ScheduledTask -TaskName $taskName | Out-Null }
}
Write-Host "Updated installation. Previous version retained at $backup. Saved profiles and startup preferences were preserved."


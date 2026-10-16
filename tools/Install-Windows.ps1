# Run manually in an administrator PowerShell after building for win-x64.
# Never installs a driver or enrolls a startup task.
[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$BuildDirectory)
$ErrorActionPreference = 'Stop'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Open PowerShell as administrator using the same Windows account.'
}
if (-not [Environment]::Is64BitOperatingSystem -or $env:PROCESSOR_ARCHITECTURE -ne 'AMD64') {
    throw 'This development build supports Windows x64 only.'
}
$source = (Resolve-Path -LiteralPath $BuildDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $source 'Zen3D.Tuner.exe') -PathType Leaf)) {
    throw 'Select the win-x64 build directory containing Zen3D.Tuner.exe and its dependencies.'
}
function Assert-NoLinks([string]$directory) {
    $item = Get-Item -LiteralPath $directory -Force
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Links/junctions are not allowed: $directory" }
    foreach ($child in Get-ChildItem -LiteralPath $directory -Force) {
        if ($child.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Links/junctions are not allowed: $($child.FullName)" }
        if ($child.PSIsContainer) { Assert-NoLinks $child.FullName }
    }
}
Assert-NoLinks $source
$destination = Join-Path $env:ProgramFiles 'Zen3DTuner'
if (Test-Path -LiteralPath $destination) {
    throw 'An installation already exists. Disable its startup task and back up/rename that installation explicitly before installing this preview. No files were overwritten.'
}
$admins = [Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
$system = [Security.Principal.SecurityIdentifier]::new('S-1-5-18')
$users = [Security.Principal.SecurityIdentifier]::new('S-1-5-32-545')
function Protect-Item([IO.FileSystemInfo]$item) {
    if ($item.PSIsContainer) { $acl = [Security.AccessControl.DirectorySecurity]::new() }
    else { $acl = [Security.AccessControl.FileSecurity]::new() }
    $acl.SetOwner($admins)
    $acl.SetAccessRuleProtection($true, $false)
    foreach ($sid in @($admins, $system, $users)) {
        if ($sid -eq $users) { $rights = [Security.AccessControl.FileSystemRights]::ReadAndExecute }
        else { $rights = [Security.AccessControl.FileSystemRights]::FullControl }
        if ($item.PSIsContainer) {
            $rule = [Security.AccessControl.FileSystemAccessRule]::new($sid, $rights, 'ContainerInherit,ObjectInherit', 'None', 'Allow')
        } else {
            $rule = [Security.AccessControl.FileSystemAccessRule]::new($sid, $rights, 'Allow')
        }
        $acl.AddAccessRule($rule)
    }
    Set-Acl -LiteralPath $item.FullName -AclObject $acl
}
$root = New-Item -Path $destination -ItemType Directory
Protect-Item $root
Get-ChildItem -LiteralPath $source -Force | Copy-Item -Destination $destination -Recurse
Get-ChildItem -LiteralPath $destination -Force -Recurse | ForEach-Object { Protect-Item $_ }
Write-Host "Installed to $destination. Startup is still disabled. Install PawnIO separately, then run --diagnose before any hardware test."

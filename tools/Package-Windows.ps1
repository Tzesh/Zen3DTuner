[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$version = '0.4.0'
$output = Join-Path $root "artifacts/Zen3DTuner-v$version-win-x64"
dotnet publish (Join-Path $root 'src/Zen3D.Tuner/Zen3D.Tuner.csproj') -c Release -r win-x64 --self-contained true -o $output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
foreach ($file in @('LICENSE', 'README.md', 'CHANGELOG.md')) {
    Copy-Item -LiteralPath (Join-Path $root $file) -Destination $output -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-Windows.ps1') -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Update-Windows.ps1') -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $root 'docs') -Destination $output -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'third_party/ZenStates-Core/ZEN3D-CHANGES.md') -Destination (Join-Path $output 'licenses') -Force
Copy-Item -LiteralPath (Join-Path $root 'examples') -Destination $output -Recurse -Force
$archive = "$output.zip"
Compress-Archive -Path (Join-Path $output '*') -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath "$archive.sha256" -Encoding ascii
Write-Host "Created $archive"




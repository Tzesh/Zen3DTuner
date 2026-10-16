# Upstream provenance and local changes
Source: https://github.com/irusanov/ZenStates-Core/tree/v1.91
Source archive SHA256: 13cab932cfc30a488eb3c1181bdbe02c37ac8a4f7c14f0af31b1f9e17b1be405
License: GPL-3.0, see LICENSE. All retained upstream C# source and embedded PawnIO modules are included here.
Local changes, 2026-10-16: replace legacy IODriver initialization in Cpu.cs with null; use net10.0 project and System.* 10.0.13 packages. Exclude upstream External legacy driver files, build hooks, IDE files, original project, Properties assembly metadata. No InpOut/WinIo/WinRing0 binaries are shipped or downloaded. PawnIO must be installed independently by the Windows user.

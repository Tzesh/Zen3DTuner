# Security boundaries for v0.2

macOS: preview and profile editing only. Hardware apply: Windows x64, administrator token, CPUID and driver identification restricted to eight-core Vermeer Ryzen 5700X3D/5800X3D. A matching identity is not a guarantee of BIOS/AGESA compatibility.

Hardware writes are explicit, use the pinned ZenStates-Core source and separately installed PawnIO, and reject invalid profiles before initialization. Legacy IO initialization is removed. There is no arbitrary external command template, shell interpolation or driver download. Direct process arguments and XML escaping are used for scheduled tasks.

Startup is opt-in: current user SID, interactive logon, highest privileges, fixed protected Program Files executable, admin/SYSTEM-owned dependencies and profile snapshot; no stored password or SYSTEM service. Normal editor profiles do not update the stable snapshot. Links/junctions and non-admin writable installation paths are rejected. ProgramData ACL checks fail closed if an untrusted preexisting directory/file is detected. Building/testing on Mac does not create tasks or modify Windows.

Logs are opened before hardware operations; one protected lock file serializes tuning helpers. A command error stops the sequence with a nonzero exit code and explicit partial-application notice. No blind automatic retry, no transactional rollback, no automatic claim of stability/readback. Task Scheduler retries are not configured. A 30 second logon delay allows recovery; the task can be deleted directly in Safe Mode.

Review/test gates remaining: target Windows ACL/UAC behavior, scheduler XML validation and registration, PawnIO compatibility, supported CPU/firmware command acceptance and independent telemetry, stability and recovery. See WINDOWS-TESTING.md. No Defender exception or disable-security instruction is part of setup.

App-open auto-apply is a separate opt-in preference in the normal user profile. It requests same-account UAC and uses the same validated helper as manual apply. Saving with auto-apply selected requires explicit tested-settings confirmation. Readback is read-only and uses the same CPU eligibility and protected operation lock. The logon helper's toast releases the hardware lock before displaying.

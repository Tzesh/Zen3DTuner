# Firmware readback

Readback opens the existing PawnIO/ZenStates-Core session after Windows, elevation, CPU identity, and topology checks. It uses the existing CO getter and power-table refresh path; it does not call tuning setters.

CO replies retain the raw unsigned value. Signed 32-bit and zero-extended signed 16-bit values in -30..30 are accepted. Unsupported replies and exceptions are shown as unavailable. The editor deliberately supports only -30..0; a positive reading is displayed but cannot be silently imported as zero.

PPT/TDC/EDC are taken from float indices 0, 2, and 8 of Vermeer PM table versions 0x380904 and 0x380905. These are limits in watts/amps, not current power consumption. The layout reference is [ryzen_monitor's PM table definitions](https://github.com/hattedsquirrel/ryzen_monitor/blob/master/src/pm_tables.c). Upstream describes the 0x380905 mapping as derived; our 5700X3D read-only check returned plausible values, but this does not validate every motherboard or BIOS. Other versions, undersized tables, non-finite values, non-positive values, and implausible values above 1000 are rejected. Readback bounds are separate from the editor's lower write caps.

Current firmware values are not factory defaults. A BIOS configuration, an earlier app session, or another tuning utility may already have changed them. Imported settings must fit the editor's ranges and whole-number format; otherwise enter requested settings explicitly. Reading never saves or applies a profile.

The protected log contains `SUCCESS HARDWARE_SNAPSHOT` followed by JSON, including timestamp, per-core raw values/errors, power values, table version, and completeness. A zero process exit code means the read routine completed; inspect `Complete` and errors to establish whether every value was available.

Successful CO readback confirms the firmware-reported margin. It does not measure physical voltage or establish stability.

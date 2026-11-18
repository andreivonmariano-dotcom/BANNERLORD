# Independent Bannerlord DLL Loader Probe

The separate CULoaderProbe module contains one extremely small .NET Framework 4.7.2 DLL compiled against the pinned Bannerlord 1.5.7 public reference assemblies. It uses only `MBSubModuleBase` and .NET file logging. It does not access Campaign objects, equipment, UI screens, families, saves, movement or custom settings.

## What this test distinguishes

- Probe missing from launcher: install path or manifest issue.
- Probe listed but `Cannot load CULoaderProbe.dll`: native runtime binding, file blocking or game version mismatch remains possible.
- Probe reaches the game's main menu and produces `%LOCALAPPDATA%\CalradiaUnified\loader-probe.log`: base Bannerlord DLL loading is functional. The full Calradia Unified v0.16 failure is narrowed to its richer assembly references or initialization.

## Safe procedure

1. Close Bannerlord. Disable the full CalradiaUnified module for this test.
2. Download the separate `CULoaderProbe-v0.1.0-diagnostic-only` artifact and extract **both ZIP layers** into the game root. This creates `Modules/CULoaderProbe/SubModule.xml` and `Modules/CULoaderProbe/bin/Win64_Shipping_Client/CULoaderProbe.dll`.
3. Right-click the new DLL and unblock it in Properties if Windows offers that checkbox.
4. Launch Bannerlord with only the official modules plus **CU Loader Probe - diagnostics only**. Do not load an important save.
5. If the game reaches its menu, inspect `%LOCALAPPDATA%\CalradiaUnified\loader-probe.log`. A log entry containing `CULoaderProbe.OnSubModuleLoad reached` confirms the managed entry point ran.

The probe does not prove the full mod works, and it must not be mistaken for a gameplay module. After testing, disable or remove CULoaderProbe.

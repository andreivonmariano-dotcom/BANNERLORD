# Calradia Unified v0.16.1: Native Sandbox character creation correction

**Target:** Bannerlord PC v1.5.7. **Gate B only until CI and actual launch tests.**

The previous v0.16 main-menu action **CU: Design Next Sandbox Character** was a mistaken separate creator. The user's game screenshots confirmed a broken Gauntlet layout and an inaccessible Close action. This build **removes that main-menu option and does not ship its broken XML prefab**.

**Use the standard Sandbox button and its native culture, appearance, background, age, naming, banner, review and Advanced Start flow.** The mod hooks `OnCharacterCreationInitializedEvent` to record the real narrative stage and link IDs in the local diagnostic journal, without changing the native stage order, actions, or Back/Next navigation. The native stage data will guide the subsequent integrated design.

**Current limitation:** Additional origin tabs for family, role, resources and equipment are **not yet inserted into the native character-creation sequence**. Our existing separate `sandbox-origin.ini` profile model remains in source for development. A preexisting active profile can still affect new-game resources under the v0.16 behavior, so keep it disabled until actual tests confirm intended results.

Set `Enabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini` to retain the untouched game start. Existing native settlement gameplay controls and diagnostic tools remain.

- [Native Sandbox flow design and acceptance](docs/V017_NATIVE_SANDBOX_FLOW.md)
- [Historical v0.16 origin profile details](docs/V016_SANDBOX_ORIGINS.md)
- [M0 actual game testing](docs/M0_TEST_PROCEDURE.md)

This is a **corrective safety build**, not a completed native Sandbox redesign. No copied DLLs, Harmony or UIExtenderEx dependency.

The discarded Sandbox Gauntlet prototype is explicitly excluded from the C# build. The new native Sandbox observation handler runs **after the engine finishes initializing its character-creation menu data**, and writes the real menu IDs without altering options or Back/Next. This is a controlled observation release, not the completed extended character-creation wizard.

# Calradia Unified v0.17.0 | Native Sandbox Starting Life candidate

**Target:** Bannerlord PC v1.5.7. **Gate B COMPILED_UNTESTED:** [Windows CI 44267754642](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44267754642) passed 562 independent policy checks and 14,600 simulated campaign days with zero compile errors and warnings. No native character-creation play test or family save/load acceptance yet. The broken separate main-menu screen remains removed.

The new native Sandbox wizard extension, real family hero creation, and actual kingdom-affiliation changes are each **experimental and independently disabled by default**. The existing `sandbox-origin.ini` remains the settings source at `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`.

New profile keys: `NativeStageEnabled`, `RealFamilyEnabled`, and `RoleAffiliationsEnabled`. Each starts false and requires explicit new-game opt-in. The regular standalone Sandbox experience is unchanged until then. Do not use active experiments with your only save.

See [the full feature matrix and in-game acceptance](docs/V017_EMBEDDED_SANDBOX_ACCEPTANCE.md).

## How to start the opt-in native wizard test

1. Use a disposable new Sandbox. Do not continue your primary save.
2. Create or edit `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini` with these lines:

```ini
Version=CU_ORIGIN_1
Enabled=false
NativeStageEnabled=true
RealFamilyEnabled=false
RoleAffiliationsEnabled=false
```

3. Start the normal **Sandbox** option. The character-creation age menu should lead into the six new Starting Life pages. Test Back and Next. Selecting a starting role sets `Enabled=true` for that new profile.
4. The **EXPERIMENTAL** family/affiliation pages have separate consent options. Keep both off until the native page navigation works and a game-test save is disposable.
5. The completed wizard and family-state changes still need live Bannerlord testing. The equipment kit and chosen town remain templates rather than automatic world changes.

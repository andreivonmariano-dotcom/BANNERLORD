# Calradia Unified v0.22.0 | Native FaceGen and Starting Fiefs

**Game target:** Mount & Blade II: Bannerlord PC v1.5.7.125802.  
**Release state:** **COMPILED_UNTESTED development prototype**. Source and reference-assembly compilation do not prove in-game appearance, castle ownership or save/reload.

## What's in the v0.22 prototype

**Q1: Real 3D appearance.** A new `CUFaceGeneratorStageView` derives from the game's actual `CharacterCreationFaceGeneratorView`. It uses Bannerlord's native BodyGenerator, face sliders, scene, cameras, character object, editing history and clean-up. On the CU Appearance page, `NativeStageEnabled` adds a passive Calradia Unified title when the real in-game face stage opens. The flag is **off by default**. If the native stage resolver does not pick up the custom view, the game retains its regular face editor and diagnostics will show the missing integration.

**Q2: Starting fief.** `StartingFiefMode` supports `Native`, `Castle`, `Town` or `Specific`. An exact `StartingFiefId` is required for the last mode. Any non-Native choice needs `AllowStartingFiefTransfer=true`, a Lord/Knight/King native starting role, a real Kingdom, and a same-kingdom non-besieged castle or town. The new-campaign phase-9 handoff uses native `ChangeOwnerOfSettlementAction.ApplyByGift` and verifies ownership afterward. **This can take an existing allied clan's settlement.** Consent is mandatory. The automatic Castle/Town modes pick a reproducible eligible settlement, preferring an already owned one. A named fief uses an exact engine ID, not a name-based suggestion.

**Q3: Native-style interface.** The CU pregame Gauntlet now shows eight sections: Identity, Appearance, Household, Progression, Equipment, Resources, Fief & World, Review. It includes a page counter, selected-tab bindings, native frame/button brushes and clearer Begin/Cancel actions.

## What is not finished

The creator is **not** yet a single, polished screen with its own integrated live 3D model. The current native culture/appearance/background/naming/review wizard remains after the initial CU form. Native BodyGenerator integration here is the first opt-in stage wrapper, not a complete replacement.

The Fief & World section does not yet have an interactive settlement map or browsable cards. An exact named fief requires its real internal settlement ID. Owner changes, spouse/child creation, role status and save/reload still need actual Bannerlord evidence. Never use an important existing save for these tests.

## Testing and traceability

- [v0.22 exact runtime acceptance test](docs/V022_FACE_FIEF_RUNTIME_ACCEPTANCE.md)
- [v0.21 engine API and final architecture](docs/V021_UNIFIED_CREATOR_ENGINE_MAP.md)
- [Issue #44: full native-looking creator](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44)
- [Issue #43: earlier Lord/family regression](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/43)
- [Current Windows build workflow](.github/workflows/bannerlord-build.yml)
- [Install compiled module script](scripts/Install-CU-From-Artifact.ps1)
- [Read-only game preflight](scripts/Run-CU-Runtime-Preflight.ps1)

No original TaleWorlds DLL is modified. The module remains independently removable. Set `OverhauledCreatorEnabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini` to restore the untouched native Sandbox start selector.

# Calradia Unified: current compatibility baseline

**Decision date:** 24 November 2026. **Status:** launcher profile written and CultureGear selectable per user report; gameplay acceptance pending.

Target: Bannerlord PC 1.5.7.125802, War Sails 1.3.7, RBM 4.8.2. This is the curated development and integration profile, not a new mandatory dependency manifest or a claim of conflict-free gameplay. Keep the standalone CU package and historical 78-mod reconstruction tracker separate from this snapshot. Pause replacement of systems already supplied by the active stack; revisit historical reconstruction only with an explicit scope decision.

## Current single-player launcher order and displayed versions

Source: user-supplied LauncherOrder-ApplyResult.json, 23 November 2026, 21:53:58 +08:00, status APPLIED_LAUNCHER_ONLY_GAMEPLAY_UNTESTED, 41 enabled entries, 244 recorded dependency edges. CultureGear activation was subsequently confirmed by the user after correcting the case-sensitive Sandbox dependency. Five current launcher screenshots supplied on 24 November 2026 confirm the same 41 entries in the same order and show their complete displayed versions. Internal IDs are carried from the earlier report. The screenshots do not prove successful runtime loading or resolve the launcher warning icons.

1. `Bannerlord.Harmony` — Harmony (v2.4.2.248)
2. `Bannerlord.ButterLib` — ButterLib (v2.12.0.0)
3. `Bannerlord.UIExtenderEx` — UIExtenderEx (v3.0.3.0)
4. `Bannerlord.MBOptionScreen` — Mod Configuration Menu v5 (v5.13.1.0)
5. `Native` — Native (v1.5.7.125802)
6. `SandBoxCore` — SandBox Core (v1.5.7.125802)
7. `Sandbox` — Sandbox (v1.5.7.125802)
8. `CustomBattle` — CustomBattle (v1.5.7.125802)
9. `StoryMode` — StoryMode (v1.5.7.125802)
10. `BirthAndDeath` — Birth and Aging Options (v1.5.7.125802)
11. `NavalDLC` — War Sails (v1.3.7.125802)
12. `ServeAsSoldier` — Serve as Soldier (v1.5.7.4)
13. `RTSCamera` — RTS Camera (v5.5.4.0)
14. `RTSCamera.CommandSystem` — RTS Camera Command System (v5.5.4.0)
15. `RBM` — (RBM) Realistic Battle Mod Bannerlord (v4.8.2.0)
16. `RBM_WS` — (RBM WS) Realistic Battle Mod War Sails Submod (v4.8.2.0)
17. `ImmersiveBattlefields` — Immersive Battlefields (v3.7.0.0)
18. `BannerFix` — Banner Fix Performance (v4.0.0.0)
19. `Vexillum` — Vexillum (e1.5.2.0)
20. `RaiseYourBanner` — Raise your Banner (v17.0.9.0)
21. `RealisticBlood` — Realistic Blood (v1.1.7.0)
22. `RaiseYourTorch` — Raise your Torch (v7.3.7.0)
23. `Bannerlord.Diplomacy` — Diplomacy (v1.5.7.0)
24. `ImprovedGarrisons` — Improved Garrisons (v4.3.1.0)
25. `Fourberie` — Fourberie (v1.5.7.0)
26. `ArenaOverhaulRedux` — Arena Overhaul Redux (v1.5.7.1)
27. `OpenSourceArmory` — Open Source Armory (v2.1.0.0)
28. `OpenSourceWeaponry` — Open Source Weaponry (v2.1.0.0)
29. `OpenSourceSaddlery` — Open Source Saddlery (v2.1.0.0)
30. `OpenSourceArmoryRBM` — Open Source Armory: RBM Patch (v2.1.0.0)
31. `OpenSourceWeaponryRBM` — Open Source Weaponry: RBM Patch (v2.1.0.0)
32. `OpenSourceSaddleryRBM` — Open Source Saddlery: RBM Patch (v2.1.0.0)
33. `BreakablePolearms` — Breakable Polearms (v1.1.2.0)
34. `ExpandedArmouryBL` — ExpandedArmouryBL (e1.5.2.0)
35. `ExpandedArmouryBL.FullFixRBM` — ExpandedArmouryBL: FullFix & RBM (v1.1.0.0)
36. `ZZZ_troop_overhaul` — Troop Overhaul by ZZZ (v1.0.1.0)
37. `NoblesBuyStuff` — Nobles Buy Stuff (v4.5.3.0)
38. `KingdomBorders` — Kingdom Borders (v1.5.7.0)
39. `CustomisableUI` — Customisable UI (v1.5.7.0)
40. `CalradiaUnified` — Calradia Unified (v0.38.0.0)
41. `EBA_OSA_ZZZ_CultureGear` — EBA OSA ZZZ Culture Gear (v1.1.0.0)

All 41 versions are launcher-display strings, including the v/e prefix and final component. Calradia Unified displays v0.38.0.0; this does not identify its exact compiled DLL. Verify the DLL hash against the candidate run before testing. Warning icons remain unassessed; inspect their actual tooltips and runtime evidence before making conflict or stability claims.

## Repair provenance and known gaps

- Q1 resource audit: 46 registered OSA-family resources passed static presence/XML checks, including 3 XSLT files.
- v5.34.4 Windows staging passed with 479 character records, 6 targeted Vlandian troops, 96 noble rosters, and 4 targeted noble templates. ApplyResult.json records FILES_APPLIED_GAMEPLAY_NOT_TESTED. FullFix selection: Standard Mod > Normal Names; separate ExpandedArmouryBL.FullFixRBM module. Do not use the v5.33 Replacer workflow.
- CultureGear manifest fix: exact dependency Id Sandbox (not SandBox); SandBoxCore remains spelled as shown. User confirmed module can be enabled. Keep all 15 dependencies; do not force-enable a rejected module.
- Game startup, live armor mesh fit, RBM/naval behavior, actual creator screens and save/reload are not certified by these reports. Four duplicate EBA item IDs were excluded from new catalog selections; do not change author stats arbitrarily.
- Preserve module files and backups; this documentation change does not install or redistribute any third-party asset.

## System ownership and overlap review

These are conservative design boundaries inferred from the installed module roles, not a completed source/API audit of third-party implementations. Verify exact patch owners, model replacements, events, XML/XSLT targets and versioned APIs before implementation.

### Combat, battlefield simulation and naval combat
Existing owners: RBM, RBM_WS, BreakablePolearms, ImmersiveBattlefields, RealisticBlood, RTSCamera and its CommandSystem.
Do not replace damage, armor penetration, weapon breakage, battle AI, formation command, camera or naval mission behavior without a specific interoperability design.

### Items, culture gear and troop progression
Existing owners: OSA trilogy and RBM patches, ExpandedArmouryBL, ExpandedArmouryBL.FullFixRBM, ZZZ_troop_overhaul, EBA_OSA_ZZZ_CultureGear, NoblesBuyStuff.
Keep item statistics, troop IDs and upgrade trees, armor rosters and noble purchasing under the existing owners. CU may read them, and use native operations for its own features. Do not assign equipment or overwrite purchasing rules in parallel.

### Politics, settlements and criminal play
Existing owners: Bannerlord.Diplomacy, ImprovedGarrisons, Fourberie, KingdomBorders.
Audit overlapping CU diplomacy, party orders, garrison training/recruitment and crime features before enabling. Prefer coordination with verified native APIs; avoid duplicate orders, transfers, recruitment, wages and state changes.

### Service, arenas and presentation
Existing owners: ServeAsSoldier, ArenaOverhaulRedux, CustomisableUI, BannerFix, Vexillum, RaiseYourBanner, RaiseYourTorch.
Preserve existing enlistment, arena reward/progression, HUD and banner/torch behavior. Audit CU starting roles and creator/UI hooks before enabling changes.

### Frameworks and official content
Existing owners: Harmony, ButterLib, UIExtenderEx, MCM, official modules and NavalDLC.
Treat installed frameworks as optional interoperability context, not automatic CU package dependencies. Do not assume native or framework APIs from names alone.

## Required development gate

1. Read this baseline and current CU feature registry/config/source. Check the exact installed or authorized source/API for any affected mod; inventory shared Harmony targets, event handlers, model overrides, XML IDs, UI hooks and save keys. Record UNKNOWN/BLOCKED for uninspected overlaps.
2. Classify each CU feature as independent, read-only integration, explicit optional adapter, or conflicting. Existing CU features are included in this review. Disable conflicting or unverified mutating features by default; do not globally disable another mod or change its assets. Do not duplicate side effects or rely on load order alone. Runtime detection must use exact installed module IDs and guard absent or unsupported versions.
3. Preserve standalone operation with official dependencies. Do not add hard requirements on the 41-module profile without explicit approval. Maintain a minimal official+CU test profile and this curated full-stack profile; isolate one feature flag per test and test with the external owner enabled.
4. Maintain the existing 21 November audit-first acceptance gate (PR #93), fix reproduced existing defects, then resume Starting Life integrity, coherent medieval social roles, and independent NPC life in that order. Focus on additive household/family continuity, personal obligations, persistent character identity and bounded decision-making; these are candidates, not proven gaps until audited.
5. Require exact pinned compilation, policy tests, clean-game and full-stack startup, native state/side-effect checks, controlled feature tests, and save/quit/reload before claiming compatibility. The existing Gate C–F requirements remain; no new feature expansion is approved by the launcher-only result. Record exact build, profile, flags, patch owners, test evidence and known conflicts. Update this snapshot when module versions/order change.

Canonical source handbook: https://docs.google.com/document/d/1A6AZpYpCyY6PoI1J6hn1rC2NQqnvPt9dsvZIZ0vwJpk/edit

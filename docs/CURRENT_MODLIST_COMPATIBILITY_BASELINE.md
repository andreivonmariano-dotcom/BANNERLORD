# Calradia Unified: current compatibility baseline

**Decision date:** 24 November 2026. **Status:** launcher profile written and CultureGear selectable per user report; gameplay acceptance pending.

Target: Bannerlord PC 1.5.7.125802, War Sails 1.3.7, RBM 4.8.2. This is the curated development and integration profile, not a new mandatory dependency manifest or a claim of conflict-free gameplay. Keep the standalone CU package and historical 78-mod reconstruction tracker separate from this snapshot. Pause replacement of systems already supplied by the active stack; revisit historical reconstruction only with an explicit scope decision.

## Exact saved single-player load order

Source: user-supplied LauncherOrder-ApplyResult.json, 23 November 2026, 21:53:58 +08:00, status APPLIED_LAUNCHER_ONLY_GAMEPLAY_UNTESTED, 41 enabled entries, 244 recorded dependency edges. CultureGear activation was subsequently confirmed by the user after correcting the case-sensitive Sandbox dependency. The saved order is a reference, not a fresh post-launch inventory.

1. `Bannerlord.Harmony`
2. `Bannerlord.ButterLib`
3. `Bannerlord.UIExtenderEx`
4. `Bannerlord.MBOptionScreen`
5. `Native` (v1.5.7, recorded manifest version)
6. `SandBoxCore` (v1.5.7, recorded manifest version)
7. `Sandbox` (v1.5.7, recorded manifest version)
8. `CustomBattle`
9. `StoryMode`
10. `BirthAndDeath`
11. `NavalDLC` (v1.3.7, recorded manifest version)
12. `ServeAsSoldier`
13. `RTSCamera`
14. `RTSCamera.CommandSystem`
15. `RBM` (v4.8.2, recorded manifest version)
16. `RBM_WS` (v4.8.2, recorded manifest version)
17. `ImmersiveBattlefields`
18. `BannerFix`
19. `Vexillum`
20. `RaiseYourBanner`
21. `RealisticBlood`
22. `RaiseYourTorch`
23. `Bannerlord.Diplomacy`
24. `ImprovedGarrisons`
25. `Fourberie`
26. `ArenaOverhaulRedux`
27. `OpenSourceArmory` (v2.1.0, recorded manifest version)
28. `OpenSourceWeaponry` (v2.1.0, recorded manifest version)
29. `OpenSourceSaddlery` (v2.1.0, recorded manifest version)
30. `OpenSourceArmoryRBM` (v2.1.0, recorded manifest version)
31. `OpenSourceWeaponryRBM` (v2.1.0, recorded manifest version)
32. `OpenSourceSaddleryRBM` (v2.1.0, recorded manifest version)
33. `BreakablePolearms`
34. `ExpandedArmouryBL` (e1.5.2, recorded manifest version)
35. `ExpandedArmouryBL.FullFixRBM` (v1.1.0.0, recorded manifest version)
36. `ZZZ_troop_overhaul` (v1.0.1, recorded manifest version)
37. `NoblesBuyStuff`
38. `KingdomBorders`
39. `CustomisableUI`
40. `CalradiaUnified`
41. `EBA_OSA_ZZZ_CultureGear` (v1.1.0, recorded manifest version)

Versions not recorded here require a fresh installed-manifest inventory; do not infer them from download titles. The installed CU manifest historically reports v0.38.0 and does not identify the exact compiled DLL; verify its hash against the candidate run before testing.

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

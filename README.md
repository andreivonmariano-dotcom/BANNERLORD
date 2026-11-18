# Calradia Unified v0.27.0 | Reliable child count and coordinated Sandbox creation

**Target game:** Bannerlord PC v1.5.7.125802.
**Status:** Compiled candidate under active verification. Source-policy tests and CI are not proof of in-game UI behavior or saved-game persistence.

## What the actual v0.26 game test proved

The player's Vlandian **Lord** start succeeded with a real newly generated NobleRandom spouse. The new wife joined `player_faction`, had native noble birth-clan parents, and two real children received parent-based appearance.

The user intended **one** child. The runtime log says `requested_children=2|actual_children=2|created_children=2`. The family system created exactly the two children stored in the origin profile. This is a **creator number-persistence error**, not a repeated hero-creation callback. The prior numeric slider changed a pending display value, but required a separate `Confirm amount` click. The request remained `ChildCount=2` if confirmation was missed.

## v0.27 corrected numeric controls

- Slider and plus/minus buttons **save every valid numeric change immediately**. Direct `Enter exact value` remains for typed numbers. If a value cannot be saved, the UI reloads the actual persisted number, so a displayed count cannot disagree with the chosen count.
- Each new Sandbox resets the dependent count to **0**, disables prior spouse request and siblings, resets fief mode to Native, and clears previous transfer permission. The role, spouse background and names may remain as editable preferences.
- The right-side campaign plan displays `1 child` and the child's exact name, sex and age. Review is read-only.
- Logs `sandbox_creator_family_plan|child_count=N` at acceptance, `sandbox_overhaul_accepted|child_count=N` at native handoff, `cu_family_loaded_plan|requested_children=N` after native character creation, and `sandbox_start_contract|requested_children=N|children=M` after creation.
- The code still creates a spouse first, then strictly loops over the saved `ChildCount` to create the requested children using native parent links and two-parent body-property inheritance.

## Native stages are now presented as an explicit continuation

The CU editor has five pages: **Starting Life → Household → Resources → Kingdom & World → Review**. The redundant blank CU Appearance page has been removed, so the player does not select culture or face twice.

The header says **PART 1 OF 2: STARTING CONDITIONS** and the final button says **NEXT: NATIVE CREATOR**. Bannerlord then owns **culture, 3D appearance, background/origins, age, and difficulty** in Part 2. The real native start is applied at character-creation phase 8; CU's spouse and children are created at phase 9. This avoids the former competing FaceGen view registration that crashed the culture stage in v0.22.

This is a more coherent two-part setup, **not** a fully replaced native wizard or a proven in-game polished interface. The remaining one-screen unified creator requires safe stage/view architecture and screenshots.

## Evidence and required game test

- [v0.27 exact one-child acceptance](docs/V027_CHILD_COUNT_NATIVE_HANDOFF.md).
- [Windows build and policy checks](.github/workflows/bannerlord-build.yml).
- [Existing generated-family marriage fix](docs/V026_ACTIVE_MARRIAGE_ACCEPTANCE.md).
- [Full unified creator design goal](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44).

Test only a disposable new Sandbox: Lord/Vlandia, NobleRandom spouse, exact **one female child**. Confirm the slider saves one immediately, the right preview shows your child's name/age, the native wizard follows once, the Clan contains a real spouse and only one child, and the same relatives persist after saving and fully restarting the game. The prior v0.26 save with two children is not silently edited by installing v0.27.

Restore native Default/Advanced starting selector by setting `OverhauledCreatorEnabled=false` in `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`.

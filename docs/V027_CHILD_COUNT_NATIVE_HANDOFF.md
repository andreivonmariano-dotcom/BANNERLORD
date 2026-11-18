# v0.27: Exact child count and coordinated native character creator

**Game:** Bannerlord PC v1.5.7.125802. **Status:** reference-compiled test candidate until new-game UI and save/reload pass.

## What the user's November 18, 2026 runtime proves

The v0.26 log shows actual native success, not a fake form value:

- `cu_family_new_adult_activated ... previous_state=NotSpawned|state=Active`.
- `cu_family_marriage_gate native_eligible=True`.
- `cu_family_spouse_created ... clan=player_faction`, noble birth clan `clan_vlandia_2`.
- `cu_family_child_created id=CharacterObject_3279|age=6` and `id=CharacterObject_3280|age=8`.
- `cu_family_verification result=PASS|requested_children=2|actual_children=2|created_children=2`.
- `sandbox_start_contract result=PASS|native_type=vassal|kingdom=vlandia|fiefs=3|spouse_present=True|children=2`.

**User intention:** one child. **Profile consumed by the game:** two children. This is a creator **input-commit mismatch**, not a duplicate child loop. The family code loops strictly `i < profile.ChildCount`, so the two children were requested by the persisted profile.

## Root cause and repair

The former `PendingNumber` setter only updated its presentation and an in-memory `pendingNumber`. The creator required a separate `Confirm amount` button to call `Write`. A user could reduce the visible value from two to one, continue, and still persist `ChildCount=2`. The right summary would remain at two, but its text was too crowded to reliably catch the mismatch.

v0.27 changes all numeric controls to **save immediately** on +/- or slider movement. The setter commits the selected number via `Write(Current.Key,next.ToString(...))`. If saving is refused, the UI is refreshed from the last actual saved value rather than showing an unsaved request. `ExecuteSaveNumber` is repurposed as **Enter exact value** and opens a validated numeric text field for direct entry.

A fresh new Sandbox calls the testable `SandboxOriginProfile.ResetForNewCampaign`. It resets the *spouse request*, `ChildCount=0`, `SiblingCount=0`, active family feature flag, fief mode to `Native` and transfer authorization to false. Names, social backgrounds and role preferences remain for convenience but cannot silently summon a previous campaign's relatives or transfer holdings.

At acceptance the editor logs `sandbox_creator_family_plan|child_count=N|children=Name:Gender:Age`. After the **native** wizard finalizes, the campaign logs `cu_family_loaded_plan|requested_children=N` and the contract logs both `requested_children=N` and `children=M`. These events expose an incorrect handoff before blaming the Hero creator.

On the right campaign summary, `FamilySummary` reports the spouse and count, and a distinct `ChildPreview` displays the single selected child's name, gender and age. The final review is read-only and cannot silently modify the role.

## Harmonized native stages without replacing FaceGen

**PART 1 / 2 | STARTING CONDITIONS (CU):** five pages: Starting Life → Household → Resources → Kingdom/World → Review. The redundant CU Appearance page and cosmetic culture preference are *removed* from quick setup, to avoid asking the player to select culture twice. Unfinished PlayerAge fields remain Advanced-only and are marked not applied. The button now says **NEXT: NATIVE CREATOR**.

**PART 2 / 2 | NATIVE BANNERLORD:** culture → live 3D appearance → background/origins → age → difficulty. These real engine stages own sex, culture and finalization. CU family generation runs at character creation phase 9 after native political-start application at phase 8.

This is a coordinated **two-part flow**, not a full one-screen replacement. No `CharacterCreationStageView` duplicate is registered, preventing recurrence of the reported v0.22 culture-stage crash. A true seamlessly integrated game-grade creator still requires a separately game-tested stage/view implementation and UI inspection. The visual quality of v0.27's new labels and five tabs must be confirmed by screenshots.

## Required test

1. Install exact **v0.27** Windows successful CI artifact, verify DLL hash against that run. Do not load existing saved campaigns for a new family.
2. Start a disposable new Lord / Vlandia / `NobleRandom` spouse. Observe that the family count starts at **0**. Set `ChildCount=1` with the plus button or slider. Verify the right preview states **1 child** and shows your chosen child's name, gender and age.
3. Continue through the five CU pages. Review displays the family plan and the footer reads **NEXT: NATIVE CREATOR**. Bannerlord's native culture/3D face/background/age/difficulty wizard runs only once each.
4. Verify `sandbox_overhaul_accepted|child_count=1` and `sandbox_creator_family_plan|child_count=1`.
5. Finish native wizard and check `cu_family_loaded_plan|requested_children=1`, `cu_family_child_created` exactly once, `cu_family_verification result=PASS|created_children=1`, and `sandbox_start_contract result=PASS|requested_children=1|children=1`. Verify real spouse and one native child in Clan, with both parents.
6. Save, close game completely, reload; verify same spouse, child, native clan and no duplicates. Logs are event histories, so filter by the latest `sandbox_overhaul_accepted` timestamp rather than counting all prior campaigns.
7. Screenshot the five-page UI and transitions. Test the slider, +/- button, direct exact-number entry, Back and Continue at the actual display resolution.
8. Only after success, test 0 children and 2 children as boundary cases. Invalid age inputs must show a validation error, never launch with a hidden older count.

**Status boundary:** The user's observed v0.26 `PASS` establishes first-run in-memory creation for two children, not that they survive save/exit/reload or that the five-page UI works in-game. v0.27 itself is not game-tested until the new attempt.

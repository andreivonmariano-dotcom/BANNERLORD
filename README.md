# Calradia Unified v0.31.0 | Experimental Native 3D Face Workshop

**Target:** Bannerlord PC v1.5.7.125802. **Status:** [Windows CI #44362408521](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44362408521) passed at commit `f7574effec028ac4d52de2ac8397666ee28b9c76`, **Gate B COMPILED_UNTESTED**. The game-facing workshop and save/reload remain untested. **The second native character wizard still appears**.

In **Sandbox → Starting Life → More options**, an optional **Experimental: edit live 3D face in CU workshop** choice adds a distinct CU stage after native difficulty. It composes Bannerlord's real `CharacterCreationFaceGeneratorView`, preserving the native BodyGenerator, model, face history, camera and face controls. A CU read-only context ribbon remains unfocused so the editor owns user input. The option is **off by default**, and the previous experimental CU review cannot be selected at the same time.

The v0.31 DLL is 242,176 bytes, SHA-256 `23321aa2d6a0c049965d37e31496389500ca53f3c24821381290296855048be6`. The inner runtime ZIP passes CRC and one-DLL checks, SHA-256 `8916d60f93eedab9145f16082d265bf482e167db7d02429f14c06d62ac4c208c`. The verified CI run executed **627 independent C# policy checks** and compiled with **0 warnings / 0 errors**. These are build results, not native screen-rendering or gameplay test evidence.

The new `NativeCreatorParityPolicy` guards all seven native stage prerequisites before the optional workshop can load. No native stage view is replaced, no child Heroes are created during preview, and the native character-creation sequence stays unchanged when the mode is disabled. The goal of a single CU creator from **Sandbox click to campaign map** remains open under [issue #44](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44).

**Technical and test protocol:** [v0.31 Face Workshop and Unified Creator Parity](docs/V031_FACE_WORKSHOP_AND_UNIFIED_CREATOR_PARITY.md). Keep the original v0.29 review and family features intact.

---

## Previous v0.29 candidate and established mechanics


**Target game:** Mount & Blade II: Bannerlord PC v1.5.7.125802. **Status:** COMPILED_UNTESTED for v0.29 UI and the v0.28 family-home feature. Previous in-game evidence validated genuine spouse and children, but the new stage, portrait gallery and save persistence still need separate Bannerlord playtests.

### New in v0.29

The starting-conditions creator uses clearer five-chapter navigation, explicit spouse and child summary text and a handoff cue to **the game's own live 3D face editor**. It never replaces native culture, face, background, banner, clan naming, review or difficulty screens. The fully unified single-screen creator is **not complete**.

Under **More options** on Starting Life, the experimental setting `Preview CU choices inside Bannerlord's 3D review` can append one distinct origin-review stage **after native difficulty**. It wraps Bannerlord's real rotatable 3D player review through its native stage-listener contract, has a nonfocus CU summary layer, and is **OFF by default**. The CU stage has no inherited native Review-stage registration. The original v0.22 duplicate FaceGen view remains banned. If engine view discovery fails, the extra page is skipped and logged. If the CU summary prefab fails, the base 3D review remains available.

After an actual family exists, native town/castle menus show **CU: View my household portraits**. The gallery reads the real main character, native spouse and player-linked children, plus age, parents and recorded home. Images use native Hero body data. It never creates preview relatives, ages children, or recruits infants. Missing baby visuals must not become fake adult heroes.

Read [v0.29 native UI and portrait runtime acceptance](docs/V029_NATIVE_CREATOR_AND_FAMILY_UI_ACCEPTANCE.md) and the [permanent PowerShell installation and playthrough standard](docs/POWERSHELL_INSTALL_AND_PLAYTHROUGH_STANDARD.md). Both the standard gameplay audit and its optional `-RequireNativeReview` / `-ExpectedPortraits` checks run under Windows PowerShell 5.1.


## Standard Windows PowerShell install and playthrough report

The project's **standing player workflow** is documented in [PowerShell Installation and Playthrough Standard](docs/POWERSHELL_INSTALL_AND_PLAYTHROUGH_STANDARD.md). Future Calradia Unified coding chats should read that file plus [AGENTS.md](AGENTS.md) and [PROJECT_INSTRUCTIONS.md](PROJECT_INSTRUCTIONS.md) before suggesting a download.

Install the exact successful Windows CI artifact through [`scripts/Install-CU-From-Artifact.ps1`](scripts/Install-CU-From-Artifact.ps1) with **`-ExpectedDllSha256`** from that same run. It rejects another same-version DLL before changing the installed module and backs up the previous folder.

After entering the campaign map, run the read-only [`scripts/Test-CU-Playthrough.ps1`](scripts/Test-CU-Playthrough.ps1) with your expected child count. This reports the latest accepted start, native family verification, actual children, home settlement events and exceptions. Check save, full exit and reload separately. For crash evidence, use [`scripts/Export-CU-Diagnostics.ps1`](scripts/Export-CU-Diagnostics.ps1).

[Successful standard-workflow CI run #44345749188](https://github.com/andreivonmariano-dotcom/BANNERLORD/actions/runs/44345749188) verified the CI hash reject-before-install test and a synthetic latest-campaign diagnostic, alongside 617 game-policy checks. The downloaded ZIP remains a **COMPILED_UNTESTED** candidate, not a proven game release.

## Observed player state and actual cause

Previous runtime logs proved a newly generated `NobleRandom` Vlandian spouse joined the player's clan and children received real parent links with inherited body properties. The user's first v0.26 family test created two children from a saved count of two; v0.27 corrected the separate numeric confirmation issue and now reports exactly one child as selected.

The player now reports spouse and baby shown as **Holding** in the Clan screen. Bannerlord's native underage heroes are not ordinary party members. Infants cannot be recruited as troops just to move them. `Holding` does not invalidate parentage.

## New v0.28 family residence

The five-step CU starting-conditions creator now includes **Where should your family live?** on Kingdom & World:

- `OwnedFief` (default): an owned town or castle, preferring your safe clan home and then a town before a castle.
- `ClanHome`: use the clan's owned home fortification if safe, then another owned fief.
- `SpecificOwned`: choose the exact internal settlement ID for an **already owned** castle/town. No cross-kingdom ownership changes. Unknown, besieged, contested or village locations are refused.

After native character creation, fief assignment, real spouse marriage and child creation are complete, `SandboxFamilyHome`:
- Places the **adult spouse** into the owned settlement with native `TeleportHeroAction.ApplyImmediateTeleportToSettlement`.
- Assigns a **minor child** `StayingInSettlement` directly, which is a saveable Hero field registered in the settlement's non-party resident list. Does not activate, age, enlist or teleport a baby to a party.
- For an adult offspring not assigned to a party, uses native active-Hero relocation. Only an adult still in the native `NotSpawned` state is activated first.
- Verifies recorded settlement and residents, logs `cu_family_home_result`, `cu_family_child_home`, `cu_family_home_status`.

**Important:** The vanilla Clan screen might still show **Holding** for a child under 18 even after residence is saved. A family-home location is not the same as a physical baby NPC walking around a keep. This candidate does not replace child lifecycle or game scenes.

## Use your existing saved family

You do **not** need to create another spouse or child. Load your existing Sandbox save and enter any town/castle **owned by your clan**. In the settlement's native menu, choose:

**CU: Make this my family's home**

This explicit in-campaign action places your already-existing spouse and minors at the current settlement. It does not spawn relatives again or modify anyone else's clan. Save, quit, reload, and verify the location and parent links remain. A spouse who is in an active party, captive or otherwise unavailable is not forcibly removed.

## Creator and native appearance

CU remains five pages (Starting Life, Household, Resources, Kingdom & World, Review), with a dark Gauntlet-style panel and family-home choice. The native Bannerlord culture, 3D appearance, origins, age and difficulty screens follow as **Part 2**. Existing native character creation has not been replaced, so v0.22's competing FaceGen handler crash is not reintroduced.

## Testing and evidence

- [Full v0.28 family-residence acceptance checklist](docs/V028_FAMILY_RESIDENCE_ACCEPTANCE.md)
- [Windows build and package gate](.github/workflows/bannerlord-build.yml)
- [Original one-child count correction](docs/V027_CHILD_COUNT_NATIVE_HANDOFF.md)
- [Long-term unified 3D creator issue](https://github.com/andreivonmariano-dotcom/BANNERLORD/issues/44)

The compiled module is **not yet game-verified for family home placement**, and save/reload of the residence is still a required acceptance step. Test on a backed-up save or a disposable new campaign before relying on settlement residency.

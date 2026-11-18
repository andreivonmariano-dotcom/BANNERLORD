# Calradia Unified v0.28.0 | Family Homes in Native Castles and Towns

**Target game:** Mount & Blade II: Bannerlord PC v1.5.7.125802. **Status:** COMPILED_UNTESTED for family residence. The underlying spouse and child creator succeeded in an earlier real campaign, but v0.28 placement still needs a live game and save/reload test.

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

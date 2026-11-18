# Calradia Unified v0.28 | Family residence in castles and towns

**Target:** Mount & Blade II: Bannerlord PC v1.5.7.125802.
**Status:** Source/reference compiled candidate. New native residence logic must pass an actual player game and save/reload before release.

## Player evidence and request

The player's v0.26/v0.27 campaign proves a real Vlandian Lord with a newly generated NobleRandom spouse and parent-linked offspring. The family members still appear as **Holding** in the Clan panel. The player cannot send an infant to a party or transfer a baby manually to a castle.

**Engine distinction:** Bannerlord deliberately holds underage heroes outside active party recruitment. A baby under `Campaign.Current.Models.AgeModel.HeroComesOfAge` is a real Hero with a native minor state. `Holding` is an expected stock status for a minor, and the mod must NOT activate a baby or add its CharacterObject to any troop roster merely to change a label.

Version-matched native APIs:
- `Hero.StayingInSettlement` setter registers a hero in `Settlement.HeroesWithoutParty` and is backed by `SaveableField(580)`.
- `Hero.CurrentSettlement` observes `StayingInSettlement` for non-party heroes.
- `TeleportHeroAction.ApplyImmediateTeleportToSettlement` is the correct adult relocation action. It changes a nonactive hero to **Active**, so it must never be called on an infant.
- `Clan.PlayerClan.Settlements`, `Settlement.IsFortification`, `OwnerClan`, `IsUnderSiege`, and the native battle pointer control valid castle/town selection.

## v0.28 logic

After native advanced start, fief grant, and a fully verified `SandboxFamilyCreator.TryCreate`, `SandboxFamilyHome.TryPlace` selects the household residence:
- **OwnedFief** (default): use a safe owned home castle/town, preferring the clan home. If unavailable, pick an owned town before an owned castle, with stable ID ordering. Never gift, seize or move between kingdoms.
- **ClanHome**: prefer the actual clan home if it is still owned and safe; otherwise fall back to another safe owned fortification.
- **SpecificOwned**: require the exact native settlement ID **already owned by the player clan**. Missing ID, foreign holdings, village, siege, or active battle are refused, with no silent fallback to other castles.

The newly generated adult spouse is moved via native `TeleportHeroAction.ApplyImmediateTeleportToSettlement`, provided they are not captive or already assigned to a party. Its `StayingInSettlement` and `CurrentSettlement` are checked.

For every real child of the player **and the same spouse**:
- A **minor** who is not in a party or captive gets `child.StayingInSettlement = target` (the saved, native settlement-residence property).
- Verify `child.CurrentSettlement == target` and `target.HeroesWithoutParty.Contains(child)`.
- Preserve the child `HeroState`, native age, parents, clan and appearance. Never use `TeleportHeroAction` or party recruitment for minors. The vanilla Clan UI **might still say Holding**, since the minor is not an active party Hero. Treat that label separately from whether the save records a physical home.
- An **adult offspring** without a party is eligible for native teleport. An adult in the native `NotSpawned` state can be activated first. Disabled, captive or party-bound heroes are not forced out of their current assignment.
- The placement is one shot. `cu_family_child_home`, `cu_family_home_result` and `cu_family_home_status` record ID, settlement, count, and minor state.

## Existing-campaign opt-in, without recreating relatives

At a castle or town **already owned by your clan**, the native settlement game menu gains:

**CU: Make this my family's home**

Click only once you have arrived at the chosen settlement. This invokes the same safe placement on the already-existing spouse and children. It does not create another spouse, regenerate children, repeat Advanced Start or change the original family data. The option is present only in standalone Sandbox and only if the hero has a real spouse and the settlement belongs to the player's clan.

This is **not** a background migration of every prior saved campaign. Existing saves are only modified by the player's explicit settlement-menu click.

## Creator and UX

The existing five-step CU starting-conditions creator remains coordinated with the native 3D culture/face/origins/age/difficulty sequence.

On Kingdom & World, select family residence:
- `Where should your family live?` → OwnedFief / ClanHome / SpecificOwned
- `Exact owned castle or town ID` appears only in SpecificOwned.
- The right summary displays the requested family home separately from native fief and world scenario.
- The real 3D appearance creator remains native. No competing CharacterCreationStageView registration or underage 3D model promises.

A single continuous native wizard with editable generated spouse models, family-tree portraits and a searchable fief gallery is **still unimplemented**. This candidate is a safe enhancement to the existing two-part flow.

## In-game acceptance

1. From the newest compiled v0.28 artifact, install with `Install-CU-From-Artifact.ps1` into the exact game directory, verify the DLL SHA-256 against the successful workflow. Back up existing saves.
2. **Existing save test:** Load the already verified Lord/Vlandia family save. Visit an **owned** castle or town; click `CU: Make this my family's home`. The spouse should be recorded `StayingInSettlement` there. Each minor should retain its original underage state and be registered in the settlement. The baby must **not** appear as a party recruit. Check `cu_family_home_result` and `cu_family_child_home`.
3. Save, quit Bannerlord completely, reload. Spouse and child still belong to PlayerClan with real parent links. They should keep their native residence at the chosen settlement, and no extra family Hero appears. A residual `Holding` label alone does not prove that the saved settlement reference failed.
4. **New campaign test:** disposable Lord / Vlandia / spouse NobleRandom / one daughter. Choose `FamilyHomeMode=OwnedFief`. After native wizard, verify `cu_family_home_result result=PASS|children_homed=1` and `sandbox_start_contract result=PASS|requested_children=1|children=1`.
5. Try SpecificOwned with an actual owned castle ID, then a foreign/unknown castle ID. The latter must refuse relocation and never change ownership.
6. Check regular spouse party recall and later adult children's roles still function. The normal game can still move the adult spouse after the initial residence placement. This feature does not permanently lock adult family members to a castle.
7. Screenshot the CU Kingdom/World page and game settlement menu. Inspect alignment and readability at the user's display resolution.

**Unproven until in-game:** exact settlement residency and saves, the UI label, the visible castle-visitor roster for minors, and whether manual location remains unchanged over many campaign days. No forced baby scenes or custom Clan-panel patch is part of v0.28.

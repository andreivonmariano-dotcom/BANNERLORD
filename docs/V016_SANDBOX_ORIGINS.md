# v0.16 Expanded Sandbox Origin Designer

**Target:** Bannerlord PC v1.5.7. **Status:** source candidate until exact Windows CI passes; native runtime unverified.

## Native baseline we keep

Bannerlord v1.5.7 already includes Advanced Starting Options: King, Vassal, Mercenary, Trader, Outlaw and Beggar, alongside world scenarios and modifiers. We do not replace that framework or bypass the game's rank unlock progression. Choose **King** in the game's Advanced Start for real kingship. The Calradia Unified "King" profile describes your resources and desired family, not a new kingdom action.

## Five origin identities

- Wanderer: independent character with a small budget.
- Soldier: martial personal history, potentially native-culture troops.
- Knight: mounted noble archetype, with custom items planned.
- Lord: native Vassal start plus desired estate and family profile.
- King: native King start, plus custom family and treasury profile.

The identity alone is not sufficient proof of social standing, service to another lord's army, equipment, or titles.

## Profile controls

User file: `%LOCALAPPDATA%\CalradiaUnified\sandbox-origin.ini`.

| Field family | Range | Current game effect |
| --- | --- | --- |
| Enabled | true/false, default false | New-game only, opt-in |
| Role | Wanderer/Soldier/Knight/Lord/King | Recorded, native ASO owns formal status |
| Gold | -1 (keep vanilla) or 0..2,000,000 | Native gold action on new game |
| Influence | -1 (keep vanilla) or 0..100,000 | Native hero influence flow only if native kingdom exists |
| Troops | 0..50 | Add only existing culture basic troop |
| PlayerAge | -1, 18..80 | Stored only |
| StartingTown | empty/native or a real town StringId | Stored only, no unsafe teleport |
| EquipmentKit | Vanilla/Traveler/Soldier/Knight/Royal/Custom | Stored only |
| Item.1..Item.12 | exact, safe item IDs | Grants one engine-recognized item per slot to the player inventory; does not auto-equip |
| Attribute.NAME | 1..10, six attributes | Native HeroDeveloper attribute change at new game, game test pending |
| Skill.NAME | 0..330, eighteen skills | Native initial skill-level setter at new game, game test pending |
| Focus.NAME | 0..5, eighteen skills | Native HeroDeveloper focus change at new game, game test pending |
| Trait.NAME | -2..2, five traits | Native personality trait-level setter at new game, game test pending |
| SpouseEnabled | false by default | Stored only |
| SpouseName/Age/Gender/Culture/FaceSeed/EquipmentKit | Age 18..70; unique names editable | Stored only, no NPC creation |
| ChildCount | 0..6 | Stored only, each Child.N.Name/Age/Gender/Culture/FaceSeed/EquipmentKit editable |
| SiblingCount | 0..4 | Stored only, matching Sibling.N.* fields |

For every child, selected child age must be at least 16 years less than both parent ages. The editor refuses children without a spouse, invalid ages, disallowed names, and unsafe text. Each configured relative remains a **template** until the native Hero and family lifecycle implementation receives save/load and age-validation tests.

## Why the feature is staged

Kingdom status, family links, Hero birthdays, clan membership, child growth, faces, inventories and location are all persisted native objects. Mutating them incorrectly at game start can corrupt an ongoing campaign. The first release focuses on an independently tested profile schema, user-prepared family templates, and guarded new-game-only player resources, progression and inventory. The real game must still verify exact effects and save/load. Full family generation and player progression are next after exact game lifecycle verification.

## Real v1.5.7 tests not yet run

1. Launch with the v0.16 module and official modules. Verify official ASO choices remain intact.
2. In an existing disposable campaign, prepare a profile, then begin a **separate** new Sandbox.
3. Record the player's starting gold and troops with `Enabled=false`, and with `Enabled=true` and explicit values. Verify exactly one native resource change.
4. Save and reload. Confirm resources are not added again and that no duplicate origin behavior or event fires.
5. Test an ASO King start with a matching King profile and a mismatched Wanderer start. Neither case must create a second kingdom.
6. Attempt invalid spouse/child ages and malformed profile fields. A refused or malformed profile must have no world effects.
7. Verify original town/castle menus, Gauntlet screen, mission transitions and siege state are unchanged.

## Native input dialogs (v0.16)

In the **Design my NEXT Sandbox starting life** menu, select Family, Attributes/Skills/Focus/Traits or Equipment. Each choice opens a Bannerlord-native text inquiry with validation. The spouse's name and age, each child's name, age, gender and appearance seed, all core skill/attribute/focus/trait numbers and twelve item IDs can be edited without touching an existing campaign. Values remain templates for anything not yet supported by the guarded new-game application.

The editor is registered only for a standalone Campaign game type. It refuses Story Mode.

## Standalone pre-campaign creator (experimental)

The `OnBeforeInitialModuleScreenSetAsRoot` entry point registers **CU: Design Next Sandbox Character** in the game's initial main menu. It creates an optional Gauntlet `SandboxOriginPanel` with five tabs: Identity, Family, Progression, Equipment, and Location. Numeric values are bounded with a slider and explicit Save, and names/face-seed strings use a native text inquiry. It writes only `sandbox-origin.ini`. The main-menu option is registered once per module lifetime. The in-campaign editor remains available as a fallback.

**CI compilation and XML validation do not prove actual screen rendering.** Confirm the initial menu entry, rendering, tab controls, close/reopen, editor text input and exact same-profile persistence on the v1.5.7 game before release. New-game family hero creation, age adjustment, actual role affiliation, item auto-equipping and town relocation remain unimplemented.

## Compile-only verification and safety

The new pre-game panel's prefab bindings and click handlers are checked by `scripts/test_sandbox_origin_panel.py`. The new-game event refuses Story Mode and stores `CU_v016_origin_applied` in a campaign save. These are source and policy gates, not independent proof of world mutation or safe campaign reload.

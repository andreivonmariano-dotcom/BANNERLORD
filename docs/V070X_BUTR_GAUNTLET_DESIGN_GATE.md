# Calradia Unified v0.70x | Game-native UX system and BUTR integration plan

## Root problem and immediate deliverable

The player's complaint concerns real usability, not merely missing art: role functions were accessible through text inquiries and settlement lists rather than through their rightful character, clan, party and kingdom contexts. v0.70v grouped the original 27-ish static registrations under two location-aware hub entries, with existing resource actions preserved. v0.70w gave the Master of Arms and Marshal true existing-troop native garrison actions via direct companion dialogue, still unverified in game.

v0.70x implements a new **native Gauntlet household dossier** with a deliberate editorial layout. The real current campaign state fills three reading groups:

1. **Family and lineage**: actual spouse, living children and player-clan name.
2. **Household officers**: actual members of the native companion roster.
3. **Service and current responsibilities**: correct Knight patron/objective/oath/escort, Lord leader/holdings/heir, or King crown/Marshal/single Kingsguard/Minister governorship/court.

A read-only `CUNobleDossierScreen` uses its own `CUNobleDossier.xml` prefab and ViewModel with named bindings, Bannerlord-native `GauntletLayer`, keyboard Escape/Close and previous screen restoration. No invented clan ownership, fake child, troop, kingdom law, or extra dependency. Failure to create the screen falls back to the preexisting native inquiry, with diagnostics.

The existing family portrait gallery was shortened to **1080x650** from **1180x716** for a genuine minimum 1280x720 clearance. Eight native Hero entries (player, spouse, up to six children) occupy a 1005x432 list with 53-pixel rows and columns totaling no more than the list width. Shorter footer copy and a smaller native button keep the action below the list and inside the panel. The optional native settings screen now accepts Escape as an alternate close route.

## Consistent design language

Use TaleWorlds' established dark leather, warm parchment, restrained bronze borders, native buttons and well-spaced headings. Keep visible values derived from living game Heroes and their current native relationships. Each screen needs: a distinctive title, concise contextual subtitle, an obvious current duty, a short location or interaction clue, a primary action, working Back/Escape, and no raw StringIds or debug receipts.

- Knight's normal path: patron's native Encyclopedia entry and direct lord dialogue, current commission and map-party escort through quest journal once supported. Dossier is informational, not an oath-acceptance remote control.
- Lord's normal path: Clan/Party screens and real Master of Arms/household Knight dialogue. Land title, succession and garrison actions never exceed native authority.
- King's normal path: Kingdom decisions and Clan/Party administration, real Minister governorship, Marshal in native conversation and **one real Kingsguard Hero**. The council cannot pass a fabricated kingdom policy through a custom screen.
- Family roster: real portraits and provenance from Hero records, no synthetic preview family or fabricated baby soldiers.
- Settings: use a proper mod options interface, not regular castle menus.
- Owned-fief stores/garrisons: meaningful local estate actions can remain local, organized under one administrative entry.

## BUTR options, with version and safety boundaries

**BUTR UIExtenderEx** is the correct tool for hooking new UI into existing vanilla Clan, Party, Encyclopedia, or Kingdom Gauntlet movies through prefab patches and ViewModel mixins. Official v3 documentation states a Bannerlord baseline of **v1.3.4**, covering the nominal pinned `v1.5.7.125802` range, but the exact installed DLL ABI, prefab names and all supported mod combinations must be validated at runtime. Modern v2 prefab patch APIs and the UIExtenderEx analyzer can help catch bad XPath, broken property bindings and unsupported widget attributes at build time.

**BUTR MCM/Mod Configuration Menu** belongs to mod configuration (e.g. feature toggles, debugging, settings), not historical family governance or live kingdom decisions. **ButterLib** offers modding services and diagnostics. Neither of these transforms a fake role into a real native action.

**Do not make BUTR a mandatory dependency in v0.70x.** The current module has only Native/SandBoxCore/Sandbox dependencies. First build a separate proof-of-concept extension against the exact game references: verify the installed UIExtenderEx release, its Harmony/ButterLib load order, original Clan/Kingdom movie names, view model property binding, live switch/Back, and save/quit/reload. Never vendor or bundle third-party runtime DLLs into Calradia Unified. Only after verified compatibility select a separate optional UI adapter (if stable) or an explicitly version-gated dependency. Keep the standalone Gauntlet dossier as the fallback.

## Quality gate

The new `scripts/test_v070x_gauntlet_dossier_layout.py` validates XML parsing, proper binds, executable Close, screen focus, Esc and fallback, 1280x720 panel bounds, the full family list width and eight-row capacity. It explicitly checks there is no unverified BUTR dependency in the mod manifest or project. Static layout numbers do not prove actual Bannerlord UI rendering, localization, font metrics, scaling, controller focus or 16:9 and ultrawide presentation.

Native acceptance remains blocked until **actual** screenshots at 1280x720, 1650x927 and 1920x1080 show no clipping, overlap, ghost focus, low contrast, raw IDs, scroll errors or duplicated menu entries. Verify mouse, keyboard and controller navigation and return; reload the same save and reopen every screen. Confirm the native Party/Clan/Kingdom states agree with the dossier. Record runtime logs and actual UI screenshots, not only synthetic fixtures.

## Remaining work

The live game does not yet have a BUTR-embedded Clan/Party/Kingdom tab. Nor is the Knight's field commission a native QuestBase journal entry, the Lord's household Knight a native battlefield officer, or the King's council a real political voting system. These are separate functional milestones. Their design and legal game authority must be implemented and tested before making them visually prominent or claiming them complete.

Keep PR #93 draft, the default installer unchanged and Gate C–E blocked until game proof.

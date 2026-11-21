# Calradia Unified v0.70x | Game-native UX system and BUTR integration plan

## Root problem and immediate deliverable

The player's complaint concerns real usability, not merely missing art: role functions were accessible through text inquiries and settlement lists rather than through their rightful character, clan, party and kingdom contexts. v0.70v grouped the original 27-ish static registrations under two location-aware hub entries, with existing resource actions preserved. v0.70w gave the Master of Arms and Marshal true existing-troop native garrison actions via direct companion dialogue, still unverified in game.

v0.70x implements a new **native Gauntlet household dossier** with a deliberate editorial layout. The real current campaign state fills three reading groups:

1. **Family and lineage**: actual spouse, living children and player-clan name.
2. **Household officers**: actual members of the native companion roster.
3. **Service and current responsibilities**: correct Knight patron/objective/oath/escort, Lord leader/holdings/heir, or King crown/Marshal/single Kingsguard/Minister governorship/court.

A read-only `CUNobleDossierScreen` uses its own `CUNobleDossier.xml` prefab and ViewModel with named bindings, Bannerlord-native `GauntletLayer`, keyboard Escape/Close and previous screen restoration. No invented clan ownership, fake child, troop, or kingdom law. Required BUTR dependencies are introduced in v0.70y. Failure to create the screen falls back to the preexisting native inquiry, with diagnostics.

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

**Current decision (v0.70y): the entire BUTR stack is mandatory.** The module now declares Harmony, ButterLib, UIExtenderEx and MCM as nonoptional launcher dependencies. The built C# uses UIExtenderEx 3.0.3 and MCM v5.13.1. Native Clan, Party and Kingdom patches must pass runtime load order, ViewModel binding, selector, click, Back, and save/quit/reload validation before release. The Gauntlet dossier remains a fallback only for a failed individual screen, never an excuse to run without BUTR. Do not ship vendor copies of the four framework DLLs.

## Quality gate

The new `scripts/test_v070x_gauntlet_dossier_layout.py` validates XML parsing, proper binds, executable Close, screen focus, Esc and fallback, 1280x720 panel bounds, the full family list width and eight-row capacity. It verifies the four required framework module declarations and the UIExtenderEx 3.0.3 compile contract. Static layout numbers do not prove actual Bannerlord UI rendering, localization, font metrics, scaling, controller focus or 16:9 and ultrawide presentation.

Native acceptance remains blocked until **actual** screenshots at 1280x720, 1650x927 and 1920x1080 show no clipping, overlap, ghost focus, low contrast, raw IDs, scroll errors or duplicated menu entries. Verify mouse, keyboard and controller navigation and return; reload the same save and reopen every screen. Confirm the native Party/Clan/Kingdom states agree with the dossier. Record runtime logs and actual UI screenshots, not only synthetic fixtures.

## Remaining work

Native Clan, Party and Kingdom extension buttons are implemented in source, but their in-game rendering, focus, XPath placement and save/reload have not yet been verified. Nor is the Knight's field commission a native QuestBase journal entry, the Lord's household Knight a native battlefield officer, or the King's council a real political voting system. These are separate functional milestones. Their design and legal game authority must be implemented and tested before making them visually prominent or claiming them complete.

Keep PR #93 draft, the default installer unchanged and Gate C–E blocked until game proof.

## v0.70x acceptance extension

Knight, Lord and King now require `cu_native_household_dossier_attached` scoped to their exact campaign role. The on-screen release gate separately requires `ui_gauntlet_dossier_after_reload=true` and `ui_gauntlet_dossier_back_escape=true`, alongside the existing native UI screenshot, clipping and identity proof. Synthetic tests reject the false movie flag and a dossier event from another noble role.

## v0.70y | Explicit user decision: BUTR becomes required

The user explicitly revoked the previous optional-but-supported BUTR plan. The draft module now **requires** `Bannerlord.Harmony`, `Bannerlord.ButterLib`, `Bannerlord.UIExtenderEx`, and `Bannerlord.MBOptionScreen` in `SubModule.xml`. The C# build compiles against UIExtenderEx **3.0.3** with its analyzer, and registers three native patches/mixins on `Clan`, `Party`, and `Kingdom` movies at module load. These are real native ViewModel targets, not settlement menu surrogates.

The native Clan/Party/Kingdom extension buttons show short contextual labels and lead into the role-specific game-owned dossier and open the existing full read-only Gauntlet dossier. The exact movie names, selector positions, collisions with other mod patches, actual focus and native close behavior still need a game test. UIExtenderEx patches do not introduce independent political authority or fictional troops.

The artifact installer requires all four BUTR modules both in the candidate manifest and in the game's `Modules` directory before it changes an installed mod. A missing dependency is an error rather than a fallback. No BUTR runtime binaries are copied into the project or artifact. Testers must install compatible releases from their own official distribution and load them before Calradia Unified. The old `-LatestVerified` pin remains historical until a live release is accepted, so the installer refuses a historical artifact lacking the new required module declarations.

**Source gate:** `test_v070y_butr_required_native_integration.py` verifies the four mandatory IDs, real extension registration, all three native movie mixins, build package, no fake native power, and install-time fail-closed checks. Gate B compilation is not native screen-rendering proof.

## v0.70y | Required MCM settings surface

The legacy `NativeSettingsBehavior` still compiles for old source compatibility, but it is **no longer registered** in an active campaign. The repeated `CU: Configure Calradia Unified` town/castle options are absent. A real `CUBUTRMCMSettings : AttributeGlobalSettings<CUBUTRMCMSettings>` exposes eight named settings in the required MCM v5 Mod Options screen. Values still read and write the existing user INI via `SettingsCatalog` and `TrySetMenuValue`, preserving one authoritative gameplay profile instead of fabricating new campaign settings. Changes take effect when the campaign systems reinitialize. MCM is configured with FormatType `none` and writes through existing INI-backed setters, not a separate JSON gameplay store.

## Full custom movie audit and release block

The source inventory parses every custom Gauntlet prefab plus the three literal native UIExtenderEx fragments and publishes `BuildResults/v070y-all-gauntlet-layouts.json`. Strict geometry is enforced for the newly active noble dossier and family gallery at 1280×720. Older creator, options and portrait-stage prefabs remain visible in the inventory with dimension, binding and navigation flags. Source checks cannot certify actual player scale, language, mouse/controller focus or another mod's patch ordering. Real screenshots at 1280×720, 1650×927 and 1920×1080 and same-save reload remain required. Only verified native character/party/kingdom state may populate UI. All seven start paths get a real career record from saved role economy, with full Knight/Lord/King information where native contracts permit it.

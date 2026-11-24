# Calradia Unified v0.71g | Native authority and historical plausibility

Target: Bannerlord v1.5.7.125802 pinned references.
Branch: audit/v069e-current-gameplay-and-visual-acceptance (draft PR #93).
Current state: source candidate, NOT in-game approved.

## Review: integration status

A successful Windows compile and policy suite prove API availability
and modeled state transitions. They do not prove a Gauntlet UI fits
every resolution, a conversation node is reachable, save-load
synchronization works inside an actual campaign, or that other mods
remain compatible. The main game remains authoritative for:
- actual Hero identities, age, parentage, culture, kinship, wounds
- parties, companions, prisoners, official clan and kingdom status
- native denars, gold transfers, relations, renown, fiefs and land
- native Army leadership and genuine vassalage

These systems must not re-issue native transactions on load.

## Q1: five-folio illustrated household register

The existing non-3D Party camp retains People, Officers, Letters,
and Service tabs. A fifth **Chronicle** tab shows a bounded list
of witnessed career chapters, Knight orders, escorts, real children,
completed petitions and observed native political service.
All names are resolved from actual Bannerlord Hero IDs.
This is a household record, not a new kingdom quest tracker.
The Letters page is the petition register. It does not simulate
mail delivery, promises, or unobserved journeys.

The visuals are native Gauntlet widgets, existing portrait assets,
and a restrained parchment-and-bronze palette. No 3D animations,
teleports, created characters or staged cutscenes are used.

## Q2: social records, loyalty and grievance

New save key: CU_v071g_social_bonds

A bounded ledger stores actual Hero IDs, verified responses,
native relation changes and later in-person meetings. It can
describe **Recorded goodwill**, **Established goodwill**,
**Strained relations**, or **Repeated grievance**, but it never
changes native AI loyalty, declares a war, or fabricates a rival.

Recorded goodwill requires actual native relationship evidence.
A repeated grievance requires at least two real refusals, a
confirmed negative relationship delta, and native relation -30
or below. A positive relation alone is not a proof of a personal
pledge. Family ambitions come from previously observed character
agendas rather than invented childhood memories.

The descriptive social ledger is exposed by inspecting actual
traveling companions. It is a first stage of character continuity,
not a complete simulated medieval household psychology.

## Q3: native military appointment and political legitimacy

New save key: CU_v071g_native_military_witness

After a real patron's recommendation and a genuine conversation
with the native realm's ruler, the Knight may ask about formal
vassalage. The response explicitly preserves the kingdom's native
membership path. The mod does NOT add the player to a kingdom.

A daily witness records the actual first observed native
vassalage date only when the player's independent household has
truly become a nonmercenary vassal clan in the patron's native
kingdom, led by the player. A separate receipt records the first
time the actual main party leads a real native Army. It neither
creates that Army nor grants a leadership office. A changed
kingdom no longer counts as current vassalage merely because it
once appeared in the household ledger.

The existing rank labels are PERSONAL household service
descriptions, not engine-level knight promotions. A royal
audience does not convey a fief, lordship or government office.

## Historical plausibility boundary

Medieval Europe did not operate under one universal, mechanically
ranked feudal code. Household officers, vassalage, land grants,
household obligations, inheritance and female property rights
differed by region and date. A single Vlandian house is closer to
a 12th-13th-century western European reference than a universal
model for the Calradic Empire, Battania, Sturgia, Aserai or Khuzait.

For a more demanding historical simulation, later work should
include culture-dependent offices and custom, household expenses,
service obligations, evidence-backed estate income, legal access
to land, marriage and inheritance disputes, and consequences
observed in the native game. These systems are NOT implemented
in this v0.71g milestone.

Reference framework:
- Oxford Handbook of European Legal History, "Feudal Law"
- Susan Reynolds, Fiefs and Vassals (Oxford, 1994)
- Cambridge, The Household Knights of King John
- Oxford Handbook of Women and Gender in Medieval Europe

## Live acceptance matrix

1. Open Party -> camp -> illustrated household register.
   Test all five folios, 0/1/6/8 actual party Heroes,
   text overflow, portrait click, Return, Escape.
2. See actual accepted/refused private petition and seven-day
   return. Verify the inspected companion's observed relation,
   recorded goodwill/grievance and original ambition.
3. Save/reload before and after the native relation change.
   No duplicate social entry, payment, or independent AI effect.
4. Complete patron recommendation and ruler's hearing.
   Confirm player clan remains independent until using genuine
   native kingdom entry. No free settlement/kingdom membership.
5. Become a native nonmercenary vassal by normal game action.
   Confirm exactly one witness receipt and correct kingdom ID.
6. After using native Army formation, check a recorded command
   day and no false command when attached to another lord's army.
7. Revisit 1280x720, 1650x927, 1920x1080 layouts and repeat with
   relevant MCM/Harmony/ButterLib/UIExtenderEx versions.
8. Test War Sails land/sea transitions separately.

Do not label this seamless or ultra-realistic solely from CI.

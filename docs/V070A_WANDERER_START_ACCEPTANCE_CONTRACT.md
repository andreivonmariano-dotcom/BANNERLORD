# v0.70a | Wanderer starting-path acceptance contract

**Status:** Proposed single-path implementation and gameplay acceptance plan. No gameplay code changed. This is the first of seven paths in the v0.70 seven-path RFC, stored on draft PR #93.

## Path identity

Wanderer is the baseline **free individual**. The new-game creator displays **Wanderer** first and selected by default, with no employer, patron, oath, military rank, landed title, royal office, bandit command or obligatory troop enlistment. The underlying native `StartType` remains `default`. The native campaign, culture/FaceGen, calendar and initial possessions are preserved except for changes explicitly selected by the user.

No forced noble kinship, no faction allegiance, no invented starting fief, no inherited clan title. The player still owns the technical/native PlayerClan required by the game engine, but that must **not** be described as a recognized noble house or heritable office.

## Ordinary creator fields

1. **Wanderer** is preselected. Description: “A free person without a sworn employer or inherited title. Start with native Sandbox resources and pursue any career.”
2. **Family** uses a simple toggle: None / Create Household. If enabled, offer Spouse, then optional Children, plus optional Siblings. The actual spouse, children and siblings require native marriage, birthday, parentage, clan/party eligibility and two-way references. No arbitrary noble surname or aristocratic birth is granted. Siblings do not grant landed status.
3. **Starting possessions:** Native Default. A permitted Advanced setting changes an explicit gold target, starting kit, or modest troops, subject to native resource checks. No starting fief or influence without a verified native authority.
4. **Companions:** Default **no forced appointed court officers**. Preserve ordinary native wandering NPCs throughout the game world. The native new-game starting party must be inspected in a real session: if native ASO inserts unrelated starter companions, offer **Start Alone** only after the native removal action is verified. Do not delete global wanderers or automatically replace every existing NPC.
5. **Native character:** culture, appearance, skills, banner, difficulty and age remain in their original native screens. Hide Soldier rank, Knight origin, noble-birth position, patron Hero IDs, royal title, faction starting affiliation, special kingdom grants and retinue court office settings.

## Current source audit

- `SandboxOriginProfile.Role` defaults to `Wanderer`; `SandboxRoleConsistencyPolicy.NativeType("Wanderer")` maps to `default`.
- `SandboxNativeStartOptions` explicitly selects native `StartType`. Phase-9 checks demand an independent, fief-free default-clan state.
- `SandboxServicePolicy.DefaultRetinue("Wanderer")` resolves to `Native`: ordinary native starting companions are retained, not automatically fired. This is a known UX choice, not verified proof that zero native companions spawn.
- `SandboxOriginProfile` currently allows spouse, children and siblings for Wanderer, with `SandboxFamilyCreator` allocating actual native Heroes under `RealFamilyEnabled`. A sibling request can synthesize **active** parent Heroes through `EnsureSiblingParents`. The inactive-parent rule implemented for Independent Lord in v0.69i does **not** yet apply to Wanderer. This is a concrete realism and clan-roster audit item.
- The Family screen contains many advanced controls, legacy role/birth settings and retinue modes. They should be removed from the ordinary **Wanderer** experience only under the new creator flag. Preserve legacy profile parsing and legacy save semantics.

## First implementation boundary

1. Add a new-game-only seven-path front-end mapping, starting with Wanderer; do not repurpose old `Role` meanings or save keys.
2. Present only Wanderer-relevant fields. The other six choices stay available through the stable/legacy flow until their own phase is completed. Do **not** display incomplete Soldier/Knight paths as if implemented.
3. Add explicit Wanderer family validation that prevents noble-birth selection, clan-title claims and any unnecessary active generated parents. Preserve true two-parent sibling genealogy and avoid deleting existing native kin.
4. Preserve native companions by default until a real startup roster check establishes whether and how `Start Alone` can safely dismiss them. Never rewrite all world wanderers.
5. Add pure policy tests for defaults, family gating, prohibited title/patron/fief settings and compatible old profiles; then run Windows native-reference compilation and session fixtures.

## Real game acceptance for Wanderer

- New Sandbox -> Wanderer -> no family, native possessions. Verify actual independent PlayerClan, no oath, no fief, no hidden personal superior, phase-9 contract PASS and native appearance/age/difficulty completion.
- Repeat with spouse only; then spouse + two children (appropriate ages), then two siblings with one older sister and one younger brother. Confirm two-way native Hero genealogy and no unrelated active synthetic parents.
- Verify exactly which native companions begin in the party and whether it matches the chosen mode. Do not infer from the creator.
- Verify with and without explicitly modified gold/kit; compare requested values against actual Hero, inventory and party.
- Save / exit / reload and verify stable Hero IDs, family links, possessions and that no second grant or family creation occurs. Resume a campaign and advance 30 real in-game days for passive systems.
- Verify the native creation UI works at 1280×720, 1650×927 and 1920×1080 by actual screenshot and input checks.

**Completion condition:** Wanderer is signed off only after compiled CI plus a real game start, exact native receipts, visual inspection and save/reload. Next path: Outlaw/Bandit. This RFC is not gameplay proof.

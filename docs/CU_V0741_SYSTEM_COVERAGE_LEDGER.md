# CU system coverage and completion ledger
24 November 2026. Source checkpoint 99390af063ac5695076c6728c3d5547e7144db78. Target Bannerlord 1.5.7.125802 / War Sails 1.3.7.125802. Draft PR #100.

## Registry audit
Canonical Data/Feature_Registry.csv expanded from 133 to 148 rows. 78 original-mod references remain IDENTIFIED, not replacement claims. 70 CU rows: 51 COMPILED, 3 earlier GAME_TESTED, 6 IMPLEMENTED_SOURCE, 4 SCOPED, 6 BLOCKED. Earlier game-tested records do not certify this exact candidate. Existing status values are retained rather than silently promoting unrelated features. Added 15 rows covering dialogue, gathering, peer history, seeded variety, director, detail settings, peer narratives, reputation, timing guard and explicitly unfinished capabilities. This is registry and selected source-contract coverage, not an exhaustive end-to-end verification of every implementation.

## New source
- Exact actor and third participant saved for observed peer standing change of at least ten points on a fresh native co-location sample; player excluded from peer pair. Existing one-thread director and 24 receipt bound. Existing twelve-field director records accepted. One participant speaks; no third-person scene, fabricated conspiracy, actual dispute adjudication or native diplomacy action.
- Reputation derived only from two or more repeatedly sampled peers with observations within 90 days. Exact hero identity; no successor inherits personal standing. Shown in gathering report; not an autonomous reputation mutation or kingdom-wide opinion.
- Optional peer-pass Stopwatch timing. A pass exceeding provisional 5 ms pauses optional peer samples until three campaign days later; transient and reset on reload. Timing logged for over-budget passes or every 21 campaign days if a pass occurs. No per-frame hook, no native AI throttle, no measured FPS claim, no hard cap on duration of the current pass. Existing story detail remains authoritative. 
- Four new policy tests written. 14 independent source checks passed; exact-head CI execution pending confirmation.

## Confirmed earlier candidate
7f0ce4d3a1a4b0ce0ab781266e298ff97b78198e / run 45409702291 / artifact 14926892969 (433051 bytes) / DLL SHA256 f5b550d184f70d489b37b909f3b92f0acc079df828194fed096cfa5053aacb7c. 1268 policy checks, 36500 modeled days stress passed, native-reference compile zero warnings/errors, package succeeded. Compiled and gameplay untested. This does not include the new peer narrative/reputation/profiler work.

## Completion gates
1. Maintain exact feature registry paths, code and tests for all approved systems; distinguish source-only, compiled, native game-tested, regression-tested and released.
2. Exercise seven careers, creator, household, estate, service, petitions, group dialogue, gatherings, stories, native save/reload, succession and real action receipts using the existing gameplay checklist. Confirm held civic/economy/garrison/convoy mutations stay off until their acceptance gates; verify role/provider dependencies and exact 41-module stack.
3. Test actual peer story trigger, dead/unavailable third participant, exact protagonist and speaker, expiry, older save, reputation eligibility and sampling pause. Measure live optional pass timings, campaign tick speed, frame times and save size using matching saves/settings. Do not report synthetic policy stress as native performance.
4. Physical multi-agent scenes, physical weddings/feasts/funerals, autonomous romance/friendship/factions, multi-thread generational political intrigue, full historical mod reconstruction and all native acceptance remain unfinished. No merge/release until the agreed gates. Keep the previous compiled candidate distinct from latest source.

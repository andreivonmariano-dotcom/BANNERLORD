# v0.60b | World-first noble birth and replacement household officers

**User's captured session:** `CU-Session-20261120T145512_4274c320-CLOSED.zip`, DLL SHA-256 `7e41207b24933f6a4def2a9ceb65b3946e710ff9796b56d178ee007bd7f3ad03`, matching v0.60a CI run 44755674069.

The real session was a 43-year-old Vlandian Lord with 4 generated siblings, 3 generated children and a 38-year-old spouse. Native starting companions: 2. The selected `RoleBased` mode added a Knight and Household Steward, and the log recorded `native_companions_dismissed=0`. House presented sibling ages before `NobleBirthPosition` at step 34. Native character creation finished without a reported managed exception.

## Corrected flow

- **World Setup:** World scenario, starting role, kingdom, **noble birth position**, and optional exact older native Hero ID. The noble fields appear only for a Lord. The elder-ID field appears in optional choices only for non-independent noble origins. A selected noble birth is persisted with the world plan before native character creation.
- **Native character stages:** Culture, FaceGen, original narrative, custom Age and button-only All Stats, original Banner and Clan Name.
- **House & Fortune:** Spouse, children, generated siblings, retinue selection and starting resources. Generated sibling age is distinct from the background choice of an existing older noble relative. Noble birth is shown in the final House summary, not asked again.
- **Native Review:** Existing game-owned review and completion remain authoritative.
- **Campaign creation:** Verify selected native older noble against actual kingdom, sex, culture, parents and required older sons, then create requested family and court officers. Refuse an impossible royal relationship rather than fabricate a title.

## Retinue correction

The visible modes are `Native` and `RoleBased`. `Native` retains the original game companions. `RoleBased` means **replace** the native starter companions with same-culture court officers appropriate to the selected role. Existing `ReplaceNative` saved plans continue to behave as a replacement alias, but the duplicate choice is no longer shown. The module stages new officer Heroes before dismissing old companions to prevent a failed Hero factory call from destroying the starting roster. It confirms native dismissal and logs original companion count, dismissals and created cultural officers. A native registration failure after dismissal can still leave a partial roster and is reported, not silently retried.

## Verification boundaries

The source contracts and Windows compilation can certify code and packaging only. Run a fresh Lord/Vlandia character: choose noble position **before** native Culture; set sibling count and ages later in House; choose RoleBased; finish; inspect companion identities. The expected log includes `cu_native_companion_snapshot` with original native companions, `cu_cultural_retinue_staged`, `cu_cultural_retinue_replacement`, and `cu_cultural_retinue_status` showing nonzero dismissals and new officers. Repeat with `Native` to confirm no dismissals. Test fallback for unavailable native noble genealogy, cancellation, and save/reload. No in-game test has been performed for v0.60b.

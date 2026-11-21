# v0.63a | Native father/older brothers and personal service lord

**Evidence:** User session CU-Session-20261120T162021_c77c4268-CLOSED.zip contains matching v0.61a DLL SHA-256 da51b2eb24fbdf6f80702bf0b113b5ba819991407c619b17606b3eb817365a25. The final Lord 20-year-old Vlandian SecondSonOfLord start logged cu_dynastic_native_kin_refused and no native parent links. The contract incorrectly reported PASS. A previous Soldier character creation reached House and was abandoned before a recorded campaign start; its reported missing personal allegiance is not proven by that log.

## Lord and royal family genealogy

**SecondSonOfLord / ThirdSonOfLord / SecondSonOfKing / ThirdSonOfKing** now mean a real father who is an existing native clan leader. His actual spouse is the mother. The selected kingdom, culture, sex, parent ages, father age, clan and royal standing must match. The game looks for existing older male offspring sharing both parents, then generates only missing adult older brother Heroes from native same-culture lord source templates in the father's actual clan. All children gain genuine native Father/Mother and parents' Children links. A second son requires exactly one older brother, a third son exactly two. Generation is preflighted before binding the player and partially created brothers are cleaned up on failure. No native lord is cloned or renamed.

The old younger-brother/sister path still requires an older clan leader with real native parents and can refuse if no suitable older native sibling exists. This is intentionally not disguised as a successful start. House previews are provisional until native start has applied.

Actual noble father ID, mother ID, older brother IDs, and selected elder display name are stored in the campaign save. A verified family is named in-game and logged. The final starting-conditions contract now FAILS if a requested noble family is not bound, rather than reporting a misleading resource-only PASS.

## Soldier and Knight service

Native Advanced Start stays authoritative. Soldier is still a **native mercenary-clan start**, not literal rank-and-file enlistment in another Hero's party. Knight is a native vassal start, not a new engine-owned feudal hierarchy. Both get a real named, eligible lord or ruler from their actual starting kingdom, with a native player-to-patron relationship and saved Hero ID/name. Matching native clan leaders are chosen deterministically; the exact Hero ID can be specified in World Advanced settings. A missing lord is an explicit starting-contract failure. The game records personal lord service and preserves native kingdom allegiance; it does not yet issue army AI commands.

**Soldier default retinue: FamilyOnly.** Native starting wanderers are dismissed with native fire actions. No Sergeant or random officer is created. The player may select adult siblings in House, who retain genuine sibling links and join the party under the existing native family code. Advanced settings can request a custom Sergeant with RoleBased, or preserve native wanderers with Native. Knight retains a role-appropriate squire by default. FamilyOnly is also selectable for other roles.

## Acceptance gates

Source regression and policy tests do not emulate game Hero creation. Compile against pinned Bannerlord 1.5.7.125802 references. For the next in-game fresh start, test a 20-year-old Vlandian second son and inspect father, mother, older brother, and native clan for the brother. Test Soldier with zero starting siblings: expect no unrelated companions and a named actual commanding lord. Test Soldier with one requested adult younger brother: expect only the genuine generated sibling. Test Knight: expect named lord/ruler with actual kingdom affiliation, with a squire only when selected. The two roles' personal relationship is not the same as a native "join lord's party as soldier" mechanic. Do not merge PR #88 before gameplay acceptance.

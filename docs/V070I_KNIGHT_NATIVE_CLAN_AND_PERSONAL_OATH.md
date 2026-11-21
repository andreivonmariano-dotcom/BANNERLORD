# v0.70i | Knight native-clan gate and personal oath

**Scope:** Q1 (existing lord's younger sibling) and Q2 (separate sworn house).
**Status:** source candidate on draft PR #93, native game acceptance pending. Do not merge or update the verified installer selection.

## Q1: genuine membership in an existing NPC lord's clan

The main player is represented by `Hero.MainHero` and the special `Clan.PlayerClan`. Sharing an NPC lord's two native parents does not make the player a real member of that lord's clan. Setting `Hero.Clan` alone, replacing `Clan.PlayerClan`, destroying the original player clan or changing the NPC clan leader is not an acceptable shortcut. Each affects party authority, kingdom membership, UI, quests and saved object references.

A metadata-only inspection of the exact pinned `Bannerlord.ReferenceAssemblies 1.5.7.125802` is added in `scripts/Inspect-CU-Knight-Clan-API.ps1`. It reports the public and private surfaces, but does not mutate the game. Review the resulting `knight-clan-api-v157.txt` for a supported, atomic **player joins existing clan without replacing its leader** action. Only a real live new-game and save/reload test can accept this origin.

**Until a verified transition exists, `KnightOrigin=YoungerSibling` must refuse character creation.** Never simulate membership with a pedigree, relation bonus, title string or vassal status. The current fail-closed result remains `native_same_clan_kinship_not_implemented`. This is an explicit scope limit, not a completed feature.

## Q2: independent sworn-house oath

The new `SandboxKnightOathState` is a versioned, strict serialized game-save record under `CU_v070i_knight_personal_oath`. It begins only after the already existing guided Sworn House start has passed its native campaign checks and resolved a real living personal patron. It records player, patron, original patron clan, patron kingdom, pledge day, next muster, completed musters, missed musters and consensual successions.

- **Duties:** A native-proven in-person meeting in the same real settlement as the named patron. Muster opens seven days before the recorded 30-day deadline. One muster resets the next deadline to 30 days after the meeting. Meeting without eligible service or repeating the same day's action earns nothing.
- **Reward:** The real native `ChangeRelationAction` grants +2 standing after each eligible, verified muster. No invented salary, troops, rank, equipment, fief or kingdom membership.
- **Breach:** The first overdue-day transition records exactly one missed muster and calls native relation -3. Further daily ticks do not stack penalties for the same missing muster.
- **Honorable release:** At a confirmed in-person meeting with the original lord, the player can leave by consent. The record becomes `Released` with a small +1 native relation adjustment. The vow does not silently restart.
- **Dishonorable oath break:** A separate confirmation menu is required. Breaking an active oath becomes `Broken` and calls native patron relation -20 once. The clan is not moved or destroyed.
- **Succession:** A confirmed death of the original patron suspends duties in `SuccessionPending`. A real, alive, currently leading lord of the **same native clan and kingdom** must be encountered in person. The player explicitly renews the oath. No automatic lineage creation, office transfer or fake succession.
- **Changed allegiances:** A native patron changing clan or kingdom moves the contract to `Interrupted`, and no phantom obligations or rewards continue. An unresolved patron does not count as dead or absent service.

The in-game `cu_knight_oath` menu appears from town/castle for an eligible saved Knight. All state changes are one-shot; external `sandbox-origin.ini` is not used to rewrite an active campaign oath.

**No military service has been invented.** This is a personal oath with settlement muster obligations and native relationship consequences. It is not formal vassalage, host-party enlistment, forced army attachment, AI-issued quests, a patron-funded payroll, hereditary titles or a complete medieval feudal contract.

## Native acceptance matrix

On a fresh Sworn House campaign, prove correct lord, clan and kingdom, a separate zero-fief `Clan.PlayerClan`, exact save-backed oath and player identity, visible oath menu, a real native settlement meeting, standing before/after, no duplicate action on repeated click, one overdue relation penalty, oath-break confirmation, honorable release and actual save/quit/reload. Validate lord death, native successor election, explicit renewal and interrupted status after allegiance change. Repeat in all chosen cultures, and retest old Knight/Lord/Soldier saves without migration.

The configured CI policy-suite and pinned game-reference compiler constitute **Gate B only**. They do not certify actual Gauntlet display, Campaign time progression, settlement presence, real native actions or save/reload. PR #93 stays draft until real evidence is recorded.

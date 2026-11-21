# v0.67a | People & Lineage: native-style character agency and family council

**Target:** Bannerlord 1.5.7.125802 reference assemblies, v0.66a base, draft PR #88. All new features must remain compatible with vanilla menu navigation, campaign save data and native Hero/fief actions. This document separates implemented controls from features still requiring in-game proof.

## UI layout

Calradia Unified creates a single **CU: People & Lineage** entry in the vanilla town and castle GameMenus, visible only in a standalone CU Sandbox with a recorded successful starting life. It does not replace the native Clan, Kingdom, Encyclopedia, dialogue, party or settlement screens. It adds three short native submenus:

1. **Household register and character ambitions:** Cycle through actual recorded native Heroes. Read their names, occupation, recorded station, culture, clan, native relation, current home evidence and personal ambitions. Unknown residence remains unverified, not invented. Native Clan remains the authority for full genealogical and military lists.
2. **Personal matters and character requests:** Hear a locally present adult's request, provide actual financial support or recognition, or decline. Only current companions in the same real party or Heroes physically staying at the current settlement are eligible. HomeSettlement alone is insufficient to prove presence. The NPC's own agenda determines their request type, with native relation and cooldown eligibility. A native GiveGoldAction performs any gift and native relation actions reflect response. No hypothetical gold is credited. The player is asked rather than automatically charged.
3. **Family council and native estate claims:** View actual birth clan, available real fortifications and cultural custom. Petition for a **secondary** native holding at a birth-clan fortification, then explicitly confirm the action. A negotiated outcome is recorded independently from a completed native property transfer. There are limited petitions and a separate confirmation step.

The menu is native `CampaignGameStarter.AddGameMenu` and `AddGameMenuOption`, not a custom persistent scene overlay. All entry IDs are namespaced `cu_`. Menus return to the originating town or castle.

## Q2: independently motivated people

The save-backed `CU_AGENCY_1` tracks 64 NPCs at most, with one pending request, three lifetime requests per actor, a seventy-day cooldown and thirty-five-day expiry. Only alive adult people whose real Hero is observed in the player's actual party or settlement can petition. Personal ambition selects `HouseholdAid` (150 denars), `LivelihoodAid` (200 denars) or `Recognition` (no denars). Actual financial support calls `GiveGoldAction.ApplyBetweenCharacters` and verifies the payer's gold decreased before recording success. Native relationships change by +2 / +1 for acceptance and -1 / 0 for refusal. In-game no random unattached wanderer can deliver an NPC request. The campaign ledger records that the request happened and whether it was accepted or rejected.

These are limited native-campaign social decisions, not a full NPC decision engine. No offscreen military parties move, no new trade deals spawn, and no constructed dialogue scenes replace vanilla conversation.

## Q3: culturally distinct family decisions and property

`SandboxInheritanceNegotiationPolicy` uses six models from `SandboxCulturalInheritancePolicy` with distinct consent thresholds and increased difficulty for claimants behind multiple older sons. This represents modeled family-court decisions based on the real living clan head's relation to the player, not an invented historical universal law or a native ruler election.

A valid petition needs **verified parentage**, a **dead native father**, a **living native birth-clan head**, an actual **secondary fortification owned by the native birth clan**, at least **two remaining native fortifications**, and **both clans in the same real kingdom**. Independent landless cadets must join the native kingdom by ordinary Bannerlord politics before any grant. The first native holding in deterministic order is reserved for the senior family branch.

Petitioning produces `ClanHeadConsents`, `ClaimContested`, or `ClaimRejected` in `CU_NEGOTIATION_1`, with the exact estate ID, consenting leader ID and original owner clan. **Consent is not itself a native transfer.** The confirmation button revalidates real parentage, father death, current owner, current leader, location, kingdom, holdings and cultural threshold. Only then does it call `ChangeOwnerOfSettlementAction.ApplyByDefault(player,estate)`. The system records completed inheritance only after native `Settlement.OwnerClan` has changed to `Clan.PlayerClan`. Invalidated or out-of-date proposals cannot execute. The system never manually writes `OwnerClan` or bestows unearned king/vassal titles.

This is a limited **secondary-family-estate grant**, not full historical succession legislation. It does not replace the native kingdom fief election, model debts/dowries, reverse dispossessions, or authorize theft of a senior heir's sole holding.

## Testing

The `SandboxLivingAffairsSimulation.Run(120,730)` independent model simulates 120 two-year campaigns, culture-varying petition outcomes, independent NPC requests, gift accounting, denial outcomes, save/reload, expired/limited requests, and prevention of unapproved transfers. Existing 96-seed 3650-day medieval simulation, 40-campaign policy simulation and 850+ policy checks continue in Windows CI.

**Not equivalent to native gameplay:** The simulation cannot prove actual Bannerlord town/castle navigation, clan head presence, future native inheritance elections, Hero gold transfer, party association, the external Serve as Soldier adapter, character portraits, native save persistence or successful property transfer from an actual game session. All require live gameplay reports. Retain PR #88 as draft until these pass.

### Manual acceptance matrix

- Open a town and castle with a CU-generated family. Confirm the single People & Lineage entry, native submenu navigation and vanilla menu return.
- Review named people, observed physical homes and personal goals, then save/reload. No phantom persons or incorrect residence labels.
- Keep an adult sibling in the same native party until a request appears. Verify acceptance subtracts native gold and raises native relation. Verify refusal does not deduct funds.
- Choose NobleCadet, confirm two or more fortifications in the birth clan and father death. Verify petition is blocked before matching kingdom allegiance. At a secondary native family castle, verify contested versus consent outcomes from native clan leader relations.
- Change native ownership, father/leader status, or faction before confirmation. The pending grant must fail safely.
- Confirm one actually approved grant, verify native settlement owner and player clan ownership. Reload and verify it is not repeated.

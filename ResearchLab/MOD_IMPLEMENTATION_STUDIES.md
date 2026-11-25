# Comparative implementation studies
25 November 2026. Source inspection, not proof of compatibility or quality in the installed stack. Original independent design notes only; no copied external code/assets.

## Diplomacy (source)
Pinned b71fe0e93d0bc6cf4c002d45771e75df80c7940d, CC BY-NC-SA 4.0 (LICENSE inspected). Do not copy or combine into CU without a license review.
- https://github.com/DiplomacyTeam/Bannerlord.Diplomacy/blob/b71fe0e93d0bc6cf4c002d45771e75df80c7940d/src/Bannerlord.Diplomacy/CampaignBehaviors/WarExhaustionBehavior.cs : battle/raid/death/capture/war/peace listeners, daily update, save/load manager.
- https://github.com/DiplomacyTeam/Bannerlord.Diplomacy/blob/b71fe0e93d0bc6cf4c002d45771e75df80c7940d/src/Bannerlord.Diplomacy/WarExhaustion/WarExhaustionManager.EventHandling.cs : categories and event records, daily update, peace action path.
- https://github.com/DiplomacyTeam/Bannerlord.Diplomacy/blob/b71fe0e93d0bc6cf4c002d45771e75df80c7940d/src/Bannerlord.Diplomacy/DiplomaticAction/WarPeace/KingdomPeaceAction.cs : computes costs, confirms player decision, applies costs and native MakePeaceAction, transfers specified fiefs and logs results.
Design lesson: state accumulation changes a real decision and its costs. CU should not recreate diplomacy. Family or career memory needs an independent consequential mechanic, not repeated descriptions.

## Banner Kings (source)
R-Vaccari/bannerlord-banner-kings pinned 0c0184662ca8be7c1ad07d9c7f73c9d8e5da22f9. Repository API reports BSD-3-Clause for this revision; upstream provenance and redistribution rights must be verified separately, do not copy.
- https://github.com/R-Vaccari/bannerlord-banner-kings/blob/0c0184662ca8be7c1ad07d9c7f73c9d8e5da22f9/BannerKings/Managers/Court/CouncilData.cs : persisted council membership, competence, administrative costs and recurring position ticks; relation changes and skill experience.
- https://github.com/R-Vaccari/bannerlord-banner-kings/blob/0c0184662ca8be7c1ad07d9c7f73c9d8e5da22f9/BannerKings/Managers/Kingdoms/Council/BKCouncilPositionDecision.cs : decision with candidates, competence-weighted support, costs and applied outcome.
- https://github.com/R-Vaccari/bannerlord-banner-kings/blob/0c0184662ca8be7c1ad07d9c7f73c9d8e5da22f9/BannerKings/Managers/Populations/PopulationData.cs : persistent population classes, settlement-linked data, growth model and population transitions.
Design lesson: appointments have decision rules and ongoing function. Do not introduce a competing council, population economy or artificial wages in CU. Verify provider API/ownership and build complementary mechanics.

## Distinguished Service (historical source)
ttronrud/Bannerlord-DistinguishedService 2284e239be938f3cc73b5ea01ce6029c75bb1d83; GitHub metadata reports MIT. Historical version; do not assume method signatures match 1.5.7.
https://github.com/ttronrud/Bannerlord-DistinguishedService/blob/2284e239be938f3cc73b5ea01ce6029c75bb1d83/PromotionManager.cs : nomination eligibility from battle results and surviving roster, hero promotion, native AddCompanionAction and AddHeroToPartyAction, troop removal, optional gold cost and skill decisions. Review partial failure hazards before any similar work. CU lesson: promotion needs a real trigger and tangible result, not a title ledger.

## Fourberie and Serve as Soldier (public gameplay documentation only)
https://www.nexusmods.com/mountandblade2bannerlord/mods/2969
https://www.nexusmods.com/mountandblade2bannerlord/mods/3242
https://www.nexusmods.com/mountandblade2bannerlord/mods/3242?tab=articles
The latter lists a versioned integration API article. Full source implementation and reusable license not established in this pass. Investigate documented provider API, do not reverse-engineer a private integration or claim it works. Both are external owners: do not build replacement criminal or soldier systems in CU. Study actual user choice, risk and consequences through a controlled gameplay session, not description alone.

## Practical first design review
Observed CU Lord playtest shows technically valid family/gift and relation receipts, but repetitive dialogue, raw technical receipts in player-facing dossier, nominal household titles, and advice without a distinct practical decision. Prioritize removal of debug prose and genuinely useful native/provider-supported actions. The first lab transaction tests a specific native mechanism, not a proposed finished feature. Next study: identify how settlement treasury is consumed, what a governor actually changes in native models, and how to offer a practical household choice without competing with Improved Garrisons. Do not implement a new economy or invent success.

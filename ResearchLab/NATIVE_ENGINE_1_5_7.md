# Installed engine reference | 1.5.7 / NavalDLC 1.3.7
25 November 2026. Observed on authorized Windows installation. Manifest versions Native/SandBox/SandBoxCore/StoryMode/CustomBattle v1.5.7; NavalDLC v1.3.7. Assembly file version 1.0.0.0 does not distinguish game patch levels; fingerprints below identify the inspected bytes. No proprietary binaries or full bodies stored.

| Assembly | SHA-256 |
|---|---|
| TaleWorlds.CampaignSystem | d7e5c67a04ef83964c882fda2a1f20f0e2afaf2bfe31345ed31d1061c495d462 |
| TaleWorlds.Core | cae7bc303c46c3a9489c6c34f8da2c05f72a430c81e218664e2c9771e7db10ec |
| TaleWorlds.MountAndBlade | eaec5c773ac9e936670790b16333bc765f949629b6d10ecd5cf5ea2346bb2406 |
| TaleWorlds.CampaignSystem.ViewModelCollection | 4cbe024692ca9a3619a3b3c90ca6fa5772376b066a82327abf5226b72f05268d |
| TaleWorlds.GauntletUI | 1daf230fa8501cfdfb4ecabb9ae1fb5768fae153b483bc57d550da331971a32a |
| TaleWorlds.SaveSystem | 7c5a1e199007b0125c5091b0e85b1035e7aeafaaaa588e0200fb0d2777a9ae7a |
| TaleWorlds.Library | f5e1d22a2be285eca08efc422e747934f32616142b51ca236204bd4bd3e2f36c |

## Metadata-verified action contracts
- ChangeGovernorAction.Apply(Town fortification, Hero governor). Call-edge inspection of its internal implementation shows Governor setter, immediate/delayed teleport actions, GovernorOf setter, OnGovernorChanged and OnHeroGetsBusy dispatch. Do not promise mere administrative reassignment without considering relocation/busy-state effects. Runtime order and compatibility remain to be tested.
- GiveGoldAction.ApplyForCharacterToSettlement(Hero giverHero, Settlement settlement, int amount, bool disableNotification). Calls native internal transfer; inspected internal call edges include Hero.ChangeHeroGold, SettlementComponent.ChangeGold, native trade dispatch and amount clamping. Do not infer improved food or recruitment. Read actual Hero.Gold and Town.Gold and verify both deltas; reject insufficient funds before action.
- GiveGoldAction.ApplyBetweenCharacters(Hero giverHero, Hero recipientHero, int amount, bool disableNotification).
- ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero, Hero, int, bool); ApplyPlayerRelation(Hero, int, bool, bool). Native relation outcome may differ from requested amount.
- MarriageAction.Apply(Hero, Hero, bool). BeforeHeroesMarried is a pre-event, not proof. Verify both links and native family/clan after.
- AddHeroToPartyAction.Apply(Hero, MobileParty, bool). Verify actual party and roster before/after, no assumed transfer.
- ChangeOwnerOfSettlementAction.ApplyByGift(Settlement, Hero); ApplyByKingDecision(Hero, Settlement); other entry points have distinct contexts. Do not substitute one for another without semantics research.
- MobileParty.SetHeroPartyRole(Hero, PartyRole); GetHeroPartyRoles(Hero); GetEffectiveRoleHolder(PartyRole); CanAssignMoreRolesToHero(Hero). Native role enum includes land specialists Surgeon, Engineer, Scout, Quartermaster and naval Captain, FirstMate, Navigator; preserve all unselected slots. Effective holder can be fallback, not proof of raw assignment.

## Events, exact generic arguments
OnGivenBirthEvent: Hero, List<Hero>, int.
HeroComesOfAgeEvent: Hero.
BeforeHeroesMarried: Hero, Hero, bool.
HeroKilledEvent: Hero, Hero, KillCharacterActionDetail, bool.
OnGovernorChangedEvent: Town, Hero, Hero; native dispatch parameter names fortification, oldGovernor, newGovernor.
SettlementEntered/AfterSettlementEntered/BeforeSettlementEnteredEvent: MobileParty, Settlement, Hero.
OnSettlementLeftEvent: MobileParty, Settlement.
DailyTickEvent: no arguments; DailyTickHeroEvent: Hero; DailyTickTownEvent: Town; DailyTickPartyEvent: MobileParty.
OnSessionLaunchedEvent, OnGameLoadedEvent: CampaignGameStarter; OnGameLoadFinishedEvent: no arguments.
Prefer bounded targeted event processing; do not scan every person every tick.

## State ownership and extension
Hero.Spouse, Father, Mother, GovernorOf, PartyBelongedTo, Gold exposed setters (not permission to bypass native actions). Children getter MBList<Hero>, Age, IsAlive, IsActive, IsPrisoner, CurrentSettlement read-only. Clan.PlayerClan; Clan.Fiefs is MBReadOnlyList<Town>; Clan.Settlements is MBReadOnlyList<Settlement>. Town.Governor, FoodStocks, Prosperity, Security, Loyalty; FoodChange, Gold, GarrisonParty getter available; Gold inherited from Fief. FoodStocks public setter does not prove supply conservation. Settlement.CurrentSettlement, IsCastle, IsTown, OwnerClan, IsUnderSiege available.
CampaignBehaviorBase.RegisterEvents(), SyncData(IDataStore); IDataStore.SyncData<T>(string, ref T) returns bool. CampaignGameStarter.AddGameMenuOption(string menuId, string optionId, string optionText, condition, consequence, bool isLeave, int index, bool isRepeatable, object relatedObject). AddDialogLine/AddPlayerLine take condition and consequence delegates with priority and additional clickable/persuasion conditions. Registering a choice is not proof of an action. Native UI and save semantics still need further research.
 
## Evidence limits
Metadata verified on local assemblies. Selected managed method call references resolved from local IL, not copied decompiled game source. Does not establish all conditional branches, event order under providers, actual gameplay results or engine internals of War Sails, UI, mission and saving. First AssemblyResolve attempt caused a stack overflow in the independent shell process; preloading local TaleWorlds assemblies before reflection succeeded. Do not use recursive unresolved assembly handlers. Repeat with Probe-Native.ps1 in a separate process. No production files modified.
Documentation context (older versions are patterns only): https://moddocs.bannerlord.com/ ; https://docs.bannerlordmodding.com/_csharp-api/campaignsystem/campaignbehaviorbase.html .

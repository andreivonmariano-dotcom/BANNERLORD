using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace CalradiaUnified
{
    public sealed partial class CivicBehavior
    {
        private void RegisterKingdomMenu(CampaignGameStarter starter, string menu)
        {
            starter.AddGameMenuOption(menu, "cu_kingdom_council_briefing",
                "CU: Hear the kingdom council briefing",
                x => CanVisit() && settings.EnableNpcAdvisory && CurrentSettlementInKingdom(),
                x => Safe(ShowKingdomCouncilBriefing), false);
            starter.AddGameMenuOption(menu, "cu_kingdom_levy",
                "CU: Recruit a kingdom levy (gold + influence)",
                x => CanVisit() && CanKingdomLevy(), x => Safe(RecruitKingdomLevy), false);
            starter.AddGameMenuOption(menu, "cu_kingdom_levy_pair",
                "CU: Recruit two kingdom levies (gold + influence)",
                x => CanVisit() && CanKingdomLevyPair(), x => Safe(RecruitKingdomLevyPair), false);
            starter.AddGameMenuOption(menu, "cu_kingdom_mission",
                "CU: Send goodwill mission to ruling clan",
                x => CanVisit() && CanKingdomMission(), x => Safe(PerformKingdomMission), false);
        }
        private void ShowKingdomCouncilBriefing()
        {
            if (!settings.EnableNpcAdvisory || !CurrentSettlementInKingdom()) return;
            var party = MobileParty.MainParty;
            if (party == null) return;
            int days = party.GetNumDaysForFoodToLast();
            int members = party.MemberRoster == null ? 0 : party.MemberRoster.TotalManCount;
            bool fighting = party.MapEvent != null;
            var advice = KingdomAdvisorPolicy.Decide(fighting, false, party.Army != null,
                party.CurrentSettlement != null, days, members,
                settings.CouncilPreferredPartySize, false);
            Notice("Council report: " + advice + ". Party: " + members + " troops, "
                + days + " days of supplies. Depot: " + Stock() + " grain."
                + " Advice only. No automated orders issued.");
        }
        private bool CurrentSettlementInKingdom()
        {
            return CanVisit() && Clan.PlayerClan != null && Clan.PlayerClan.Kingdom != null
                && Settlement.CurrentSettlement.OwnerClan != null
                && Settlement.CurrentSettlement.OwnerClan.Kingdom == Clan.PlayerClan.Kingdom;
        }
        private CivicState.CouncilRecord CurrentLevyRecord()
        {
            return CivicState.Valid(CurrentId()) ? state.CouncilFor(CurrentId()) : null;
        }
        private static Hero SettlementClanLeader()
        {
            var clan = Settlement.CurrentSettlement == null ? null : Settlement.CurrentSettlement.OwnerClan;
            return clan == null ? null : clan.Leader;
        }
        private bool CanKingdomLevy()
        {
            if (!settings.EnableKingdomLevies || !CurrentSettlementInKingdom()) return false;
            var record = CurrentLevyRecord();
            var party = MobileParty.MainParty;
            return record != null && party != null && KingdomPolicy.CanLevy(
                settings.EnableKingdomLevies, CurrentSettlementInKingdom(),
                Clan.PlayerClan != null && Clan.PlayerClan.Kingdom != null,
                LocalRecruit() != null, PlayerGold(),
                Clan.PlayerClan == null ? 0 : (int)Clan.PlayerClan.Influence,
                party.MemberRoster.TotalManCount, settings.RecruitmentMaxPartySize,
                record.LevyToday, settings.KingdomLevyDailyCap,
                settings.KingdomLevyCostGold, settings.KingdomLevyCostInfluence);
        }
        private void RecruitKingdomLevy()
        {
            if (!CanKingdomLevy()) return;
            var recruit = LocalRecruit(); var record = CurrentLevyRecord();
            if (recruit == null || record == null) return;
            MobileParty.MainParty.MemberRoster.AddToCounts(recruit, 1);
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -settings.KingdomLevyCostGold, true);
            ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -settings.KingdomLevyCostInfluence);
            record.LevyToday++;
            Notice("A kingdom levy joined your party. Gold and influence paid.");
        }
        private bool CanKingdomLevyPair()
        {
            if (!settings.EnableKingdomLevies || !CurrentSettlementInKingdom()) return false;
            var record = CurrentLevyRecord();
            var party = MobileParty.MainParty;
            return record != null && party != null && KingdomPolicy.CanBatchLevy(
                true, CurrentSettlementInKingdom(), Clan.PlayerClan != null && Clan.PlayerClan.Kingdom != null,
                LocalRecruit() != null, PlayerGold(),
                Clan.PlayerClan == null ? 0 : (int)Clan.PlayerClan.Influence,
                party.MemberRoster.TotalManCount, settings.RecruitmentMaxPartySize,
                record.LevyToday, settings.KingdomLevyDailyCap,
                settings.KingdomLevyCostGold, settings.KingdomLevyCostInfluence, 2);
        }
        private void RecruitKingdomLevyPair()
        {
            if (!CanKingdomLevyPair()) return;
            var troop = LocalRecruit(); var record = CurrentLevyRecord();
            if (troop == null || record == null || Hero.MainHero == null) return;
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -(settings.KingdomLevyCostGold * 2), true);
            ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -(settings.KingdomLevyCostInfluence * 2));
            MobileParty.MainParty.MemberRoster.AddToCounts(troop, 2);
            record.LevyToday += 2;
            Notice("Two kingdom levies recruited using gold and clan influence.");
        }
        private bool CanKingdomMission()
        {
            if (!settings.EnableKingdomDiplomacy || !CurrentSettlementInKingdom()) return false;
            var target = SettlementClanLeader();
            var clan = Settlement.CurrentSettlement == null ? null : Settlement.CurrentSettlement.OwnerClan;
            var record = clan == null ? null : state.CouncilFor(clan.StringId);
            return record != null && KingdomPolicy.CanMission(settings.EnableKingdomDiplomacy,
                CurrentSettlementInKingdom(), Clan.PlayerClan != null && Clan.PlayerClan.Kingdom != null,
                target != null && target.IsAlive && target != Hero.MainHero && clan != Clan.PlayerClan,
                PlayerGold(), Clan.PlayerClan == null ? 0 : (int)Clan.PlayerClan.Influence,
                record.MissionCooldown, settings.DiplomaticMissionCostGold,
                settings.DiplomaticMissionCostInfluence);
        }
        private void PerformKingdomMission()
        {
            if (!CanKingdomMission()) return;
            var leader = SettlementClanLeader();
            var clan = Settlement.CurrentSettlement.OwnerClan;
            var record = state.CouncilFor(clan.StringId);
            if (leader == null || record == null) return;
            ChangeRelationAction.ApplyPlayerRelation(leader, 1, true);
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -settings.DiplomaticMissionCostGold, true);
            ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -settings.DiplomaticMissionCostInfluence);
            record.MissionCooldown = settings.DiplomaticMissionCooldownDays;
            Notice("Goodwill mission improved relations with " + leader.Name + ".");
        }
        private void TickKingdomCouncil()
        {
            foreach (var r in state.Council)
            {
                r.LevyToday = 0;
                if (r.MissionCooldown > 0) r.MissionCooldown--;
            }
        }
    }
}

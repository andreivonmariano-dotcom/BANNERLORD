using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaUnified
{
    /// <summary>
    /// One unique, named Wanderer friend from a same-culture native Hero
    /// template. No existing person is recruited, rewritten or destroyed.
    /// The companion is not a spouse, sibling, noble or court officer.
    /// An opt-in must never create two companions by accident.
    /// </summary>
    public static class SandboxWandererCompanion
    {
        private static string PrimarySkill(string specialty)
        {
            switch(specialty)
            {
                case "Guard":return "OneHanded";
                case "Scout":return "Scouting";
                case "Healer":return "Medicine";
                case "Steward":return "Steward";
                case "Trader":return "Trade";
                default:return "";
            }
        }

        public static string TryCreate(SandboxOriginProfile profile,
            Hero player,out Hero created)
        {
            created=null;
            if(profile==null || !profile.WandererNoHouseStart ||
                profile.Role!="Wanderer")
                return "not_a_new_wanderer";
            if(!profile.WandererCompanionEnabled)
                return "not_requested";
            if(player==null || player.Culture==null ||
                Clan.PlayerClan==null || MobileParty.MainParty==null)
                return "refused:player_clan_party_or_culture_missing";
            if(player.Name.ToString()==profile.WandererCompanionName)
                return "refused:companion_name_matches_player";
            if(Clan.PlayerClan.Companions.Count!=0)
                return "refused:native_companions_not_cleared";
            bool female=profile.WandererCompanionGender=="Female";
            string skillId=PrimarySkill(profile.WandererCompanionSpecialty);
            var skill=SandboxNativeSkillCatalog.Find(skillId);
            if(String.IsNullOrEmpty(skillId) || skill==null)
                return "refused:specialty_native_skill_missing";

            // Do not take a living, named native Wanderer from the world
            // or clone a living lord's actual clan position.
            var source=CharacterObject.All.Where(c=>c!=null &&
                c.IsTemplate && !c.IsBasicTroop &&
                c.Culture==player.Culture && c.IsFemale==female &&
                (c.Occupation==Occupation.Wanderer ||
                 c.Occupation==Occupation.Lord))
                .OrderBy(c=>c.Occupation==Occupation.Wanderer?0:1)
                .ThenBy(c=>c.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
            if(source==null)
                source=Hero.AllAliveHeroes.Where(h=>h!=null &&
                    h!=player && h.IsActive && h.IsLord &&
                    h.CharacterObject!=null &&
                    h.Culture==player.Culture && h.IsFemale==female)
                    .OrderBy(h=>h.StringId,StringComparer.Ordinal)
                    .Select(h=>h.CharacterObject)
                    .FirstOrDefault();
            if(source==null)
                return "refused:same_culture_native_source_missing";

            Hero provisional=null;
            bool registered=false;
            try
            {
                provisional=HeroCreator.CreateSpecialHero(source,null,
                    Clan.PlayerClan,null,profile.WandererCompanionAge);
                if(provisional==null)
                    throw new InvalidOperationException(
                        "Native companion factory returned null");
                provisional.SetName(
                    new TextObject(profile.WandererCompanionName),
                    new TextObject(profile.WandererCompanionName));
                provisional.Culture=player.Culture;
                provisional.SetBirthDay(CampaignTime.YearsFromNow(
                    -profile.WandererCompanionAge));
                if(!SandboxFamilyAgePolicy.MatchesRequestedAge(
                    provisional.Age,profile.WandererCompanionAge))
                    throw new InvalidOperationException(
                        "Companion age was not applied");
                provisional.SetNewOccupation(Occupation.Wanderer);
                if(!provisional.IsActive)
                    provisional.ChangeState(Hero.CharacterStates.Active);
                if(provisional.HeroDeveloper==null)
                    throw new InvalidOperationException(
                        "Companion has no native development model");
                provisional.HeroDeveloper.SetInitialSkillLevel(skill,
                    profile.WandererCompanionSkill);
                if(provisional.GetSkillValue(skill)!=
                    profile.WandererCompanionSkill)
                    throw new InvalidOperationException(
                        "Exact companion specialty skill not established");

                // Native same-culture equipment from the donor template.
                if(provisional.BattleEquipment!=null &&
                   source.BattleEquipments!=null)
                {
                    var gear=source.BattleEquipments.FirstOrDefault();
                    if(gear!=null)
                        for(int slot=0;slot<
                            (int)EquipmentIndex.NumEquipmentSetSlots;slot++)
                            provisional.BattleEquipment[slot]=gear[slot];
                }

                AddCompanionAction.Apply(Clan.PlayerClan,provisional);
                registered=Clan.PlayerClan.Companions.Contains(provisional) ||
                    provisional.CompanionOf==Clan.PlayerClan;
                AddHeroToPartyAction.Apply(
                    provisional,MobileParty.MainParty,true);
                // A genuine player relationship is stored on the native
                // Hero and measured, not asserted in a UI description.
                int delta=100-player.GetRelation(provisional);
                if(delta!=0)
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                        player,provisional,delta,false);
                // The player's requested starting friendship is exact.
                // The public native personal-relation setter avoids
                // inventing a family or modifying a patron's clan.
                if(player.GetRelation(provisional)!=100)
                    player.SetPersonalRelation(provisional,100);
                if(provisional.GetRelation(player)!=100)
                    provisional.SetPersonalRelation(player,100);
                if(player.GetRelation(provisional)!=100 ||
                   provisional.GetRelation(player)!=100 ||
                   Clan.PlayerClan.Companions.Count!=1 ||
                   !Clan.PlayerClan.Companions.Contains(provisional) ||
                   provisional.CompanionOf!=Clan.PlayerClan ||
                   provisional.Clan!=Clan.PlayerClan ||
                   MobileParty.MainParty.MemberRoster.GetTroopCount(
                       provisional.CharacterObject)!=1)
                    throw new InvalidOperationException(
                        "Native companion party/relationship not confirmed");
                created=provisional;
                DiagnosticJournal.Event("cu_wanderer_unique_companion",
                    "result=PASS|id="+created.StringId+
                    "|name="+created.Name.ToString()+
                    "|age="+created.Age+
                    "|gender="+profile.WandererCompanionGender+
                    "|culture="+created.Culture.StringId+
                    "|specialty="+profile.WandererCompanionSpecialty+
                    "|skill="+skillId+
                    "|requested_skill="+profile.WandererCompanionSkill+
                    "|actual_skill="+created.GetSkillValue(skill)+
                    "|player_relation="+player.GetRelation(created)+
                    "|reverse_relation="+created.GetRelation(player)+
                    "|native_companion_count="+
                       Clan.PlayerClan.Companions.Count+
                    "|wage_payment_policy=player_daily_reimbursement"+
                    "|native_wage_not_mutated=true");
                return "created:"+created.StringId;
            }
            catch(Exception failure)
            {
                DiagnosticJournal.Exception(
                    "cu_wanderer_unique_companion_failed",failure);
                if(provisional!=null && !registered &&
                    provisional.CompanionOf==null &&
                    provisional.PartyBelongedTo==null &&
                    provisional.StayingInSettlement==null)
                {
                    try
                    {
                        DisableHeroAction.Apply(provisional);
                        if(provisional.IsDisabled)
                            provisional.Clan=null;
                    }
                    catch(Exception cleanupError)
                    {
                        DiagnosticJournal.Exception(
                            "cu_wanderer_companion_cleanup_failed",
                            cleanupError);
                    }
                }
                return "partial:companion="+
                    (provisional==null?"none":provisional.StringId)+
                    "|registered="+registered+
                    "|reason="+failure.GetType().Name;
            }
        }
    }
}

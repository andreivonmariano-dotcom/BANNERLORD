using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
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

        private static void ApplyCustomStats(
            SandboxOriginProfile profile,Hero companion)
        {
            var flags=BindingFlags.Public|BindingFlags.Static;
            foreach(var entry in profile.WandererCompanionOverrides)
            {
                if(!SandboxStatBudgetPolicy.InRange(
                    entry.Key,entry.Value))
                    throw new InvalidOperationException(
                        "Invalid companion stat: "+entry.Key);
                string[] parts=entry.Key.Split('.');
                if(parts.Length!=2)
                    throw new InvalidOperationException(
                        "Companion stat key invalid: "+entry.Key);
                int requested=entry.Value;
                if(parts[0]=="Skill" || parts[0]=="Focus")
                {
                    var skill=SandboxNativeSkillCatalog.Find(parts[1]);
                    if(skill==null)
                        throw new InvalidOperationException(
                            "Companion native skill unavailable: "+parts[1]);
                    if(parts[0]=="Skill")
                    {
                        companion.HeroDeveloper.SetInitialSkillLevel(
                            skill,requested);
                        if(companion.GetSkillValue(skill)!=requested)
                            throw new InvalidOperationException(
                                "Companion skill override failed");
                    }
                    else
                    {
                        int delta=requested-
                            companion.HeroDeveloper.GetFocus(skill);
                        if(delta!=0)
                            companion.HeroDeveloper.AddFocus(
                                skill,delta,false);
                        if(companion.HeroDeveloper.GetFocus(skill)!=requested)
                            throw new InvalidOperationException(
                                "Companion focus override failed");
                    }
                }
                else if(parts[0]=="Attribute")
                {
                    var info=typeof(DefaultCharacterAttributes)
                        .GetProperty(parts[1],flags);
                    var attr=info==null?null:
                        info.GetValue(null,null) as CharacterAttribute;
                    if(attr==null)
                        throw new InvalidOperationException(
                            "Companion native attribute missing");
                    int delta=requested-
                        companion.GetAttributeValue(attr);
                    if(delta!=0)
                        companion.HeroDeveloper.AddAttribute(
                            attr,delta,false);
                    if(companion.GetAttributeValue(attr)!=requested)
                        throw new InvalidOperationException(
                            "Companion attribute override failed");
                }
                else if(parts[0]=="Trait")
                {
                    var info=typeof(DefaultTraits).GetProperty(parts[1],flags);
                    var trait=info==null?null:
                        info.GetValue(null,null) as TraitObject;
                    if(trait==null)
                        throw new InvalidOperationException(
                            "Companion native trait missing");
                    companion.SetTraitLevel(trait,requested);
                    if(companion.GetTraitLevel(trait)!=requested)
                        throw new InvalidOperationException(
                            "Companion trait override failed");
                }
                else if(parts[0]=="Level" && parts[1]=="Character")
                {
                    if(!SandboxFamilyLevelPolicy.Valid(requested))
                        throw new InvalidOperationException(
                            "Companion level outside native range");
                    companion.Level=requested;
                    companion.HeroDeveloper.SetInitialLevel(requested);
                    if(companion.Level!=requested)
                        throw new InvalidOperationException(
                            "Companion level override failed");
                }
                else throw new InvalidOperationException(
                    "Companion override type unsupported: "+entry.Key);
                DiagnosticJournal.Event("cu_wanderer_companion_stat_applied",
                    "hero="+companion.StringId+
                    "|key="+entry.Key+
                    "|requested="+requested+
                    "|native_verified=true");
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
            var random=new Random(Guid.NewGuid().GetHashCode());
            var candidates=CharacterObject.All.Where(c=>c!=null &&
                c.IsTemplate && !c.IsBasicTroop &&
                c.Culture==player.Culture && c.IsFemale==female &&
                (c.Occupation==Occupation.Wanderer ||
                 c.Occupation==Occupation.Lord))
                .OrderBy(c=>c.Occupation==Occupation.Wanderer?0:1)
                .ThenByDescending(c=>c.GetSkillValue(skill))
                .ThenBy(c=>c.StringId,StringComparer.Ordinal)
                .Take(6).ToArray();
            var source=candidates.Length==0?null:candidates[random.Next(candidates.Length)];
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
                // The same editor used for the player's own native stats
                // now edits this independent companion override dictionary.
                foreach(string supporting in SandboxRetinueOfficePolicy.WandererSupportSkills(profile.WandererCompanionSpecialty))
                {
                    var support=SandboxNativeSkillCatalog.Find(supporting);
                    if(support!=null && support!=skill)
                    {
                        int desired=Math.Max(provisional.GetSkillValue(support),
                            Math.Min(80,profile.WandererCompanionSkill*2/3));
                        provisional.HeroDeveloper.SetInitialSkillLevel(support,desired);
                        if(provisional.GetSkillValue(support)!=desired)
                            throw new InvalidOperationException("Companion support skill not established");
                    }
                }
                ApplyCustomStats(profile,provisional);

                string specialty=profile.WandererCompanionSpecialty;
                string style=specialty=="Guard"?"Shieldguard":specialty=="Scout"?"Scout":"Court";
                SandboxCulturalRetinue.EquipForStyle(provisional,source,style,specialty,random);
                SandboxCulturalRetinue.EquipCivilian(provisional,source,specialty,random);

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
                int targetSkill;
                if(!profile.WandererCompanionOverrides.TryGetValue(
                    "Skill."+skillId,out targetSkill))
                    targetSkill=profile.WandererCompanionSkill;
                if(player.GetRelation(provisional)!=100 ||
                   provisional.GetRelation(player)!=100 ||
                   provisional.GetSkillValue(skill)!=targetSkill ||
                   provisional.IsFemale!=female ||
                   Clan.PlayerClan.Companions.Count!=1 ||
                   !Clan.PlayerClan.Companions.Contains(provisional) ||
                   provisional.CompanionOf!=Clan.PlayerClan ||
                   provisional.Clan!=Clan.PlayerClan ||
                   MobileParty.MainParty.MemberRoster.GetTroopCount(
                       provisional.CharacterObject)!=1)
                    throw new InvalidOperationException(
                        "Native companion party/relationship not confirmed");
                provisional.EncyclopediaText=new TextObject(provisional.Name+" travels with "+player.Name+
                    " as a "+SandboxRetinueOfficePolicy.CompanionProfession(profile.WandererCompanionSpecialty).ToLowerInvariant()+".");
                created=provisional;
                DiagnosticJournal.Event("cu_wanderer_unique_companion",
                    "result=PASS|id="+created.StringId+
                    "|name="+created.Name.ToString()+
                    "|age="+created.Age+
                    "|gender="+profile.WandererCompanionGender+
                    "|culture="+created.Culture.StringId+
                    "|specialty="+profile.WandererCompanionSpecialty+
                    "|skill="+skillId+
                    "|requested_skill="+targetSkill+
                    "|actual_skill="+created.GetSkillValue(skill)+
                    "|manual_stat_overrides="+
                        profile.WandererCompanionOverrides.Count+
                    "|player_relation="+player.GetRelation(created)+
                    "|reverse_relation="+created.GetRelation(player)+
                    "|native_companion_count="+
                       Clan.PlayerClan.Companions.Count+
                    "|wage_payment_policy=native_only"+
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

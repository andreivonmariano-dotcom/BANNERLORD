using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaUnified
{
    /// <summary>
    /// Starting officers are NEW, named heroes cloned from same-culture
    /// native LORD templates. No existing native noble is recruited or
    /// taken from their faction. RoleBased replaces native starters.
    /// ReplaceNative remains a legacy saved-profile alias.
    /// </summary>
    public static class SandboxCulturalRetinue
    {
        private static int SourceMatch(CharacterObject character,
            string office)
        {
            if(character==null)return 0;
            var skills=SandboxRetinueOfficePolicy.Skills(office);
            int score=0;
            for(int i=0;i<skills.Length;i++)
            {
                var skill=SandboxNativeSkillCatalog.Find(skills[i]);
                if(skill!=null)score+=
                    character.GetSkillValue(skill)*(skills.Length-i);
            }
            return score;
        }

        private static CharacterObject FindNobleSource(
            Hero player,bool female,string office)
        {
            // Prefer native same-culture, sex-matched donors with the
            // strongest real training for this particular household post.
            var preferred=CharacterObject.All.Where(c=>c!=null &&
                c.IsTemplate && !c.IsBasicTroop &&
                c.Culture==player.Culture &&
                c.Occupation==Occupation.Lord &&
                c.IsFemale==female)
                .OrderByDescending(c=>SourceMatch(c,office))
                .ThenBy(c=>c.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
            if(preferred!=null)return preferred;
            var donor=Hero.AllAliveHeroes.Where(h=>h!=null &&
                h!=player && h.IsActive && h.IsLord &&
                h.Culture==player.Culture &&
                h.IsFemale==female && h.CharacterObject!=null)
                .OrderByDescending(h=>SourceMatch(h.CharacterObject,office))
                .ThenBy(h=>h.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
            return donor==null?null:donor.CharacterObject;
        }

        private static string OfficerName(string culture,
            string office,int index)
        {
            bool female=office=="Chamberlain" || office=="Navigator";
            return SandboxCulturalNames.Suggest(culture,female,index+2,
                new string[0])+" ("+office.Replace("Of"," of ")+")";
        }

        private static void EquipFromNativeDonor(Hero officer,
            CharacterObject source,string office)
        {
            if(officer==null || officer.BattleEquipment==null ||
                source==null || source.BattleEquipments==null)return;
            var available=source.BattleEquipments.ToArray();
            if(available.Length==0)return;
            // Official game assets only. Prefer the first combat kit
            // for field officers, later native outfit for court officers.
            var selected=available[
                SandboxRetinueOfficePolicy.IsFieldOfficer(office)?
                    0:available.Length-1];
            if(selected==null)return;
            for(int slot=0;slot<(int)EquipmentIndex.NumEquipmentSetSlots;slot++)
                officer.BattleEquipment[slot]=selected[slot];
            DiagnosticJournal.Event("cu_retinue_native_gear",
                "hero="+officer.StringId+"|office="+office+
                "|source="+source.StringId+"|native_gear=true");
        }

        private static void TrainForOffice(Hero officer,string office)
        {
            if(officer==null || officer.HeroDeveloper==null)return;
            var skills=SandboxRetinueOfficePolicy.Skills(office);
            for(int i=0;i<skills.Length;i++)
            {
                var skill=SandboxNativeSkillCatalog.Find(skills[i]);
                if(skill==null)continue;
                int target=i==0?120:i==1?90:70;
                if(officer.GetSkillValue(skill)<target)
                    officer.HeroDeveloper.SetInitialSkillLevel(skill,target);
            }
            if(SandboxRetinueOfficePolicy.IsFieldOfficer(office))
                officer.SetTraitLevel(
                    TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Valor,1);
            else
                officer.SetTraitLevel(
                    TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits.Honor,1);
        }

        private static string AssignNativeDuty(Hero officer,string office)
        {
            var party=MobileParty.MainParty;
            if(party==null)return "party_missing";
            string duty=SandboxRetinueOfficePolicy.PartyDuty(office);
            if(duty=="Quartermaster")
                party.SetPartyQuartermaster(officer);
            else if(duty=="Scout")
                party.SetPartyScout(officer);
            return duty=="None"?"combat_or_household":
                "native_"+duty.ToLowerInvariant();
        }

        private static bool HasBrother(Hero player)
        {
            var party=MobileParty.MainParty;
            return party!=null && player.Father!=null &&
                player.Father.Children!=null &&
                player.Father.Children.Any(h=>h!=null && h!=player &&
                    h.IsAlive && h.Age>=18 && !h.IsFemale &&
                    h.Mother==player.Mother &&
                    h.Clan==Clan.PlayerClan &&
                    h.CharacterObject!=null &&
                    party.MemberRoster.GetTroopCount(
                        h.CharacterObject)>0);
        }

        public static string TryCreate(SandboxOriginProfile profile,
            Hero player)
        {
            if(profile==null || player==null ||
                profile.RetinueMode=="Native")
                return "native_companions_preserved";
            if(Clan.PlayerClan==null ||
                MobileParty.MainParty==null ||
                player.Culture==null)
                return "refused:clan_party_or_culture_missing";
            string[] offices=SandboxNobleStartPolicy.RetinueOffices(
                profile.Role,HasBrother(player));
            if(offices.Length==0)
                return "refused:no_retinue_for_role";
            // RoleBased is an actual replacement, not an additive court.
            // ReplaceNative is retained as a legacy saved-profile alias.
            bool replace=profile.RetinueMode=="RoleBased" ||
                profile.RetinueMode=="ReplaceNative";
            var existing=Clan.PlayerClan.Companions.Where(
                h=>h!=null && h!=player).ToArray();
            // Do not silently add new officers on top of an original
            // companion that native Fire cannot legally dismiss.
            if(replace && existing.Any(old=>
                old.Clan!=Clan.PlayerClan ||
                old.CompanionOf!=Clan.PlayerClan))
                return "refused:native_companion_not_firable";
            var sources=new CharacterObject[offices.Length];
            for(int i=0;i<offices.Length;i++)
            {
                bool female=offices[i]=="Chamberlain" ||
                    offices[i]=="Navigator";
                sources[i]=FindNobleSource(player,female,offices[i]);
                if(sources[i]==null)
                {
                    DiagnosticJournal.Event(
                        "cu_cultural_retinue_refused",
                        "reason=same_culture_lord_source_missing"+
                        "|office="+offices[i]+
                        "|culture="+player.Culture.StringId+
                        "|no_random_wanderer_used=true");
                    return "refused:local_noble_source_missing:"+offices[i];
                }
            }
            int created=0;
            int replaced=0;
            var staged=new List<Hero>();
            try
            {
                // Create all replacement Heroes before firing any original
                // companions. A failed source/factory allocation must not
                // dismiss the player's native roster.
                for(int i=0;i<offices.Length;i++)
                {
                    string name=OfficerName(player.Culture.StringId,
                        offices[i],i);
                    var officer=HeroCreator.CreateSpecialHero(
                        sources[i],null,Clan.PlayerClan,null,27+i*3);
                    if(officer==null)
                        throw new InvalidOperationException(
                            "Native HeroCreator refused retinue officer");
                    staged.Add(officer);
                    officer.SetName(new TextObject(name),
                        new TextObject(name));
                    officer.Culture=player.Culture;
                    officer.SetBirthDay(CampaignTime.YearsFromNow(
                        -(27+i*3)));
                    officer.SetNewOccupation(Occupation.Lord);
                    if(!officer.IsActive)
                        officer.ChangeState(Hero.CharacterStates.Active);
                    EquipFromNativeDonor(officer,sources[i],offices[i]);
                    TrainForOffice(officer,offices[i]);
                }
                DiagnosticJournal.Event("cu_cultural_retinue_staged",
                    "mode="+profile.RetinueMode+
                    "|requested_officers="+offices.Length+
                    "|staged="+staged.Count+
                    "|native_companions_before="+existing.Length);
                if(replace)
                {
                    // Only the explicitly selected custom retinue path
                    // replaces native starting companions.
                    foreach(var old in existing)
                    {
                        RemoveCompanionAction.ApplyByFire(
                            Clan.PlayerClan,old);
                        if(old.CompanionOf!=Clan.PlayerClan &&
                            !Clan.PlayerClan.Companions.Contains(old))
                            replaced++;
                        else
                            throw new InvalidOperationException(
                                "Native companion dismissal was not confirmed");
                    }
                }

                for(int i=0;i<staged.Count;i++)
                {
                    var officer=staged[i];
                    AddCompanionAction.Apply(Clan.PlayerClan,officer);
                    AddHeroToPartyAction.Apply(
                        officer,MobileParty.MainParty,true);
                    bool confirmed=Clan.PlayerClan.Companions
                        .Contains(officer) &&
                        officer.Culture==player.Culture &&
                        officer.Clan==Clan.PlayerClan &&
                        MobileParty.MainParty.MemberRoster
                            .GetTroopCount(officer.CharacterObject)>0;
                    if(!confirmed)
                        throw new InvalidOperationException(
                            "Retinue officer not registered as clan companion");
                    created++;
                    string duty="unassigned";
                    try
                    {
                        duty=AssignNativeDuty(officer,offices[i]);
                    }
                    catch(Exception roleError)
                    {
                        DiagnosticJournal.Exception(
                            "cu_cultural_retinue_duty_failed",roleError);
                        duty="native_assignment_failed";
                    }
                    DiagnosticJournal.Event("cu_cultural_retinue_officer",
                        "id="+officer.StringId+
                        "|office="+offices[i]+
                        "|culture="+officer.Culture.StringId+
                        "|occupation="+officer.Occupation+
                        "|source_native_lord=true"+
                        "|source="+sources[i].StringId+
                        "|duty="+duty+
                        "|clan_companion=true"+
                        "|party_member=true");
                }
                DiagnosticJournal.Event("cu_cultural_retinue_replacement",
                    "mode="+profile.RetinueMode+
                    "|native_companions_before="+existing.Length+
                    "|native_companions_dismissed="+replaced+
                    "|cultural_officers_created="+created);
                return "officers="+created+
                    "|native_companions_dismissed="+replaced+
                    "|same_culture=true";
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception(
                    "cu_cultural_retinue_creation_failed",e);
                // Reclaim staged Heroes that were never registered as
                // companions, rather than leaving inactive court orphans.
                foreach(var pending in staged)
                {
                    if(pending==null ||
                        Clan.PlayerClan.Companions.Contains(pending))
                        continue;
                    try
                    {
                        DisableHeroAction.Apply(pending);
                        pending.Clan=null;
                    }
                    catch(Exception cleanupError)
                    {
                        DiagnosticJournal.Exception(
                            "cu_cultural_retinue_stage_cleanup_failed",
                            cleanupError);
                    }
                }
                return "partial:officers="+created+
                    "|native_companions_dismissed="+replaced+
                    "|reason="+e.GetType().Name;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
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
        private static int EquipmentScore(Equipment equipment,
            string style)
        {
            if(equipment==null)return -1000;
            var types=new List<string>();
            for(int slot=0;slot<(int)EquipmentIndex.NumEquipmentSetSlots;slot++)
            {
                var item=equipment[slot].Item;
                if(item!=null)types.Add(item.ItemType.ToString());
            }
            return SandboxRetinueCulturePolicy.EquipmentScore(
                style,types.ToArray());
        }

        private static int SourceMatch(CharacterObject character,
            string culture,string office)
        {
            if(character==null)return 0;
            var skills=SandboxRetinueCulturePolicy.Training(
                culture,office);
            int score=0;
            for(int i=0;i<skills.Length;i++)
            {
                var skill=SandboxNativeSkillCatalog.Find(skills[i]);
                if(skill!=null)score+=
                    character.GetSkillValue(skill)*(skills.Length-i);
            }
            if(character.BattleEquipments!=null)
            {
                string style=SandboxRetinueCulturePolicy.GearStyle(
                    culture,office);
                var gear=character.BattleEquipments.ToArray();
                if(gear.Length>0)
                    score+=Math.Max(0,gear.Max(e=>
                        EquipmentScore(e,style)))*8;
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
                .OrderByDescending(c=>SourceMatch(
                    c,player.Culture.StringId,office))
                .ThenBy(c=>c.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
            if(preferred!=null)return preferred;
            var donor=Hero.AllAliveHeroes.Where(h=>h!=null &&
                h!=player && h.IsActive && h.IsLord &&
                h.Culture==player.Culture &&
                h.IsFemale==female && h.CharacterObject!=null)
                .OrderByDescending(h=>SourceMatch(
                    h.CharacterObject,player.Culture.StringId,office))
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
            // Use only real installed native items. The preferred kit
            // depends on both culture and office, not donor array order.
            string style=SandboxRetinueCulturePolicy.GearStyle(
                officer.Culture.StringId,office);
            var selected=available.OrderByDescending(e=>
                EquipmentScore(e,style)).FirstOrDefault();
            if(selected==null)return;
            for(int slot=0;slot<(int)EquipmentIndex.NumEquipmentSetSlots;slot++)
                officer.BattleEquipment[slot]=selected[slot];
            DiagnosticJournal.Event("cu_retinue_native_gear",
                "hero="+officer.StringId+"|office="+office+
                "|source="+source.StringId+"|native_gear=true"+
                "|style="+style+
                "|gear_score="+EquipmentScore(selected,style));
        }

        private static void ApplyPersonality(Hero officer,
            string culture,string office)
        {
            var first=SandboxRetinueCulturePolicy.PrimaryTrait(
                culture,office);
            var second=SandboxRetinueCulturePolicy.SecondaryTrait(
                culture,office);
            foreach(var name in new[]{first,second}.Distinct())
            {
                var prop=typeof(
                    TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultTraits)
                    .GetProperty(name,
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Static);
                var trait=prop==null?null:
                    prop.GetValue(null,null) as TraitObject;
                if(trait==null)
                    throw new InvalidOperationException(
                        "Native officer trait unavailable: "+name);
                int level=SandboxRetinueCulturePolicy.TraitLevel(
                    name,culture,office);
                officer.SetTraitLevel(trait,level);
            }
            DiagnosticJournal.Event("cu_retinue_personality",
                "hero="+officer.StringId+"|office="+office+
                "|culture="+culture+
                "|primary="+first+"|secondary="+second);
        }

        private static void TrainForOffice(Hero officer,string office)
        {
            if(officer==null || officer.HeroDeveloper==null)return;
            string culture=officer.Culture.StringId;
            var skills=SandboxRetinueCulturePolicy.Training(
                culture,office);
            for(int i=0;i<skills.Length;i++)
            {
                var skill=SandboxNativeSkillCatalog.Find(skills[i]);
                if(skill==null)continue;
                int target=i==0?125:i==1?105:80;
                if(officer.GetSkillValue(skill)<target)
                    officer.HeroDeveloper.SetInitialSkillLevel(skill,target);
            }
            ApplyPersonality(officer,culture,office);
            DiagnosticJournal.Event("cu_retinue_cultural_training",
                "hero="+officer.StringId+"|office="+office+
                "|culture="+culture+
                "|skills="+String.Join(",",skills)+
                "|style="+SandboxRetinueCulturePolicy.GearStyle(
                    culture,office));
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
            if(profile.RetinueMode=="FamilyOnly")
            {
                // Zero additional officers. The player's explicitly
                // selected brothers and sisters are clan family, never
                // part of the native starting-wanderer dismissal list.
                var native=Clan.PlayerClan.Companions.Where(
                    h=>h!=null && h!=player).ToArray();
                if(native.Any(h=>h.Clan!=Clan.PlayerClan ||
                    h.CompanionOf!=Clan.PlayerClan))
                    return "refused:native_companion_not_firable";
                // Native Fire operates one companion at a time. Validate
                // all candidates first and check EVERY confirmed removal.
                // A partial native mutation is reported as partial rather
                // than a fabricated atomic success.
                if(native.Length>16)
                    return "refused:native_companion_roster_too_large";
                int fired=0;
                try
                {
                    foreach(var old in native)
                    {
                        RemoveCompanionAction.ApplyByFire(
                            Clan.PlayerClan,old);
                        if(old.CompanionOf==Clan.PlayerClan ||
                            Clan.PlayerClan.Companions.Contains(old))
                            throw new InvalidOperationException(
                                "Native companion was not dismissed");
                        fired++;
                    }
                    if(fired!=native.Length || native.Any(old=>
                        old.CompanionOf==Clan.PlayerClan ||
                        Clan.PlayerClan.Companions.Contains(old)))
                        throw new InvalidOperationException(
                            "Not all native wanderers were dismissed");
                    DiagnosticJournal.Event("cu_cultural_retinue_replacement",
                        "mode=FamilyOnly"+
                        "|native_companions_before="+native.Length+
                        "|native_companions_dismissed="+fired+
                        "|cultural_officers_created=0"+
                        "|requested_family_heroes_preserved=true");
                    return "officers=0|native_companions_dismissed="+fired+
                        "|family_only=true";
                }
                catch(Exception ex)
                {
                    DiagnosticJournal.Exception(
                        "cu_family_only_retinue_failed",ex);
                    return "partial:native_companions_dismissed="+fired+
                        "|reason="+ex.GetType().Name;
                }
            }
            string[] offices=SandboxNobleStartPolicy.RetinueOffices(
                profile.Role,HasBrother(player));
            // Keep the original native court for historical King saves.
            // Only the new guided King fixes its three royal officers.
            if(profile.KingGuidedStart)
                offices=SandboxKingStartPolicy.CourtOffices;
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
            if(replace && existing.Length>16)
                return "refused:native_companion_roster_too_large";
            DiagnosticJournal.Event("cu_cultural_retinue_roster_preflight",
                "mode="+profile.RetinueMode+
                "|native_companions_before="+existing.Length+
                "|every_native_companion_firable="+
                    !existing.Any(old=>old.CompanionOf!=Clan.PlayerClan)+
                "|staged_before_dismissal=true"+
                "|native_fire_sequential=true"+
                "|failed_removal_reported_partial=true");
            var sources=new CharacterObject[offices.Length];
            for(int i=0;i<offices.Length;i++)
            {
                bool female=offices[i]=="Chamberlain" ||
                    offices[i]=="RoyalMinister" ||
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
                    // A trade factor, sergeant or navigator does not
                    // become a landed LORD merely by joining a party.
                    // Preserve native lord occupation for a noble
                    // household's appointed mounted combat officer.
                    bool nobleFieldOffice=(profile.Role=="King" ||
                        profile.Role=="Lord" || profile.Role=="Knight") &&
                        SandboxRetinueOfficePolicy.IsFieldOfficer(offices[i]);
                    officer.SetNewOccupation(nobleFieldOffice?
                        Occupation.Lord:Occupation.Wanderer);
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
                for(int i=0;i<staged.Count;i++)
                {
                    var officer=staged[i];
                    AddCompanionAction.Apply(Clan.PlayerClan,officer);
                    bool royalMinister=profile.KingGuidedStart &&
                        offices[i]=="RoyalMinister";
                    string duty="unassigned";
                    if(royalMinister)
                    {
                        // A governor serves at the owned fief, not in the
                        // traveling King's army. The native action is
                        // the sole authority for the appointment.
                        string holding;
                        duty=SandboxKingNativeMinistry.TryAppoint(
                            officer,out holding);
                        if(!duty.StartsWith(
                            "native_royal_minister_governs:",
                            StringComparison.Ordinal) ||
                            !SandboxKingNativeMinistry.VerifiedGovernor(
                                officer))
                            throw new InvalidOperationException(
                                "Royal minister native governor not verified: "+
                                duty);
                    }
                    else
                    {
                        AddHeroToPartyAction.Apply(
                            officer,MobileParty.MainParty,true);
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
                    }
                    bool confirmed=Clan.PlayerClan.Companions
                        .Contains(officer) &&
                        officer.Culture==player.Culture &&
                        officer.Clan==Clan.PlayerClan &&
                        officer.CompanionOf==Clan.PlayerClan &&
                        (royalMinister?
                            SandboxKingNativeMinistry.VerifiedGovernor(
                                officer):
                            MobileParty.MainParty.MemberRoster
                                .GetTroopCount(officer.CharacterObject)>0);
                    if(!confirmed)
                        throw new InvalidOperationException(
                            "Retinue officer native assignment not verified");
                    created++;
                    DiagnosticJournal.Event("cu_cultural_retinue_officer",
                        "id="+officer.StringId+
                        "|office="+offices[i]+
                        "|culture="+officer.Culture.StringId+
                        "|occupation="+officer.Occupation+
                        "|source_native_lord=true"+
                        "|source="+sources[i].StringId+
                        "|style="+SandboxRetinueCulturePolicy.GearStyle(
                            player.Culture.StringId,offices[i])+
                        "|duty="+duty+
                        "|native_occupation="+officer.Occupation+
                        "|clan_companion=true"+
                        "|party_member="+(!royalMinister)+
                        "|native_governor="+royalMinister);
                }
                if(replace)
                {
                    // Verify the full new roster BEFORE the first
                    // irreversible native dismissal. Failure to recruit
                    // any officer leaves original companions in place.
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
                if(replace && (replaced!=existing.Length ||
                    existing.Any(old=>
                        old.CompanionOf==Clan.PlayerClan ||
                        Clan.PlayerClan.Companions.Contains(old))))
                    throw new InvalidOperationException(
                        "Native companion replacement not fully verified");
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
                // A native party or companion assignment is a real
                // game relationship, not an unattached object to delete.
                // Reclaim only new officers that never joined the party.
                // Registered officers need manual native reconciliation.
                int provisionalDisabled=0,registeredPreserved=0;
                foreach(var pending in staged)
                {
                    if(pending==null)continue;
                    if(Clan.PlayerClan.Companions.Contains(pending) ||
                        pending.CompanionOf!=null ||
                        pending.PartyBelongedTo!=null ||
                        pending.StayingInSettlement!=null)
                    {
                        registeredPreserved++;
                        continue;
                    }
                    try
                    {
                        DisableHeroAction.Apply(pending);
                        if(pending.IsDisabled)
                        {
                            pending.Clan=null;
                            provisionalDisabled++;
                        }
                        else registeredPreserved++;
                    }
                    catch(Exception cleanupError)
                    {
                        registeredPreserved++;
                        DiagnosticJournal.Exception(
                            "cu_cultural_retinue_stage_cleanup_failed",
                            cleanupError);
                    }
                }
                DiagnosticJournal.Event(
                    "cu_cultural_retinue_failure_reconciliation",
                    "staged_disabled="+provisionalDisabled+
                    "|registered_preserved="+registeredPreserved+
                    "|native_roster_not_fabricated=true");
                return "partial:officers="+created+
                    "|native_companions_dismissed="+replaced+
                    "|reason="+e.GetType().Name;
            }
        }
    }
}

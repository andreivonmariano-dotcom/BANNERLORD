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
            Hero player,bool female,string office,Random random,HashSet<string> usedSources)
        {
            // Prefer native same-culture, sex-matched donors with the
            // strongest real training for this particular household post.
            var preferred=CharacterObject.All.Where(c=>c!=null &&
                !usedSources.Contains(c.StringId) &&
                c.IsTemplate && !c.IsBasicTroop &&
                c.Culture==player.Culture &&
                c.Occupation==Occupation.Lord &&
                c.IsFemale==female)
                .OrderByDescending(c=>SourceMatch(
                    c,player.Culture.StringId,office))
                .ThenBy(c=>c.StringId,StringComparer.Ordinal)
                .Take(6).ToArray();
            if(preferred.Length>0)return preferred[random.Next(preferred.Length)];
            var donor=Hero.AllAliveHeroes.Where(h=>h!=null &&
                h!=player && h.IsActive && h.IsLord &&
                h.Culture==player.Culture &&
                h.IsFemale==female && h.CharacterObject!=null &&
                !usedSources.Contains(h.CharacterObject.StringId))
                .OrderByDescending(h=>SourceMatch(
                    h.CharacterObject,player.Culture.StringId,office))
                .ThenBy(h=>h.StringId,StringComparer.Ordinal)
                .Take(6).ToArray();
            return donor.Length==0?null:donor[random.Next(donor.Length)].CharacterObject;
        }

        private static string OfficerName(string culture,
            string office,Random random,IEnumerable<string> used)
        {
            bool female=office=="Chamberlain" ||
                office=="RoyalMinister" || office=="Navigator";
            return SandboxCulturalNames.RandomUnused(culture,female,random,used);
        }

        internal static void EquipForStyle(Hero officer,
            CharacterObject source,string style,string office,Random random=null)
        {
            if(officer==null || officer.BattleEquipment==null || source==null || source.BattleEquipments==null)
                throw new InvalidOperationException("Native companion battle equipment unavailable");
            var available=source.BattleEquipments.ToArray();
            var candidates=CharacterObject.All.Where(c=>c!=null &&
                c.Culture==officer.Culture && c.BattleEquipments!=null &&
                (c.IsBasicTroop || (c.IsTemplate && c.IsFemale==officer.IsFemale)))
                .OrderBy(c=>c.StringId,StringComparer.Ordinal)
                .SelectMany(c=>c.BattleEquipments).Concat(available)
                .Where(e=>e!=null);
            var ranked=candidates.OrderByDescending(e=>EquipmentScore(e,style))
                .ThenByDescending(e=>KitQuality(e,office,false)).ToArray();
            var best=ranked.FirstOrDefault();
            // Variety only among close role matches; never use a random
            // low-scoring kit. Native equipment arrays can repeat sets.
            var pool=best==null?new Equipment[0]:ranked.Where(e=>
                EquipmentScore(e,style)>=EquipmentScore(best,style)-8 &&
                KitQuality(e,office,false)>=KitQuality(best,office,false)-10)
                .GroupBy(KitKey,StringComparer.Ordinal).Select(g=>g.First()).Take(8).ToArray();
            var selected=pool.Length==0?null:pool[random==null?0:random.Next(pool.Length)];
            if(selected==null)throw new InvalidOperationException("No native companion battle equipment set");
            for(int slot=0;slot<(int)EquipmentIndex.NumEquipmentSetSlots;slot++)
                officer.BattleEquipment[slot]=selected[slot];
            for(int slot=0;slot<(int)EquipmentIndex.NumEquipmentSetSlots;slot++)
                if(officer.BattleEquipment[slot].Item!=selected[slot].Item)
                    throw new InvalidOperationException("Native officer equipment not applied");
            DiagnosticJournal.Event("cu_retinue_native_gear",
                "hero="+officer.StringId+"|office="+office+
                "|hero_template="+source.StringId+"|native_gear=true"+
                "|style="+style+
                "|gear_score="+EquipmentScore(selected,style));
        }

        private static string KitKey(Equipment gear)
        {
            return String.Join(",",Enumerable.Range(0,(int)EquipmentIndex.NumEquipmentSetSlots)
                .Select(i=>gear[i].Item==null?"":gear[i].Item.StringId));
        }
        private static int KitQuality(Equipment gear,string office,bool civilian)
        {
            int sum=0,count=0;
            for(int slot=0;slot<(int)EquipmentIndex.NumEquipmentSetSlots;slot++)
            {
                var item=gear[slot].Item;
                if(item==null)continue;
                if(civilian && !item.ItemType.ToString().Contains("Armor") &&
                    item.ItemType.ToString()!="Cape")continue;
                sum+=(int)item.Tier;count++;
            }
            if(count==0)return -10000;
            return -Math.Abs(sum*10/count-SandboxRetinueOfficePolicy.KitTier(office,civilian)*10);
        }
        internal static void EquipCivilian(Hero officer,CharacterObject source,string office,Random random=null)
        {
            if(officer==null || source==null || officer.CivilianEquipment==null)
                throw new InvalidOperationException("Native civilian equipment unavailable");
            var sets=CharacterObject.All.Where(c=>c!=null &&
                c.Culture==officer.Culture && c.IsFemale==officer.IsFemale &&
                c.CivilianEquipments!=null)
                .OrderBy(c=>c.StringId,StringComparer.Ordinal)
                .SelectMany(c=>c.CivilianEquipments)
                .Concat(source.CivilianEquipments??Enumerable.Empty<Equipment>())
                .Where(e=>e!=null && e[(int)EquipmentIndex.Body].Item!=null);
            var ranked=sets.OrderByDescending(e=>KitQuality(e,office,true)).ToArray();
            var best=ranked.FirstOrDefault();
            var pool=best==null?new Equipment[0]:ranked.Where(e=>KitQuality(e,office,true)>=KitQuality(best,office,true)-5)
                .GroupBy(KitKey,StringComparer.Ordinal).Select(g=>g.First()).Take(8).ToArray();
            var selected=pool.Length==0?null:pool[random==null?0:random.Next(pool.Length)];
            if(selected==null)throw new InvalidOperationException("No native civilian outfit");
            for(int slot=0;slot<(int)EquipmentIndex.NumEquipmentSetSlots;slot++)
                officer.CivilianEquipment[slot]=selected[slot];
            for(int slot=0;slot<(int)EquipmentIndex.NumEquipmentSetSlots;slot++)
                if(officer.CivilianEquipment[slot].Item!=selected[slot].Item)
                    throw new InvalidOperationException("Native civilian outfit not applied");
            DiagnosticJournal.Event("cu_retinue_civilian_gear",
                "hero="+officer.StringId+"|office="+office+"|native_readback=true");
        }
        private static void ApplyPersonality(Hero officer,
            string culture,string office)
        {
            var first=SandboxRetinueCulturePolicy.PrimaryTrait(
                culture,office);
            var second=SandboxRetinueCulturePolicy.SecondaryTrait(
                culture,office);
            foreach(var name in SandboxOriginProfile.Traits)
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
                int level=name==first || name==second?
                    SandboxRetinueCulturePolicy.TraitLevel(name,culture,office):0;
                officer.SetTraitLevel(trait,level);
                if(officer.GetTraitLevel(trait)!=level)
                    throw new InvalidOperationException("Native officer trait not applied: "+name);
            }
            DiagnosticJournal.Event("cu_retinue_personality",
                "hero="+officer.StringId+"|office="+office+
                "|culture="+culture+
                "|primary="+first+"|secondary="+second);
        }

        private static void TrainForOffice(Hero officer,string office,Random random)
        {
            if(officer==null || officer.HeroDeveloper==null)
                throw new InvalidOperationException("Native officer training model missing");
            string culture=officer.Culture.StringId;
            var skills=SandboxRetinueCulturePolicy.Training(
                culture,office);
            foreach(string name in SandboxNativeSkillCatalog.AvailableNames())
            {
                var skill=SandboxNativeSkillCatalog.Find(name);
                if(skill==null)continue;
                int rank=Array.IndexOf(skills,name);
                int target=rank>=0?SandboxRetinueOfficePolicy.RandomSkill(office,rank,random):
                    Math.Min(officer.GetSkillValue(skill),45);
                officer.HeroDeveloper.SetInitialSkillLevel(skill,target);
                if(officer.GetSkillValue(skill)!=target)
                    throw new InvalidOperationException("Native officer skill not applied: "+name);
            }
            ApplyPersonality(officer,culture,office);
            var attributes=SandboxRetinueOfficePolicy.AttributesForOffice(office);
            foreach(string name in SandboxOriginProfile.Attributes)
            {
                var prop=typeof(DefaultCharacterAttributes).GetProperty(name);
                var attr=prop==null?null:prop.GetValue(null,null) as CharacterAttribute;
                if(attr==null)throw new InvalidOperationException("Native officer attribute unavailable: "+name);
                int target=Array.IndexOf(attributes,name)==0?5:Array.IndexOf(attributes,name)==1?4:2;
                int delta=target-officer.GetAttributeValue(attr);
                if(delta!=0)officer.HeroDeveloper.AddAttribute(attr,delta,false);
                if(officer.GetAttributeValue(attr)!=target)
                    throw new InvalidOperationException("Native officer attribute not applied: "+name);
            }
            foreach(string name in SandboxNativeSkillCatalog.AvailableNames())
            {
                var skill=SandboxNativeSkillCatalog.Find(name);
                if(skill==null)continue;
                int target=SandboxRetinueOfficePolicy.FocusTarget(Array.IndexOf(skills,name));
                int delta=target-officer.HeroDeveloper.GetFocus(skill);
                if(delta!=0)officer.HeroDeveloper.AddFocus(skill,delta,false);
                if(officer.HeroDeveloper.GetFocus(skill)!=target)
                    throw new InvalidOperationException("Native officer focus not applied: "+name);
            }
            int level=SandboxRetinueOfficePolicy.StartingLevel(office);
            officer.Level=level;
            officer.HeroDeveloper.SetInitialLevel(level);
            if(officer.Level!=level)throw new InvalidOperationException("Native officer level not applied");
            DiagnosticJournal.Event("cu_retinue_progression_profile",
                "hero="+officer.StringId+"|office="+office+"|level="+level+"|native_readback=true|perks_not_rewritten=true");
            DiagnosticJournal.Event("cu_retinue_cultural_training",
                "hero="+officer.StringId+"|office="+office+
                "|culture="+culture+
                "|skills="+String.Join(",",skills)+
                "|native_skill_readback=true"+
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
            Hero player,SandboxRetinueOfficeState registry)
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
            if(registry==null || registry.HasRecords)return "refused:retinue_already_recorded";
            var random=new Random(Guid.NewGuid().GetHashCode());
            var usedSources=new HashSet<string>(StringComparer.Ordinal);
            var sources=new CharacterObject[offices.Length];
            for(int i=0;i<offices.Length;i++)
            {
                bool female=offices[i]=="Chamberlain" ||
                    offices[i]=="RoyalMinister" ||
                    offices[i]=="Navigator";
                sources[i]=FindNobleSource(player,female,offices[i],random,usedSources);
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
                usedSources.Add(sources[i].StringId);
            }
            int created=0;
            int replaced=0;
            var staged=new List<Hero>();
            var usedNames=new HashSet<string>(Clan.PlayerClan.Heroes.Where(h=>h!=null && h.Name!=null).Select(h=>h.Name.ToString()),StringComparer.OrdinalIgnoreCase);
            usedNames.Add(player.Name.ToString());
            var preexistingIds=new HashSet<string>(Hero.AllAliveHeroes.Where(h=>h!=null)
                .Select(h=>h.StringId),StringComparer.Ordinal);
            try
            {
                // Create all replacement Heroes before firing any original
                // companions. A failed source/factory allocation must not
                // dismiss the player's native roster.
                for(int i=0;i<offices.Length;i++)
                {
                    string name=OfficerName(player.Culture.StringId,
                        offices[i],random,usedNames);
                    if(String.IsNullOrEmpty(name) || !usedNames.Add(name))
                        throw new InvalidOperationException("No unused cultural officer name");
                    int age=SandboxRetinueOfficePolicy.RandomAge(offices[i],random);
                    var officer=HeroCreator.CreateSpecialHero(
                        sources[i],null,Clan.PlayerClan,null,age);
                    if(officer==null || !preexistingIds.Add(officer.StringId))
                        throw new InvalidOperationException(
                            "Native HeroCreator refused unique retinue officer");
                    staged.Add(officer);
                    officer.SetName(new TextObject(name),
                        new TextObject(name));
                    officer.Culture=player.Culture;
                    officer.SetBirthDay(CampaignTime.YearsFromNow(-age));
                    if(!SandboxFamilyAgePolicy.MatchesRequestedAge(officer.Age,age))
                        throw new InvalidOperationException("Native officer age not applied");
                    if(!officer.IsFemale && age<=21)officer.ModifyHair(-1,0,-1);
                    // A trade factor, sergeant or navigator does not
                    // become a landed LORD merely by joining a party.
                    // Preserve native lord occupation for a noble
                    // household's appointed mounted combat officer.
                    bool nobleFieldOffice=SandboxRetinueOfficePolicy.NobleCommission(profile.Role,offices[i]);
                    officer.SetNewOccupation(nobleFieldOffice?
                        Occupation.Lord:Occupation.Wanderer);
                    if(!officer.IsActive)
                        officer.ChangeState(Hero.CharacterStates.Active);
                    EquipForStyle(officer,sources[i],
                        SandboxRetinueCulturePolicy.GearStyle(player.Culture.StringId,offices[i]),offices[i],random);
                    EquipCivilian(officer,sources[i],offices[i],random);
                    TrainForOffice(officer,offices[i],random);
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
                    // The native recruitment/assignment action can affect occupation.
                    // Restore only the originally intended category for this new officer.
                    var expectedOccupation=SandboxRetinueOfficePolicy.NobleCommission(profile.Role,offices[i])?
                        Occupation.Lord:Occupation.Wanderer;
                    if(officer.Occupation!=expectedOccupation)officer.SetNewOccupation(expectedOccupation);
                    DiagnosticJournal.Event("cu_officer_occupation_verified",
                        "hero="+officer.StringId+"|office="+offices[i]+"|expected="+expectedOccupation+
                        "|hero_occupation="+officer.Occupation+"|character_occupation="+officer.CharacterObject.Occupation);
                    bool confirmed=officer.Occupation==expectedOccupation &&
                        officer.CharacterObject.Occupation==expectedOccupation &&
                        Clan.PlayerClan.Companions
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
                    if(registry==null || !registry.Register(officer.StringId,offices[i]))
                        throw new InvalidOperationException("Retinue office identity not saved");
                    officer.EncyclopediaText=new TextObject(officer.Name+" serves the house of "+
                        Clan.PlayerClan.Name+" as "+SandboxRetinueOfficePolicy.Profession(offices[i])+". "+
                        SandboxRetinueOfficePolicy.OfficeBackground(offices[i]));
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

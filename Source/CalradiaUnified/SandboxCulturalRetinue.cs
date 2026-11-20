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
    /// taken from their faction. Only "ReplaceNative" dismisses the native
    /// game's initial wanderers, after explicit creator selection.
    /// </summary>
    public static class SandboxCulturalRetinue
    {
        private static CharacterObject FindNobleSource(
            Hero player,bool female)
        {
            // A fixed ordering gives repeatable culture and source choice.
            // There is no random tavern wanderer selection.
            var preferred=CharacterObject.All.Where(c=>c!=null &&
                c.IsTemplate && !c.IsBasicTroop &&
                c.Culture==player.Culture &&
                c.Occupation==Occupation.Lord &&
                c.IsFemale==female)
                .OrderBy(c=>c.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
            if(preferred!=null)return preferred;
            var donor=Hero.AllAliveHeroes.Where(h=>h!=null &&
                h!=player && h.IsActive && h.IsLord &&
                h.Culture==player.Culture &&
                h.IsFemale==female && h.CharacterObject!=null)
                .OrderBy(h=>h.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
            return donor==null?null:donor.CharacterObject;
        }

        private static string OfficerName(string culture,
            string office,int index)
        {
            string[] vlandia={"Aldric","Gervais","Mathieu"};
            string[] sturgia={"Mstislav","Vladimir","Vesna"};
            string[] battania={"Caelan","Branoc","Nessa"};
            string[] empire={"Cassian","Valerius","Livia"};
            string[] khuzait={"Bortu","Temir","Altani"};
            string[] aserai={"Faris","Yahya","Zahra"};
            string[] common={"Aldren","Rowan","Elira"};
            string[] roots= culture=="vlandia"?vlandia :
                culture=="sturgia"?sturgia :
                culture=="battania"?battania :
                culture=="empire"?empire :
                culture=="khuzait"?khuzait :
                culture=="aserai"?aserai:common;
            string stem=roots[index%roots.Length];
            // No random "the Wanderer" suffix or tavern backstory.
            return stem+" ("+office+")";
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
            var sources=new CharacterObject[offices.Length];
            for(int i=0;i<offices.Length;i++)
            {
                bool female=offices[i]=="Chamberlain" ||
                    offices[i]=="Navigator";
                sources[i]=FindNobleSource(player,female);
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
                        if(old.Clan!=Clan.PlayerClan ||
                            old.CompanionOf!=Clan.PlayerClan)
                            continue;
                        RemoveCompanionAction.ApplyByFire(
                            Clan.PlayerClan,old);
                        if(old.CompanionOf!=Clan.PlayerClan)
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
                    DiagnosticJournal.Event("cu_cultural_retinue_officer",
                        "id="+officer.StringId+
                        "|office="+offices[i]+
                        "|culture="+officer.Culture.StringId+
                        "|occupation="+officer.Occupation+
                        "|source_native_lord=true"+
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

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.ObjectSystem;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxLordInheritanceState lordInheritance=
            new SandboxLordInheritanceState();
        private string lordInheritanceReturnMenu="town";

        private static SandboxLordSuccessionPolicy.Candidate[]
            VerifiedLordChildCandidates(Hero founder,Clan birthClan)
        {
            if(founder==null || birthClan==null ||
                founder.Children==null)
                return new SandboxLordSuccessionPolicy.Candidate[0];
            return founder.Children.Where(c=>c!=null)
                .Select((c,index)=>
                {
                    bool realParents=c.Father!=null && c.Mother!=null &&
                        (c.Father==founder || c.Mother==founder) &&
                        c.Father.Children!=null &&
                        c.Mother.Children!=null &&
                        c.Father.Children.Contains(c) &&
                        c.Mother.Children.Contains(c);
                    return new SandboxLordSuccessionPolicy.Candidate(
                        c.StringId,(int)c.Age,c.IsFemale,index,
                        c.IsAlive,c.Clan==birthClan,realParents);
                }).ToArray();
        }

        private static int CurrentLordInheritanceDay()
        {
            double day=CampaignTime.Now.ToDays;
            if(Double.IsNaN(day) || Double.IsInfinity(day) ||
                day<0 || day>Int32.MaxValue-100)
                return -1;
            return (int)Math.Floor(day);
        }

        private string BeginGuidedLordInheritance(
            SandboxOriginProfile profile,Hero founder)
        {
            if(profile==null || !profile.LordGuidedStart ||
                founder==null || founder.Spouse==null ||
                Clan.PlayerClan==null ||
                Clan.PlayerClan.Kingdom==null)
                return "missing_verified_lord_founder";
            var candidates=VerifiedLordChildCandidates(
                founder,Clan.PlayerClan);
            string result=lordInheritance.Begin(
                founder.StringId,founder.Spouse.StringId,
                Clan.PlayerClan.StringId,Clan.PlayerClan.Kingdom.StringId,
                profile.LordSuccessionRule,candidates,
                CurrentLordInheritanceDay());
            DiagnosticJournal.Event("cu_lord_inheritance_start",
                "result="+result+
                "|rule="+profile.LordSuccessionRule+
                "|founder="+founder.StringId+
                "|spouse="+founder.Spouse.StringId+
                "|claimant="+lordInheritance.PrimaryHeirId+
                "|ranked_native_children="+
                    lordInheritance.RankedHeirIds+
                "|native_clan_leader="+Clan.PlayerClan.Leader.StringId+
                "|no_native_clan_transfer=true"+
                "|no_fief_transfer=true"+
                "|no_automatic_regency=true");
            return result;
        }

        private void ObserveLordInheritanceDaily()
        {
            if(!applied || !IsStandaloneSandbox() ||
                !lordInheritance.IsRecorded ||
                MBObjectManager.Instance==null)
                return;
            int day=CurrentLordInheritanceDay();
            if(day<0)return;
            var founder=MBObjectManager.Instance.GetObject<Hero>(
                lordInheritance.FounderId);
            var clan=Clan.All.FirstOrDefault(c=>c!=null &&
                c.StringId==lordInheritance.ClanId);
            if(founder==null || clan==null || clan.IsEliminated ||
                clan.Leader==null)
                return;
            string result=lordInheritance.Observe(day,
                founder.IsAlive,clan.Leader.StringId,
                VerifiedLordChildCandidates(founder,clan));
            if(result=="native_succession_observation_changed")
                DiagnosticJournal.Event("cu_lord_inheritance_observed",
                    "result="+result+
                    "|status="+lordInheritance.Status+
                    "|native_leader="+clan.Leader.StringId+
                    "|requested_primary="+lordInheritance.PrimaryHeirId+
                    "|ranked_native_heirs="+
                        lordInheritance.RankedHeirIds+
                    "|rule="+lordInheritance.Rule+
                    "|fiefs_unchanged_by_CU=true"+
                    "|no_native_clan_leadership_action=true");
        }

        private void RegisterLordInheritanceMenus(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddGameMenu("cu_lord_inheritance",
                "Founding Lord's household inheritance. The rule "+
                "ranks proven biological children. Native clan "+
                "leadership, elections, fiefs and regency remain "+
                "under game authority.",args=>{});
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_lord_inheritance_entry",
                "My household inheritance",
                args=>lordInheritance.IsRecorded,
                args=>
                {
                    lordInheritanceReturnMenu="cu_people_lineage";
                    GameMenu.SwitchToMenu("cu_lord_inheritance");
                },false);
            starter.AddGameMenuOption("cu_lord_inheritance",
                "cu_lord_inheritance_read",
                "Review my family succession order",
                args=>lordInheritance.IsRecorded,
                args=>
                {
                    ObserveLordInheritanceDaily();
                    ShowNativeDetail("Lord household | Inheritance",
                        "Lord household custom: "+
                        lordInheritance.Rule+
                        ". First living verified child: "+
                        NativeHeroDisplayName(
                            lordInheritance.PrimaryHeirId)+
                        ". Current clan leader: "+
                        NativeHeroDisplayName(
                            lordInheritance.NativeLeaderId)+
                        ". Native status: "+lordInheritance.Status+
                        ". Ranked living children: "+
                        NativeHeroDisplayNames(
                            lordInheritance.RankedHeirIds)+
                        ". A minor has no automatic regent. "+
                        "This records claims only, not automatic land "+
                        "or clan leadership.");
                },false);
            starter.AddGameMenuOption("cu_lord_inheritance",
                "cu_lord_inheritance_back","Return",
                args=>true,
                args=>GameMenu.SwitchToMenu(lordInheritanceReturnMenu),
                false);
        }
    }
}

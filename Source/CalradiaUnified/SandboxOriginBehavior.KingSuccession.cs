using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.ObjectSystem;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxKingSuccessionState kingSuccession=
            new SandboxKingSuccessionState();
        private string kingSuccessionReturnMenu="town";

        private string BeginGuidedKingSuccession(
            SandboxOriginProfile profile,Hero monarch)
        {
            if(profile==null || !profile.KingGuidedStart ||
                monarch==null || Clan.PlayerClan==null ||
                Clan.PlayerClan.Leader!=monarch ||
                Clan.PlayerClan.Kingdom==null ||
                Clan.PlayerClan.Kingdom.RulingClan!=Clan.PlayerClan)
                return "native_monarch_not_verified";
            string result=kingSuccession.Begin(
                monarch.StringId,Clan.PlayerClan.StringId,
                Clan.PlayerClan.Kingdom.StringId,
                profile.KingSuccessionRule,
                VerifiedLordChildCandidates(monarch,Clan.PlayerClan),
                CurrentLordInheritanceDay());
            DiagnosticJournal.Event("cu_king_succession_recorded",
                "result="+result+
                "|native_ruling_clan="+Clan.PlayerClan.StringId+
                "|kingdom="+Clan.PlayerClan.Kingdom.StringId+
                "|rule="+profile.KingSuccessionRule+
                "|preferred_child="+kingSuccession.PreferredHeirId+
                "|native_child_list="+kingSuccession.RankedHeirIds+
                "|actual_monarch="+monarch.StringId+
                "|native_crown_election_unchanged=true"+
                "|no_regent_created=true"+
                "|no_fiefs_transferred=true");
            return result;
        }

        private void ObserveKingSuccessionDaily(){RefreshKingSuccessionObservation();}

        private bool RefreshKingSuccessionObservation()
        {
            if(!applied || !IsStandaloneSandbox() ||
                !kingSuccession.IsRecorded ||
                MBObjectManager.Instance==null)return false;
            int day=CurrentLordInheritanceDay();
            if(day<0)return false;
            var founder=MBObjectManager.Instance.GetObject<Hero>(
                kingSuccession.FounderId);
            var kingdom=Kingdom.All.FirstOrDefault(k=>k!=null &&
                k.StringId==kingSuccession.KingdomId);
            if(founder==null || kingdom==null || kingdom.IsEliminated ||
                kingdom.RulingClan==null ||
                kingdom.RulingClan.Leader==null)return false;
            string result=kingSuccession.Observe(
                day,founder.IsAlive,
                kingdom.RulingClan.StringId,
                kingdom.RulingClan.Leader.StringId,
                VerifiedLordChildCandidates(
                    founder,Clan.All.FirstOrDefault(c=>c!=null &&
                        c.StringId==kingSuccession.RulingClanId)));
            if(result=="native_crown_observation_changed")
                DiagnosticJournal.Event("cu_king_succession_observed",
                    "result="+result+
                    "|native_status="+kingSuccession.Status+
                    "|kingdom="+kingdom.StringId+
                    "|actual_ruling_clan="+
                        kingdom.RulingClan.StringId+
                    "|actual_ruler="+kingdom.RulingClan.Leader.StringId+
                    "|preferred_child="+kingSuccession.PreferredHeirId+
                    "|ranking="+kingSuccession.RankedHeirIds+
                    "|no_native_kingdom_or_fief_mutation=true");
            return result=="native_crown_observation_changed" || result=="native_crown_unchanged";
        }

        private void RegisterKingSuccessionMenus(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddGameMenu("cu_king_succession",
                "Royal household inheritance. These child priorities "+
                "are saved claims, not an election, an appointed "+
                "regent or a legal grant of crown and estates.",
                args=>{});
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_king_succession_entry",
                "Dynastic succession",
                args=>kingSuccession.IsRecorded,
                args=>
                {
                    kingSuccessionReturnMenu="cu_people_lineage";
                    GameMenu.SwitchToMenu("cu_king_succession");
                },false);
            starter.AddGameMenuOption("cu_king_succession",
                "cu_king_succession_review",
                "Royal succession",
                args=>kingSuccession.IsRecorded,
                args=>
                {
                    bool current=RefreshKingSuccessionObservation();
                    ShowNativeDetail("Royal household | Succession",
                        "Royal household rule: "+kingSuccession.Rule+
                        (current?". Preferred heir: ":". Last recorded preferred child: ")+
                        NativeHeroDisplayName(
                            kingSuccession.PreferredHeirId)+
                        (current?". Child claim order: ":". Last recorded child claim order: ")+
                        NativeHeroDisplayNames(
                            kingSuccession.RankedHeirIds)+
                        (current?". Current ruler: ":". Last known ruler: ")+
                        NativeHeroDisplayName(
                            kingSuccession.ActualRulerId)+
                        ". Kingdom: "+kingSuccession.KingdomId+
                        ". "+(current?"Status: ":"Last recorded status: ")+kingSuccession.Status+
                        (current?"":". Current kingdom state unavailable; last observation day "+kingSuccession.LastObservedDay)+
                        ". Succession follows the kingdom's laws. "+
                        "Minor heirs receive no automatic regency.");
                },false);
            starter.AddGameMenuOption("cu_king_succession",
                "cu_king_dynastic_branches","Review descendants and collateral kin",
                args=>kingSuccession.IsRecorded,args=>ShowDynasticBranches(true),false);
            starter.AddGameMenuOption("cu_king_succession",
                "cu_king_succession_return","Return",
                args=>true,
                args=>GameMenu.SwitchToMenu(kingSuccessionReturnMenu),
                false);
        }
    }
}

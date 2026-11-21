using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        private SandboxKnightOathState knightOath=
            new SandboxKnightOathState();
        private string oathReturnMenu="town";

        private static int CurrentOathDay()
        {
            double d=CampaignTime.Now.ToDays;
            if(Double.IsNaN(d) || Double.IsInfinity(d) ||
                d<0 || d>Int32.MaxValue-100)
                return -1;
            return (int)Math.Floor(d);
        }
        private Hero KnightOathPatron()
        {
            if(!knightOath.IsConfigured || MBObjectManager.Instance==null)
                return null;
            var hero=MBObjectManager.Instance.GetObject<Hero>(
                knightOath.PatronId);
            return hero!=null && hero.StringId==knightOath.PatronId?
                hero:null;
        }
        private Clan KnightOathClan()
        {
            if(!knightOath.IsConfigured)return null;
            return Clan.All.FirstOrDefault(c=>c!=null &&
                c.StringId==knightOath.PatronClanId);
        }
        private bool KnightOathInPerson(Hero target)
        {
            var here=Settlement.CurrentSettlement;
            return here!=null && target!=null && target.IsAlive &&
                target.IsActive && target.CurrentSettlement==here &&
                Hero.MainHero!=null && 
                Hero.MainHero.StringId==knightOath.PlayerId;
        }
        private bool CanKnightMuster()
        {
            int day=CurrentOathDay();
            var patron=KnightOathPatron();
            return day>=0 && patron!=null &&
                !patron.IsPrisoner && patron.Clan!=null &&
                patron.Clan.StringId==knightOath.PatronClanId &&
                patron.Clan.Kingdom!=null &&
                patron.Clan.Kingdom.StringId==knightOath.KingdomId &&
                knightOath.CanMuster(day,KnightOathInPerson(patron));
        }
        private bool CanKnightRelease()
        {
            var patron=KnightOathPatron();
            return knightOath.HasCurrentOath && patron!=null &&
                !patron.IsPrisoner && patron.Clan!=null &&
                patron.Clan.StringId==knightOath.PatronClanId &&
                patron.Clan.Kingdom!=null &&
                patron.Clan.Kingdom.StringId==knightOath.KingdomId &&
                KnightOathInPerson(patron);
        }
        private Hero AvailableKnightSuccessor()
        {
            if(knightOath.Status!="SuccessionPending")return null;
            var clan=KnightOathClan();
            if(clan==null || clan.IsEliminated ||
                clan.Kingdom==null ||
                clan.Kingdom.StringId!=knightOath.KingdomId ||
                clan.Leader==null ||
                clan.Leader.StringId==knightOath.PatronId ||
                !clan.Leader.IsAlive || !clan.Leader.IsActive ||
                !clan.Leader.IsLord)
                return null;
            return clan.Leader;
        }
        private bool CanKnightRenew()
        {
            var successor=AvailableKnightSuccessor();
            return successor!=null && KnightOathInPerson(successor);
        }
        private bool KnightOathNativeRelation(Hero patron,int change,
            string eventName)
        {
            if(patron==null || Hero.MainHero==null)return false;
            try
            {
                int before=Hero.MainHero.GetRelation(patron);
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    Hero.MainHero,patron,change,false);
                DiagnosticJournal.Event(eventName,
                    "patron="+patron.StringId+
                    "|native_relation_before="+before+
                    "|native_relation_after="+
                        Hero.MainHero.GetRelation(patron)+
                    "|no_gold_or_fief_transferred=true");
                return true;
            }
            catch(Exception ex)
            {
                DiagnosticJournal.Exception(
                    "cu_knight_native_relation_failed",ex);
                return false;
            }
        }
        private void ObserveKnightOathDaily()
        {
            if(!applied || !IsStandaloneSandbox() ||
                !knightOath.HasCurrentOath ||
                Hero.MainHero==null ||
                Hero.MainHero.StringId!=knightOath.PlayerId)
                return;
            int day=CurrentOathDay();
            if(day<0)return;
            var patron=KnightOathPatron();
            bool sameClan=patron!=null && patron.Clan!=null &&
                patron.Clan.StringId==knightOath.PatronClanId;
            bool sameKingdom=sameClan && patron.Clan.Kingdom!=null &&
                patron.Clan.Kingdom.StringId==knightOath.KingdomId;
            string result=knightOath.Observe(day,patron!=null,
                patron!=null && patron.IsAlive,sameClan,sameKingdom,
                patron!=null && patron.IsPrisoner);
            if(result=="new_muster_breach")
            {
                // One state transition per overdue period, hence one
                // native standing penalty. Re-loading cannot replay it.
                KnightOathNativeRelation(patron,-3,
                    "cu_knight_oath_missed_muster_relation");
            }
            if(result=="new_muster_breach" ||
               result=="successor_consent_required" ||
               result=="native_patron_allegiance_changed" ||
               result=="patron_captivity_pause" ||
               result=="patron_returned_muster_reset" ||
               result=="muster_window_open")
                DiagnosticJournal.Event("cu_knight_oath_observed",
                    "result="+result+
                    "|day="+day+
                    "|patron="+knightOath.PatronId+
                    "|clan="+knightOath.PatronClanId+
                    "|next_muster="+knightOath.NextMusterDay+
                    "|native_lord_changed="+(!sameClan)+
                    "|no_automatic_clan_transfer=true");
        }
        private void RegisterKnightOathMenus(CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddGameMenu("cu_knight_oath",
                "Sworn knight of a separate house. Your personal "+
                "service oath is saved in this campaign. No kingdom "+
                "vassalage or NPC-party enlistment is implied.",
                args=>{});
            foreach(string townMenu in new[]{"town","castle"})
            {
                string from=townMenu;
                starter.AddGameMenuOption(from,
                    "cu_knight_oath_entry_"+from,
                    "CU: Knightly oath and service",
                    args=>knightOath.IsConfigured &&
                        Hero.MainHero!=null &&
                        Hero.MainHero.StringId==knightOath.PlayerId,
                    args=>
                    {
                        oathReturnMenu=from;
                        GameMenu.SwitchToMenu("cu_knight_oath");
                    },false);
            }
            starter.AddGameMenuOption("cu_knight_oath",
                "cu_knight_oath_status","Review my sworn duty",
                args=>knightOath.IsConfigured,
                args=>
                {
                    var patron=KnightOathPatron();
                    Notify("Oath: "+knightOath.Status+
                        ". Patron: "+(patron==null?
                            knightOath.PatronId:patron.Name.ToString())+
                        " of "+(patron==null || patron.Clan==null?
                            knightOath.PatronClanId:
                            patron.Clan.Name.ToString())+
                        ". Native relation: "+(patron==null ||
                            Hero.MainHero==null?"unavailable":
                            Hero.MainHero.GetRelation(patron).ToString())+
                        ". Musters: "+knightOath.CompletedMusters+
                        ". Missed: "+knightOath.MissedMusters+
                        ". Next muster on day "+knightOath.NextMusterDay+
                        ". No automatic wage, land or command.");
                },false);
            starter.AddGameMenuOption("cu_knight_oath",
                "cu_knight_oath_muster",
                "Report to patron in person for sworn muster",
                args=>CanKnightMuster(),
                args=>
                {
                    var patron=KnightOathPatron();
                    int day=CurrentOathDay();
                    if(patron==null || !CanKnightMuster())return;
                    string result=knightOath.RecordMuster(day,true);
                    if(result!="native_meeting_muster_recorded")return;
                    bool nativeFavor=KnightOathNativeRelation(patron,2,
                        "cu_knight_native_muster_reward");
                    DiagnosticJournal.Event("cu_knight_oath_muster",
                        "result="+result+"|day="+day+
                        "|native_patron_meeting=true"+
                        "|native_favor_action="+nativeFavor+
                        "|next_muster="+knightOath.NextMusterDay);
                    Notify("Muster recorded with "+patron.Name+
                        (nativeFavor?". Native favor action completed.":
                        ". Native favor unconfirmed; see diagnostics."));
                },false);
            starter.AddGameMenuOption("cu_knight_oath",
                "cu_knight_oath_release",
                "Request honorable release in person",
                args=>CanKnightRelease(),
                args=>
                {
                    var patron=KnightOathPatron();
                    if(patron==null || !CanKnightRelease())return;
                    string result=knightOath.Release(true);
                    if(result!="consensual_release")return;
                    KnightOathNativeRelation(patron,1,
                        "cu_knight_oath_honorable_release_relation");
                    DiagnosticJournal.Event("cu_knight_oath_release",
                        "result="+result+"|patron="+patron.StringId+
                        "|no_clan_or_property_changed=true");
                    Notify("You have been released from the personal "+
                        "oath. Your clan remains independent.");
                },false);
            starter.AddGameMenuOption("cu_knight_oath",
                "cu_knight_oath_renew",
                "Swear renewed oath to the actual successor",
                args=>CanKnightRenew(),
                args=>
                {
                    var successor=AvailableKnightSuccessor();
                    if(successor==null || !CanKnightRenew())return;
                    int day=CurrentOathDay();
                    string result=knightOath.AcceptSuccessor(
                        successor.StringId,successor.Clan.StringId,
                        successor.Clan.Kingdom.StringId,day,true);
                    if(result!="oath_renewed_to_native_successor")return;
                    KnightOathNativeRelation(successor,2,
                        "cu_knight_successor_oath_relation");
                    DiagnosticJournal.Event("cu_knight_oath_succession",
                        "result="+result+
                        "|new_native_clan_leader="+successor.StringId+
                        "|same_clan="+successor.Clan.StringId+
                        "|player_house_retained=true");
                    Notify("The oath now names "+successor.Name+
                        ". Your independent clan was not transferred.");
                },false);
            starter.AddGameMenuOption("cu_knight_oath",
                "cu_knight_oath_break_request",
                "Renounce oath without leave (confirmation)",
                args=>knightOath.HasCurrentOath,
                args=>GameMenu.SwitchToMenu(
                    "cu_knight_oath_confirm_break"),false);
            starter.AddGameMenu("cu_knight_oath_confirm_break",
                "Renouncing your oath ends personal service and "+
                "damages standing with the patron. This cannot be "+
                "reversed through the creator.",args=>{});
            starter.AddGameMenuOption("cu_knight_oath_confirm_break",
                "cu_knight_oath_break_confirm",
                "Confirm: break my oath",
                args=>knightOath.HasCurrentOath,
                args=>
                {
                    var patron=KnightOathPatron();
                    string result=knightOath.Break();
                    if(result=="oath_broken")
                    {
                        // Resolved native identity is required for a
                        // native penalty. Never claim one if absent.
                        if(patron!=null)
                            KnightOathNativeRelation(patron,-20,
                                "cu_knight_oath_break_relation");
                        DiagnosticJournal.Event("cu_knight_oath_break",
                            "result="+result+
                            "|native_patron_resolved="+(patron!=null)+
                            "|no_kingdom_or_fief_mutation=true");
                        Notify("The personal oath is broken. Any "+
                            "native relation effect is recorded.");
                    }
                    GameMenu.SwitchToMenu("cu_knight_oath");
                },false);
            starter.AddGameMenuOption("cu_knight_oath_confirm_break",
                "cu_knight_oath_break_cancel",
                "Keep my oath",
                args=>true,
                args=>GameMenu.SwitchToMenu("cu_knight_oath"),false);
            starter.AddGameMenuOption("cu_knight_oath",
                "cu_knight_oath_leave","Return",
                args=>true,
                args=>GameMenu.SwitchToMenu(oathReturnMenu),false);
        }
    }
}

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        // Native inventories, parties, companions and garrisons only.
        // A named title never creates troops or political authority.
        private static bool CanNativeOfficerTransfer(Settlement seat)
        {
            var party=MobileParty.MainParty;
            var clan=Clan.PlayerClan;
            if(!Settings.NativeOfficerTransfersEnabled)return false;
            if(seat==null || party==null || clan==null ||
               seat.OwnerClan!=clan || !seat.IsFortification ||
               seat.IsUnderSiege || seat.Town==null ||
               seat.Town.GarrisonParty==null || seat.Culture==null ||
               seat.Culture.BasicTroop==null ||
               Settlement.CurrentSettlement!=seat ||
               party.LeaderHero!=Hero.MainHero ||
               party.MemberRoster==null ||
               seat.Town.GarrisonParty.MemberRoster==null)return false;
            var troop=seat.Culture.BasicTroop;
            int i=party.MemberRoster.FindIndexOfTroop(troop);
            if(i<0 || party.MemberRoster.GetTroopCount(troop)<5)
                return false;
            var element=party.MemberRoster.GetElementCopyAtIndex(i);
            return element.WoundedNumber==0 && element.Xp==0;
        }
        private bool RealOfficerSpeaking(string title)
        {
            var officer=NativeCourtOfficer(title);
            var party=MobileParty.MainParty;
            var clan=Clan.PlayerClan;
            return officer!=null && clan!=null &&
                Hero.OneToOneConversationHero==officer &&
                officer.CompanionOf==clan && officer.Clan==clan &&
                party!=null && party.MemberRoster!=null &&
                officer.CharacterObject!=null &&
                party.MemberRoster.GetTroopCount(
                    officer.CharacterObject)==1;
        }
        private bool LordMasterAvailable()
        {
            var seat=Settlement.CurrentSettlement;
            return lordInheritance.IsRecorded &&
                Clan.PlayerClan!=null &&
                Clan.PlayerClan.Leader==Hero.MainHero &&
                RealOfficerSpeaking("Master of Arms") &&
                seat!=null && seat.IsCastle &&
                CanNativeOfficerTransfer(seat);
        }
        private bool KingMarshalAvailable()
        {
            return kingSuccession.IsRecorded &&
                RealOfficerSpeaking("Marshal") &&
                NativeRoyalCouncilPresent(true) &&
                royalCouncil.LastMeetingDay==
                    CurrentLordInheritanceDay() &&
                CanNativeOfficerTransfer(RecordedRoyalSeat());
        }
        private void TransferExistingTroopsByOfficer(bool royal)
        {
            bool permitted=royal?
                KingMarshalAvailable():LordMasterAvailable();
            if(!permitted)return;
            var seat=royal?RecordedRoyalSeat():
                Settlement.CurrentSettlement;
            var troop=seat.Culture.BasicTroop;
            var party=MobileParty.MainParty.MemberRoster;
            var garrison=seat.Town.GarrisonParty.MemberRoster;
            int oldParty=party.GetTroopCount(troop);
            int oldGarrison=garrison.GetTroopCount(troop);
            bool confirmed=false;
            try
            {
                party.AddToCounts(troop,-5);
                garrison.AddToCounts(troop,5);
                confirmed=party.GetTroopCount(troop)==oldParty-5 &&
                    garrison.GetTroopCount(troop)==oldGarrison+5;
                if(!confirmed)
                    throw new InvalidOperationException(
                        "Native roster delta not confirmed");
            }
            catch(Exception error)
            {
                int excess=garrison.GetTroopCount(troop)-oldGarrison;
                if(excess>0)garrison.AddToCounts(troop,-excess);
                int shortfall=oldParty-party.GetTroopCount(troop);
                if(shortfall>0)party.AddToCounts(troop,shortfall);
                DiagnosticJournal.Exception(
                    "cu_noble_officer_garrison_order_failed",error);
                confirmed=false;
            }
            var officer=NativeCourtOfficer(royal?
                "Marshal":"Master of Arms");
            DiagnosticJournal.Event(royal?
                "cu_king_marshal_native_court_order":
                "cu_lord_master_native_garrison_order",
                "result="+(confirmed?"PASS":"FAIL")+
                "|actual_officer="+(officer==null?
                    "none":officer.StringId)+
                "|native_fief="+seat.StringId+
                "|troops_transferred="+(confirmed?5:0)+
                "|troops_spawned=0"+
                "|real_garrison="+confirmed);
            Notify(confirmed?
                "Five of your existing recruits now serve "+
                    "in "+seat.Name+"'s native garrison.":
                "The native garrison rejected this order. "+
                    "No soldiers were granted.");
        }
        private void RegisterNobleOfficerConversations(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddPlayerLine("cu_lord_master_order",
                "hero_main_options","cu_lord_master_order_reply",
                "Master of Arms, station five of my recruits in this castle.",
                ()=>LordMasterAvailable(),
                ()=>TransferExistingTroopsByOfficer(false),100);
            starter.AddDialogLine("cu_lord_master_order_reply",
                "cu_lord_master_order_reply","close_window",
                "I will post your existing soldiers to the garrison.",
                null,null,100);
            starter.AddPlayerLine("cu_king_marshal_order",
                "hero_main_options","cu_king_marshal_order_reply",
                "Marshal, execute today's council garrison order.",
                ()=>KingMarshalAvailable(),
                ()=>TransferExistingTroopsByOfficer(true),100);
            starter.AddDialogLine("cu_king_marshal_order_reply",
                "cu_king_marshal_order_reply","close_window",
                "I will move your own recruits to our royal holding.",
                null,null,100);
        }
    }
}

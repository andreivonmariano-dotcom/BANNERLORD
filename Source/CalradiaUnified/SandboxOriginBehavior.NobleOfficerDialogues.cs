using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        // Native inventories, parties, companions and garrisons only.
        // A named title never creates troops or political authority.
        private static bool CanNativeOfficerTransfer(Settlement seat)
        {
            if(!Settings.NativeOfficerTransfersEnabled)return false;
            return CanTransferExistingRecruits(seat);
        }
        // Shared manual-transfer guard; never used by an autonomous observer.
        private static bool CanTransferExistingRecruits(Settlement seat)
        {
            var party=MobileParty.MainParty;
            var clan=Clan.PlayerClan;
            if(seat==null || party==null || clan==null ||
               seat.OwnerClan!=clan || !seat.IsFortification ||
               seat.IsUnderSiege || seat.Town==null ||
               seat.Town.GarrisonParty==null || seat.Culture==null ||
               seat.Culture.BasicTroop==null ||
               Settlement.CurrentSettlement!=seat ||
               party.LeaderHero!=Hero.MainHero || party.MapEvent!=null ||
               Hero.MainHero==null || Hero.MainHero.CurrentSettlement!=seat ||
               (seat.Party!=null && seat.Party.MapEvent!=null) ||
               party.MemberRoster==null ||
               seat.Town.GarrisonParty.MemberRoster==null)return false;
            var nativeGarrison=seat.Town.GarrisonParty;
            if(nativeGarrison.Party==null || nativeGarrison.MapEvent!=null ||
                nativeGarrison.Party.PartySizeLimit-nativeGarrison.MemberRoster.TotalManCount<5)
                return false;
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
            return officer!=null && clan!=null && officer.IsAlive &&
                officer.IsActive && !officer.IsPrisoner &&
                Hero.OneToOneConversationHero==officer &&
                officer.CompanionOf==clan && officer.Clan==clan &&
                party!=null && party.MemberRoster!=null && officer.PartyBelongedTo==party &&
                officer.CharacterObject!=null &&
                party.MemberRoster.GetTroopCount(
                    officer.CharacterObject)==1;
        }
        private bool LordMasterAvailable()
        {
            var seat=Settlement.CurrentSettlement;
            return lordInheritance.IsRecorded && CurrentNobleAuthority(false) &&
                Clan.PlayerClan!=null && Clan.PlayerClan.StringId==lordInheritance.ClanId &&
                Clan.PlayerClan.Leader==Hero.MainHero &&
                RealOfficerSpeaking("Master of Arms") &&
                seat!=null && seat.IsFortification &&
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
        private bool TransferExistingTroopsByOfficer(bool royal)
        {
            MBTextManager.SetTextVariable("CU_OFFICER_TRANSFER_REPLY",new TextObject(
                "The order cannot be carried out just now. No transfer has been confirmed."));
            bool permitted=royal?
                KingMarshalAvailable():LordMasterAvailable();
            if(!permitted)return false;
            var seat=royal?RecordedRoyalSeat():
                Settlement.CurrentSettlement;
            var officer=NativeCourtOfficer(royal?"Marshal":"Master of Arms");
            return TransferExistingRecruits(seat,officer,royal?
                "cu_king_marshal_native_court_order":"cu_lord_master_native_garrison_order");
        }
        private bool householdTransferBusy;
        private bool TransferExistingRecruits(Settlement seat,Hero officer,string receipt)
        {
            if(householdTransferBusy || !CanTransferExistingRecruits(seat))return false;
            householdTransferBusy=true;
            try{return TransferExistingRecruitsCore(seat,officer,receipt);}
            finally{householdTransferBusy=false;}
        }
        private bool TransferExistingRecruitsCore(Settlement seat,Hero officer,string receipt)
        {
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
            DiagnosticJournal.Event(receipt,
                "result="+(confirmed?"PASS":"FAIL")+
                "|actual_officer="+(officer==null?
                    "none":officer.StringId)+
                "|native_fief="+seat.StringId+
                "|troops_transferred="+(confirmed?5:0)+
                "|troops_spawned=0"+
                "|real_garrison="+confirmed+
                "|party_before="+oldParty+"|party_after="+party.GetTroopCount(troop)+
                "|garrison_before="+oldGarrison+"|garrison_after="+garrison.GetTroopCount(troop));
            MBTextManager.SetTextVariable("CU_OFFICER_TRANSFER_REPLY",new TextObject(
                confirmed?"Five of your recruits now serve in "+seat.Name+"’s garrison. The men have been accounted for.":
                "I cannot confirm that the order was carried out. We should check the rolls before attempting it again."));
            Notify(confirmed?
                "Five of your existing recruits now serve "+
                    "in "+seat.Name+"'s native garrison.":
                "The native transfer was not confirmed. "+
                    "Check both rosters before issuing another order.");
            return confirmed;
        }
        // Explicit opt-in for a native full-stack playtest. Normal launches
        // retain the existing freeze until roster conservation is witnessed.
        private static bool HouseholdOrderTrialEnabled()
        {
            // No raw CU roster edits in a process with the external garrison owner.
            return !ImprovedGarrisonsOwnsRecruitment() &&
                String.Equals(Environment.GetEnvironmentVariable("CU_HOUSEHOLD_ORDER_TRIAL"),
                "1",StringComparison.Ordinal);
        }
        private static bool ImprovedGarrisonsOwnsRecruitment()
        {
            // Inspect only loaded assembly identities; no private hooks or invocation.
            try
            {
                foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    if(assembly.GetName().Name.IndexOf("ImprovedGarrisons",StringComparison.OrdinalIgnoreCase)>=0)return true;
                return false;
            }
            catch{return true;}
        }
        private bool HouseholdOrderSpeaker()
        {
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            var seat=Settlement.CurrentSettlement;
            var party=MobileParty.MainParty;
            return HouseholdOrderTrialEnabled() && applied && IsStandaloneSandbox() &&
                actor!=null && player!=null && actor!=player &&
                player.IsAlive && !player.IsPrisoner && clan!=null && clan.Leader==player &&
                actor.IsAlive && actor.IsActive && !actor.IsPrisoner && actor.Age>=18f &&
                actor.Clan==clan && (actor==player.Spouse && actor.Spouse==player ||
                    actor.CompanionOf==clan) &&
                seat!=null && seat.IsFortification && seat.OwnerClan==clan && !seat.IsUnderSiege &&
                party!=null && (actor.CurrentSettlement==seat || actor.PartyBelongedTo==party);
        }
        private void ExecuteHouseholdDefenseOrder()
        {
            string reply="I cannot carry that out here. We need five unwounded, untrained local recruits in your party and room in our garrison. No transfer was confirmed.";
            if(HouseholdOrderSpeaker() && CanTransferExistingRecruits(Settlement.CurrentSettlement))
            {
                if(TransferExistingRecruits(Settlement.CurrentSettlement,Hero.OneToOneConversationHero,
                    "cu_household_garrison_order"))
                    reply="Done. Five recruits left your party and joined this holding's garrison.";
                else reply="The transfer could not be confirmed. Check both rosters before giving another order.";
            }
            MBTextManager.SetTextVariable("CU_HOUSEHOLD_ORDER_REPLY",new TextObject(reply));
        }
        // A conversational shortcut to the native governor action, not a second governor simulation.
        private bool CanAskResidentToGovern()
        {
            var actor=Hero.OneToOneConversationHero;
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            var seat=Settlement.CurrentSettlement;
            var party=MobileParty.MainParty;
            return applied && IsStandaloneSandbox() && player!=null && player.IsAlive && player.IsActive && !player.IsPrisoner &&
                HasHouseholdCommandServiceAuthority() && clan!=null && clan.Leader==player && actor!=null && actor!=player && actor.Clan==clan &&
                actor.IsAlive && actor.IsActive && actor.Age>=18f && !actor.IsPrisoner && !actor.IsTraveling &&
                !actor.IsFugitive && actor.GovernorOf==null && actor.PartyBelongedTo==null &&
                actor.PartyBelongedToAsPrisoner==null && actor.GetRelation(player)>=0 &&
                ActualAdultHouseholdMember(player,actor,clan) &&
                seat!=null && seat.OwnerClan==clan && seat.IsFortification && !seat.IsUnderSiege &&
                seat.Town!=null && seat.Town.Governor==null && actor.CurrentSettlement==seat &&
                player.CurrentSettlement==seat && party!=null && party.LeaderHero==player && party.ActualClan==clan &&
                party.Army==null && !party.IsCurrentlyAtSea && party.MapEvent==null &&
                (seat.Party==null || seat.Party.MapEvent==null);
        }
        private void AskResidentToGovern()
        {
            string reply="I cannot take that appointment just now. No change was confirmed.";
            if(CanAskResidentToGovern())
            {
                var actor=Hero.OneToOneConversationHero;
                var seat=Settlement.CurrentSettlement;
                try{ChangeGovernorAction.Apply(seat.Town,actor);}
                catch(Exception error){DiagnosticJournal.Exception("cu_household_governor_action_failed",error);}
                bool confirmed=seat.Town.Governor==actor && actor.GovernorOf==seat.Town;
                DiagnosticJournal.Event("cu_household_native_governor_order",
                    "result="+(confirmed?"PASS":"UNCONFIRMED")+"|actor="+actor.StringId+"|seat="+seat.StringId+
                    "|native_action=true|no_teleport=true|no_incumbent_replaced=true");
                if(confirmed)
                {
                    livingPersons.Remember("NativeGovernorAssigned",Hero.MainHero.StringId,actor.StringId,
                        "seat="+seat.StringId+"|day="+CurrentHouseholdDay());
                    reply="I am now governor of "+seat.Name+". You can see the appointment in the clan screen.";
                }
                else reply="The appointment was not confirmed. Check the native clan screen before trying again.";
            }
            MBTextManager.SetTextVariable("CU_HOUSEHOLD_GOVERNOR_REPLY",new TextObject(reply));
        }
        private static readonly PartyRole[] HouseholdSpecialties={PartyRole.Quartermaster,PartyRole.Scout,PartyRole.Surgeon,PartyRole.Engineer};
        private static readonly PartyRole[] NativeAssignmentSlots={PartyRole.Quartermaster,PartyRole.Scout,PartyRole.Surgeon,PartyRole.Engineer,PartyRole.FirstMate,PartyRole.Navigator};
        private static Hero EffectiveSpecialist(MobileParty party,PartyRole role)
        {
            switch(role)
            {
                case PartyRole.Quartermaster:return party.EffectiveQuartermaster;
                case PartyRole.Scout:return party.EffectiveScout;
                case PartyRole.Surgeon:return party.EffectiveSurgeon;
                case PartyRole.Engineer:return party.EffectiveEngineer;
                default:return null;
            }
        }
        private static bool ActualAdultHouseholdMember(Hero player,Hero actor,Clan clan)
        {
            if(player==null || actor==null || actor==player || clan==null || actor.Clan!=clan ||
                !actor.IsAlive || !actor.IsActive || actor.Age<18f || actor.IsPrisoner || actor.IsTraveling || actor.IsFugitive)return false;
            return (actor==player.Spouse && actor.Spouse==player) ||
                (player.Children!=null && player.Children.Contains(actor) && (actor.Father==player || actor.Mother==player)) ||
                (clan.Companions!=null && clan.Companions.Contains(actor) && actor.CompanionOf==clan);
        }
        private bool CanAssignHouseholdSpecialty(PartyRole role)
        {
            try
            {
                var player=Hero.MainHero;var actor=Hero.OneToOneConversationHero;
                var clan=Clan.PlayerClan;var party=MobileParty.MainParty;
                if(!applied || !IsStandaloneSandbox() || !HouseholdSpecialties.Contains(role) ||
                    player==null || !player.IsAlive || !player.IsActive || player.IsPrisoner || clan==null || clan.Leader!=player ||
                    party==null || party.LeaderHero!=player || party.ActualClan!=clan || party.Army!=null ||
                    party.MapEvent!=null || party.IsCurrentlyAtSea || party.MemberRoster==null ||
                    !ActualAdultHouseholdMember(player,actor,clan) || actor.GetRelation(player)<0 || actor.GovernorOf!=null ||
                    actor.PartyBelongedTo!=party || party.MemberRoster.GetTroopCount(actor.CharacterObject)!=1)return false;
                if(!HasHouseholdCommandServiceAuthority())return false;
                var seat=Settlement.CurrentSettlement;
                if(seat!=null && seat.IsUnderSiege)return false;
                // GetRoleHolder returns the raw native slot, even for an absent holder.
                // The native setter can remove another duty at the role limit.
                return party.GetRoleHolder(role)==null && party.CanAssignMoreRolesToHero(actor) &&
                    party.GetHeroPartyRoles(actor).Count==0;
            }
            catch{return false;}
        }
        private static string NativeHouseholdAssignment(Hero person)
        {
            if(person==null)return "";
            if(person.GovernorOf!=null)return "Governor of "+person.GovernorOf.Settlement.Name;
            var party=person.PartyBelongedTo;
            if(party==null)return "";
            var roles=NativeAssignmentSlots.Where(role=>party.GetRoleHolder(role)==person).Select(role=>role.ToString()).ToArray();
            return roles.Length==0?"":String.Join(", ",roles);
        }
        private bool HasHouseholdCommandServiceAuthority()
        {
            try
            {
                var service=SandboxSaSBridge.Read();
                if(service.Enlisted || (!service.Available && (serviceCareer.VerifiedExternalEnlistment ||
                    serviceCareer.Status=="ExternalStatusUnverified" || CurrentCareerRole()=="Soldier")))return false;
                return true;
            }
            catch{return false;}
        }
        private bool CanOfferHouseholdResponsibilities()
        {
            return CanAskResidentToGovern() || HouseholdSpecialties.Any(role=>(role!=PartyRole.Surgeon || CU80ManualSurgeonAllowed()) && CanAssignHouseholdSpecialty(role));
        }
        private bool CanReviewNativeHouseholdAssignment()
        {
            var person=Hero.OneToOneConversationHero;
            return CanSpeakSocially() && ActualAdultHouseholdMember(Hero.MainHero,person,Clan.PlayerClan) &&
                NativeHouseholdAssignment(person).Length>0;
        }
        private void AssignHouseholdSpecialty(PartyRole role)
        {
            if(role==PartyRole.Surgeon && !CU80ManualSurgeonAllowed())
            {
                MBTextManager.SetTextVariable("CU_NATIVE_PARTY_DUTY_REPLY",new TextObject("We should check the party screen before issuing another duty."));
                return;
            }
            AssignHouseholdSpecialty(role,true);
        }
        private void AssignHouseholdSpecialty(PartyRole role,bool recordMemory)
        {
            string reply="I cannot take that duty just now. No appointment was confirmed.";
            if(CanAssignHouseholdSpecialty(role))
            {
                var actor=Hero.OneToOneConversationHero;var party=MobileParty.MainParty;
                var previous=NativeAssignmentSlots.Select(x=>party.GetRoleHolder(x)).ToArray();
                try{party.SetHeroPartyRole(actor,role);}
                catch(Exception error){DiagnosticJournal.Exception("cu_native_party_duty_failed",error);if(!recordMemory)throw;}
                bool assigned=party.GetRoleHolder(role)==actor && party.GetHeroPartyRoles(actor).Contains(role) && EffectiveSpecialist(party,role)==actor;
                bool othersUnchanged=NativeAssignmentSlots.Select((x,i)=>x==role || party.GetRoleHolder(x)==previous[i]).All(x=>x);
                bool confirmed=assigned && othersUnchanged;
                DiagnosticJournal.Event("cu_household_native_party_duty","result="+(confirmed?"PASS":"UNCONFIRMED")+
                    "|actor="+actor.StringId+"|party="+party.StringId+"|role="+role+"|native_role_confirmed="+assigned+"|other_specialists_unchanged="+othersUnchanged);
                if(confirmed)
                {
                    if(recordMemory)livingPersons.Remember("NativePartyDutyAssigned",Hero.MainHero.StringId,actor.StringId,"role="+role+"|day="+CurrentHouseholdDay());
                    reply="I now serve as your "+role.ToString().ToLowerInvariant()+". You can see the appointment in the party screen.";
                }
                else reply="The appointment was not fully confirmed. Review the native party screen before issuing another duty.";
            }
            MBTextManager.SetTextVariable("CU_NATIVE_PARTY_DUTY_REPLY",new TextObject(reply));
        }
        private void RegisterNobleOfficerConversations(
            CampaignGameStarter starter)
        {
            if(starter==null)return;
            starter.AddPlayerLine("cu_native_party_duties_open","hero_main_options","cu_native_party_duties_intro",
                "I have a responsibility for you.",()=>CanOfferHouseholdResponsibilities(),null,139);
            starter.AddPlayerLine("cu_native_party_duties_private","cu_personal_matters_topics","cu_native_party_duties_intro",
                "Let us arrange your responsibilities.",()=>CanOfferHouseholdResponsibilities(),null,139);
            starter.AddPlayerLine("cu_native_party_duties_concern","cu_personal_followup_choices","cu_native_party_duties_intro",
                "Then let us agree on a duty you can carry out.",()=>CanFollowPersonalTopic() &&
                    (personalTopicName=="Concerns" || personalTopicName=="Family") && CanOfferHouseholdResponsibilities() &&
                    !(personalTopicName=="Concerns" && CU80ContextualSurgeonAvailable()),null,139);
            starter.AddDialogLine("cu_native_party_duties_intro","cu_native_party_duties_intro","cu_native_party_duties_choices",
                "What duty do you wish me to undertake?",null,null,139);
            foreach(var specialty in HouseholdSpecialties)
            {
                var role=specialty;
                starter.AddPlayerLine("cu_native_party_duty_"+role,"cu_native_party_duties_choices","cu_native_party_duty_reply",
                    "Serve as our "+role.ToString().ToLowerInvariant()+".",()=>(role!=PartyRole.Surgeon || CU80ManualSurgeonAllowed()) && CanAssignHouseholdSpecialty(role),()=>AssignHouseholdSpecialty(role),139);
            }
            starter.AddPlayerLine("cu_native_party_duties_cancel","cu_native_party_duties_choices","close_window","Not now.",null,null,1);
            starter.AddDialogLine("cu_native_party_duty_reply","cu_native_party_duty_reply","close_window","{CU_NATIVE_PARTY_DUTY_REPLY}",null,null,139);
            starter.AddPlayerLine("cu_household_governor_order","cu_native_party_duties_choices","cu_household_governor_reply",
                "Will you govern this holding for our house?",()=>CanAskResidentToGovern(),()=>AskResidentToGovern(),140);
            starter.AddDialogLine("cu_household_governor_reply","cu_household_governor_reply","close_window",
                "{CU_HOUSEHOLD_GOVERNOR_REPLY}",null,null,140);
            starter.AddPlayerLine("cu_household_defense_order",
                "hero_main_options","cu_household_defense_order_reply",
                "Station five of my local recruits in this garrison.",
                ()=>HouseholdOrderSpeaker(),()=>ExecuteHouseholdDefenseOrder(),145);
            starter.AddDialogLine("cu_household_defense_order_reply",
                "cu_household_defense_order_reply","close_window",
                "{CU_HOUSEHOLD_ORDER_REPLY}",null,null,145);
            starter.AddPlayerLine("cu_lord_master_order",
                "hero_main_options","cu_lord_master_order_reply",
                "Master of Arms, station five of my recruits in this holding.",
                ()=>LordMasterAvailable(),
                ()=>TransferExistingTroopsByOfficer(false),100);
            starter.AddDialogLine("cu_lord_master_order_reply",
                "cu_lord_master_order_reply","close_window",
                "{CU_OFFICER_TRANSFER_REPLY}",
                null,null,100);
            starter.AddPlayerLine("cu_king_marshal_order",
                "hero_main_options","cu_king_marshal_order_reply",
                "Marshal, execute today's council garrison order.",
                ()=>KingMarshalAvailable(),
                ()=>TransferExistingTroopsByOfficer(true),100);
            starter.AddDialogLine("cu_king_marshal_order_reply",
                "cu_king_marshal_order_reply","close_window",
                "{CU_OFFICER_TRANSFER_REPLY}",
                null,null,100);
        }
    }
}

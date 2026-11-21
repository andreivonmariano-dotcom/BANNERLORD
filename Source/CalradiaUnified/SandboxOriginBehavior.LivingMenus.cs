using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    /// <summary>
    /// Small, native-style settlement menu. Vanilla Clan, Kingdom, game
    /// dialogue and encyclopedia screens retain their own authority.
    /// Actions are offered only with a real campaign and observed Heroes.
    /// </summary>
    public sealed partial class SandboxOriginBehavior
    {
        private static void ShowNativeDetail(string heading,string detail)
        {
            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    heading,detail,true,false,"Continue",null,
                    ()=>{},null),true);
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_native_people_inquiry_failed",error);
                Notify(detail);
            }
        }
        private void ConfirmCharacterSupport()
        {
            var actor=RequestedCharacter();
            if(actor==null)return;
            int fee=SandboxAgencyDecisionPolicy.Amount(
                agencyDecisions.PendingKind);
            if(fee<0)return;
            InformationManager.ShowInquiry(new InquiryData(
                "Support "+actor.Name.ToString(),
                fee==0?
                    "Acknowledge this person's work? The response "+
                    "affects their real native relationship with you.":
                    "Give "+fee+" of your actual denars to "+
                    actor.Name.ToString()+
                    "? This gift is processed by Bannerlord's "+
                    "native gold-transfer action.",
                true,true,"Confirm","Cancel",
                ()=>AnswerCharacterRequest(true),()=>{}),true);
        }
        private void ConfirmEstateInNativeDialog()
        {
            if(!CanConfirmFamilyGrant())return;
            var estate=Settlement.CurrentSettlement;
            InformationManager.ShowInquiry(new InquiryData(
                "Confirm family estate grant",
                "Transfer "+estate.Name.ToString()+
                " from the recorded birth clan to your native clan? "+
                "This change is permanent in the campaign and uses "+
                "Bannerlord's own settlement-ownership action. "+
                "The live family, clan leader, kingdom and remaining "+
                "family estates will be checked again.",
                true,true,"Confirm grant","Cancel",
                ()=>ConfirmFamilyGrant(),()=>{}),true);
        }
        private Hero FindLivingPerson(string id)
        {
            if(String.IsNullOrEmpty(id))return null;
            return Hero.AllAliveHeroes.FirstOrDefault(h=>h!=null &&
                h.StringId==id && h.IsAlive);
        }
        private static bool SamePlace(Hero player,Hero other)
        {
            if(player==null || other==null || player==other)return false;
            if(player.PartyBelongedTo!=null &&
                player.PartyBelongedTo==other.PartyBelongedTo)
                return true;
            var settlement=Settlement.CurrentSettlement;
            // HomeSettlement is not evidence of physical presence.
            return settlement!=null &&
                other.StayingInSettlement==settlement;
        }
        private Hero RequestedCharacter()
        {
            var target=FindLivingPerson(agencyDecisions.PendingHeroId);
            return SamePlace(Hero.MainHero,target)?target:null;
        }
        private bool HasRequest()
        {
            return agencyDecisions.HasPending &&
                RequestedCharacter()!=null;
        }
        private void ObserveAgencyDaily(Hero player)
        {
            agencyDecisions.Tick();
            if(agencyDecisions.HasPending &&
                FindLivingPerson(agencyDecisions.PendingHeroId)==null)
            {
                livingPersons.Remember("CharacterRequestExpired",
                    agencyDecisions.PendingHeroId,"",
                    "actor_no_longer_alive_or_loaded");
                agencyDecisions.Dismiss("Unavailable");
            }
            if(agencyDecisions.Day<15 ||
                agencyDecisions.Day%5!=0 || agencyDecisions.HasPending)
                return;
            OfferLocalCharacterRequest(player);
        }
        private void OfferLocalCharacterRequest(Hero player)
        {
            if(player==null || agencyDecisions.HasPending)return;
            foreach(var record in characterAgendas.Entries
                .OrderBy(a=>a.Id,StringComparer.Ordinal))
            {
                var hero=FindLivingPerson(record.Id);
                if(hero==null || hero==player ||
                    !SamePlace(player,hero) || hero.Age<18f)
                    continue;
                if(!livingPersons.People.Any(p=>p.Id==hero.StringId))
                    continue;
                var prior=agencyDecisions.For(hero.StringId);
                string kind=SandboxAgencyDecisionPolicy.Choose(
                    record.Goal,true,true,player.GetRelation(hero),
                    prior==null?0:prior.RequestsMade,
                    prior==null?1000:
                        agencyDecisions.Day-prior.LastDay);
                if(kind.Length==0)continue;
                if(agencyDecisions.Propose(hero.StringId,kind))
                {
                    livingPersons.Remember("IndependentCharacterRequest",
                        hero.StringId,player.StringId,
                        "kind="+kind+
                        "|source=character_goal_and_native_presence");
                    DiagnosticJournal.Event("cu_character_request_offered",
                        "hero="+hero.StringId+"|kind="+kind+
                        "|day="+agencyDecisions.Day);
                }
                break;
            }
        }
        private void DescribeCharacterRequest()
        {
            OfferLocalCharacterRequest(Hero.MainHero);
            var actor=RequestedCharacter();
            if(actor==null)
            {
                Notify("No reachable character has an outstanding request. "+
                    "Known people may bring matters to your attention "+
                    "after spending time together.");
                return;
            }
            int amount=SandboxAgencyDecisionPolicy.Amount(
                agencyDecisions.PendingKind);
            ShowNativeDetail(actor.Name.ToString()+" | Personal Matter",
                actor.Name.ToString()+" asks for 
                (agencyDecisions.PendingKind=="Recognition"?
                    "public acknowledgment of their work":
                    amount+" denars of household support")+
                ". Their personal goal is "+
                (characterAgendas.Get(actor.StringId)==null?"unknown":
                    characterAgendas.Get(actor.StringId).Goal)+
                ". This is a new request, not a fabricated earlier promise.");
        }
        private void AnswerCharacterRequest(bool accept)
        {
            var player=Hero.MainHero;
            var actor=RequestedCharacter();
            if(player==null || actor==null)
            {
                Notify("That person is not physically available here.");
                return;
            }
            string kind=agencyDecisions.PendingKind;
            int amount=SandboxAgencyDecisionPolicy.Amount(kind);
            if(amount<0)return;
            int relation=SandboxAgencyDecisionPolicy.RelationDelta(
                kind,accept);
            if(accept && amount>0)
            {
                if(!SandboxAgencyDecisionPolicy.NativeTransferPermitted(
                    kind,true,player.Gold,actor.IsAlive))
                {
                    Notify("You lack the denars for this support request.");
                    return;
                }
                int goldBefore=player.Gold;
                try
                {
                    GiveGoldAction.ApplyBetweenCharacters(
                        player,actor,amount,false);
                }
                catch(Exception error)
                {
                    DiagnosticJournal.Exception(
                        "cu_character_native_gift_failed",error);
                    Notify("The native gold transfer did not complete.");
                    return;
                }
                if(player.Gold>goldBefore-amount)
                {
                    Notify("The native gold transfer could not be verified.");
                    return;
                }
            }
            if(relation!=0)
            {
                try
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                        player,actor,relation,false);
                }
                catch(Exception error)
                {
                    DiagnosticJournal.Exception(
                        "cu_character_request_relation_failed",error);
                }
            }
            livingPersons.Remember(
                accept?"CharacterRequestAccepted":"CharacterRequestDeclined",
                actor.StringId,player.StringId,
                "kind="+kind+"|real_gold_transferred="+
                    (accept?amount:0)+"|native_relation_delta="+relation);
            DiagnosticJournal.Event("cu_character_request_resolved",
                "actor="+actor.StringId+
                "|choice="+(accept?"accepted":"declined")+
                "|type="+kind+
                "|gold="+(accept?amount:0));
            agencyDecisions.Dismiss(accept?"Accepted":"Declined");
            Notify(actor.Name.ToString()+": request "+
                (accept?"accepted.":"declined.")+
                (accept && amount>0?
                    " A native gold transfer was confirmed.":""));
        }
        private Clan RecordedBirthClan()
        {
            var player=Hero.MainHero;
            if(player==null || player.Father==null ||
                !successionInterest.IsRecorded ||
                player.StringId!=successionInterest.ClaimantId ||
                player.Father.StringId!=successionInterest.ParentId)
                return null;
            var clan=player.Father.Clan;
            return clan!=null &&
                clan.StringId==successionInterest.BirthClanId?
                    clan:null;
        }
        private static int NativeBirthHoldings(Clan clan)
        {
            return clan==null?0:clan.Settlements.Count(s=>s!=null &&
                s.IsFortification && s.OwnerClan==clan);
        }
        private Settlement CandidateClanGrant()
        {
            var clan=RecordedBirthClan();
            var current=Settlement.CurrentSettlement;
            if(clan==null || current==null ||
                !current.IsFortification ||
                current.OwnerClan!=clan)return null;
            // Retain the first native family stronghold for the senior
            // branch. The claimant may petition for a secondary holding.
            var primary=clan.Settlements.Where(s=>s!=null &&
                s.OwnerClan==clan && s.IsFortification)
                .OrderBy(s=>s.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
            return current==primary?null:current;
        }
        private bool SameNativeRealm(Clan clan)
        {
            return clan!=null && Clan.PlayerClan!=null &&
                Clan.PlayerClan!=clan &&
                clan.Kingdom!=null &&
                Clan.PlayerClan.Kingdom==clan.Kingdom;
        }
        private void DescribeFamilyClaim()
        {
            var clan=RecordedBirthClan();
            var player=Hero.MainHero;
            if(clan==null || player==null)
            {
                Notify("No verified native family claim exists.");
                return;
            }
            string leader=clan.Leader==null?"none":
                clan.Leader.Name.ToString();
            ShowNativeDetail("Family Council | "+clan.Name.ToString(),
                "Your native birth clan: "+clan.Name.ToString()+
                ". Family custom: "+successionInterest.CustomaryModel+
                ". Older sons at origin: "+
                successionInterest.EarlierSonsAtStart+
                ". Living strongholds: "+NativeBirthHoldings(clan)+
                ". Current clan head: "+leader+
                ". Claim: "+inheritanceNegotiation.Status+
                ". Clan-head consent threshold: "+
                SandboxInheritanceNegotiationPolicy.ConsentThreshold(
                    successionInterest.CultureId,
                    successionInterest.EarlierSonsAtStart)+
                " native relation. Cultural mediation payment: "+
                SandboxInheritanceNegotiationPolicy
                    .FamilySettlementContribution(
                        successionInterest.CultureId)+
                " denars (maximum two, made at a native family court)."+
                " A cadet must first join the same native kingdom "+
                "through normal game politics. Family grants require "+
                "death, clan-head consent, a secondary estate, and "+
                "your explicit confirmation.");
        }
        private void PetitionFamilyCouncil()
        {
            var player=Hero.MainHero;
            var clan=RecordedBirthClan();
            if(player==null || clan==null)
            {
                Notify("Your native birth clan is unavailable.");
                return;
            }
            var leader=clan.Leader;
            var estate=CandidateClanGrant();
            bool parentDead=player.Father!=null &&
                !player.Father.IsAlive;
            string outcome=SandboxInheritanceNegotiationPolicy.Evaluate(
                successionInterest.IsRecorded,parentDead,
                leader!=null && leader.IsAlive,
                estate!=null,SameNativeRealm(clan),
                NativeBirthHoldings(clan),
                successionInterest.EarlierSonsAtStart,
                leader==null? -100:player.GetRelation(leader),
                successionInterest.CultureId);
            string status=inheritanceNegotiation.Petition(
                outcome,estate==null?"":estate.StringId,
                leader==null?"":leader.StringId,
                clan.StringId,successionInterest.CustomaryModel,
                agencyDecisions.Day);
            livingPersons.Remember("FamilyCouncilPetition",
                player.StringId,
                leader==null?"":leader.StringId,
                "outcome="+outcome+"|culture="+
                    successionInterest.CustomaryModel+
                "|native_estate_transferred=false");
            DiagnosticJournal.Event("cu_native_family_petition",
                "result="+status+
                "|reason="+outcome+
                "|birth_clan="+clan.StringId+
                "|native_transfer=false");
            Notify("Family petition: "+status+
                ". "+(status=="ClanHeadConsents"?
                    "The modeled clan-head decision permits a secondary "+
                    "family grant. Review and explicitly confirm the "+
                    "native transfer at this same estate.":
                    "No property has changed. The native kingdom and "+
                    "clan rules remain in force."));
        }
        private bool CanOfferFamilyMediation()
        {
            var player=Hero.MainHero;
            var clan=RecordedBirthClan();
            return player!=null && clan!=null &&
                SandboxInheritanceNegotiationPolicy.MediationGate(
                    inheritanceNegotiation.Status,
                    inheritanceNegotiation.Mediations,
                    CandidateClanGrant()!=null,
                    SameNativeRealm(clan),
                    clan.Leader!=null && clan.Leader.IsAlive,
                    player.Gold,successionInterest.CultureId);
        }
        private void OfferFamilyMediation()
        {
            if(!CanOfferFamilyMediation())
            {
                Notify("Clan mediation requires a contested petition, "+
                    "a matching native kingdom, the secondary family "+
                    "court and enough actual denars.");
                return;
            }
            var player=Hero.MainHero;
            var leader=RecordedBirthClan().Leader;
            int amount=SandboxInheritanceNegotiationPolicy
                .FamilySettlementContribution(
                    successionInterest.CultureId);
            int before=player.Gold;
            try
            {
                GiveGoldAction.ApplyBetweenCharacters(
                    player,leader,amount,false);
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_family_mediation_native_payment_failed",error);
                Notify("The native family mediation payment failed.");
                return;
            }
            if(player.Gold>before-amount)
            {
                Notify("The payment was not confirmed by native gold.");
                return;
            }
            // The native gift occurred. Record it once even if relations
            // subsequently refuse to update, to prevent double spending.
            if(!inheritanceNegotiation.RecordMediation())
                return;
            int relationBefore=player.GetRelation(leader);
            try
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    player,leader,4,false);
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_family_mediation_relation_failed",error);
            }
            livingPersons.Remember("CulturalClanMediationPaid",
                player.StringId,leader.StringId,
                "denars="+amount+
                "|native_relation_before="+relationBefore+
                "|native_relation_after="+player.GetRelation(leader)+
                "|culture="+successionInterest.CustomaryModel+
                "|native_estate_transferred=false");
            DiagnosticJournal.Event("cu_cultural_family_mediation",
                "player="+player.StringId+"|head="+leader.StringId+
                "|payment="+amount+
                "|mediations="+inheritanceNegotiation.Mediations+
                "|native_fief_transferred=false");
            Notify("Family mediation delivered: "+amount+
                " native denars. The clan head's relation has been "+
                "updated through the game. Petition again after "+
                "checking the family council's terms.");
        }
        private void WithdrawFamilyClaim()
        {
            if(!SandboxInheritanceNegotiationPolicy.MayWithdraw(
                inheritanceNegotiation.Status))return;
            string result=inheritanceNegotiation.Withdraw();
            livingPersons.Remember("FamilyClaimWithdrawn",
                Hero.MainHero.StringId,
                successionInterest.BirthClanId,
                "result="+result+
                "|native_property_unchanged=true");
            Notify("The outstanding family claim has been withdrawn. "+
                "Existing native ownership remains unchanged.");
        }
        private bool CanConfirmFamilyGrant()
        {
            var clan=RecordedBirthClan();
            var player=Hero.MainHero;
            var estate=Settlement.CurrentSettlement;
            if(player==null || clan==null || estate==null ||
                !inheritanceNegotiation.Authorized ||
                estate.StringId!=inheritanceNegotiation.SettlementId ||
                clan.StringId!=inheritanceNegotiation.BirthClanId ||
                clan.Leader==null ||
                !clan.Leader.IsAlive ||
                clan.Leader.StringId!=inheritanceNegotiation.LeaderId)
                return false;
            int relation=player.GetRelation(clan.Leader);
            string outcome=SandboxInheritanceNegotiationPolicy.Evaluate(
                true,player.Father!=null && !player.Father.IsAlive,
                true,CandidateClanGrant()==estate,
                SameNativeRealm(clan),NativeBirthHoldings(clan),
                successionInterest.EarlierSonsAtStart,relation,
                successionInterest.CultureId);
            return outcome=="ClanHeadConsents" &&
                SandboxInheritanceNegotiationPolicy.TransferGate(
                    true,inheritanceNegotiation.Authorized,
                    clan.StringId,estate.OwnerClan==null?"":
                        estate.OwnerClan.StringId,
                    SameNativeRealm(clan),
                    player.Father!=null && !player.Father.IsAlive,
                    NativeBirthHoldings(clan));
        }
        private void ConfirmFamilyGrant()
        {
            if(!CanConfirmFamilyGrant())
            {
                inheritanceNegotiation.Cancel();
                Notify("The native family, leader, allegiance or holding "+
                    "has changed. Clan consent must be reviewed again.");
                return;
            }
            var player=Hero.MainHero;
            var estate=Settlement.CurrentSettlement;
            var priorClan=estate.OwnerClan;
            try
            {
                ChangeOwnerOfSettlementAction.ApplyByDefault(
                    player,estate);
                bool confirmed=estate.OwnerClan==Clan.PlayerClan &&
                    estate.OwnerClan!=priorClan;
                string result=inheritanceNegotiation.Complete(confirmed);
                livingPersons.Remember(
                    confirmed?"NativeFamilyGrantCompleted":
                        "NativeFamilyGrantUnconfirmed",
                    player.StringId,estate.StringId,
                    "result="+result+"|old_owner="+priorClan.StringId+
                    "|native_action=ChangeOwnerOfSettlementAction");
                DiagnosticJournal.Event("cu_family_native_fief_grant",
                    "result="+result+
                    "|settlement="+estate.StringId+
                    "|from="+priorClan.StringId+
                    "|to="+(estate.OwnerClan==null?"none":
                        estate.OwnerClan.StringId));
                Notify(confirmed?
                    "Native family grant confirmed. "+
                        estate.Name.ToString()+
                        " now belongs to your clan.":
                    "The native owner did not change. No inheritance "+
                        "completion has been recorded.");
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_family_native_grant_action_failed",error);
                Notify("Native ownership action failed. The proposed "+
                    "estate was not recorded as inherited.");
            }
        }
        private int selectedLivingPerson;
        private void DescribeSelectedPerson()
        {
            if(livingPersons.People.Count==0)
            {
                Notify("No campaign household dossier has been recorded.");
                return;
            }
            selectedLivingPerson=(selectedLivingPerson+
                livingPersons.People.Count)%livingPersons.People.Count;
            var person=livingPersons.People[selectedLivingPerson];
            var agenda=characterAgendas.Get(person.Id);
            var hero=FindLivingPerson(person.Id);
            ShowNativeDetail("Household Register | "+person.Name,
                person.Name+" | "+person.Occupation+
                ", "+person.Station+". Native culture: "+person.Culture+
                ", recorded clan: "+person.HouseholdId+
                ". Personal ambition: "+(agenda==null?
                    person.Ambition:agenda.Goal)+
                ". Home: "+(agenda==null ||
                    agenda.HomeId.Length==0?
                    "unverified":agenda.HomeId)+" ("+
                    (agenda==null?"unknown":agenda.HomeEvidence)+"). "+
                (hero==null?"No current live-Hero observation. ":
                    "Current native relation to you: "+
                    Hero.MainHero.GetRelation(hero)+". ")+
                "See the vanilla Clan screen for full native family "+
                "membership and military status.");
        }
        private void RegisterPeopleAndLineageMenus(
            CampaignGameStarter starter)
        {
            starter.AddGameMenu("cu_people_lineage",
                "Calradia Unified | People & Lineage. "+
                "This uses the vanilla settlement menu. The vanilla Clan "+
                "and Kingdom screens retain political and family authority.",
                args=>{OfferLocalCharacterRequest(Hero.MainHero);});
            starter.AddGameMenu("cu_people_household",
                "Household Register | Actual Hero records, observed homes, "+
                "occupations, personal goals and native relationships.",
                args=>{});
            starter.AddGameMenu("cu_people_requests",
                "Personal Matters | Character-initiated requests. "+
                "Gold and relationships change only through native actions.",
                args=>{OfferLocalCharacterRequest(Hero.MainHero);});
            starter.AddGameMenu("cu_people_claim",
                "Family Council | Cultural claim review, contested "+
                "petitions and conditional native estate grants.",
                args=>{});
            foreach(string location in new[]{"town","castle"})
            {
                string from=location;
                starter.AddGameMenuOption(from,
                    "cu_people_lineage_entry_"+from,
                    "CU: People & Lineage",
                    args=>livingPersons.People.Count>0 &&
                        Hero.MainHero!=null,
                    args=>{
                        affairsReturnMenu=from;
                        GameMenu.SwitchToMenu("cu_people_lineage");
                    },false);
            }
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_people_open_household",
                "Household register and character ambitions",
                args=>livingPersons.People.Count>0,
                args=>GameMenu.SwitchToMenu("cu_people_household"),false);
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_people_open_requests",
                "Personal matters and character requests",
                args=>characterAgendas.Entries.Count>0,
                args=>GameMenu.SwitchToMenu("cu_people_requests"),false);
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_people_open_claim",
                "Family council and native estate claims",
                args=>successionInterest.IsRecorded,
                args=>GameMenu.SwitchToMenu("cu_people_claim"),false);
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_people_exit","Return to vanilla settlement menu",
                args=>true,
                args=>GameMenu.SwitchToMenu(affairsReturnMenu),false);

            starter.AddGameMenuOption("cu_people_household",
                "cu_household_view","Review selected household member",
                args=>livingPersons.People.Count>0,
                args=>DescribeSelectedPerson(),false);
            starter.AddGameMenuOption("cu_people_household",
                "cu_household_next","Next household member",
                args=>livingPersons.People.Count>1,
                args=>{
                    selectedLivingPerson=(selectedLivingPerson+1)%
                        livingPersons.People.Count;
                    DescribeSelectedPerson();
                },false);
            starter.AddGameMenuOption("cu_people_household",
                "cu_household_back","Return to People & Lineage",
                args=>true,
                args=>GameMenu.SwitchToMenu("cu_people_lineage"),false);

            starter.AddGameMenuOption("cu_people_requests",
                "cu_people_request","Hear the pending request",
                args=>HasRequest(),
                args=>DescribeCharacterRequest(),false);
            starter.AddGameMenuOption("cu_people_requests",
                "cu_people_accept","Provide the requested support",
                args=>HasRequest(),
                args=>ConfirmCharacterSupport(),false);
            starter.AddGameMenuOption("cu_people_requests",
                "cu_people_decline","Decline the request",
                args=>HasRequest(),
                args=>AnswerCharacterRequest(false),false);
            starter.AddGameMenuOption("cu_people_requests",
                "cu_people_no_requests","Check for local requests",
                args=>!HasRequest(),
                args=>DescribeCharacterRequest(),false);
            starter.AddGameMenuOption("cu_people_requests",
                "cu_people_requests_back","Return to People & Lineage",
                args=>true,
                args=>GameMenu.SwitchToMenu("cu_people_lineage"),false);

            starter.AddGameMenuOption("cu_people_claim",
                "cu_family_claim_review","Review native family claim",
                args=>successionInterest.IsRecorded,
                args=>DescribeFamilyClaim(),false);
            starter.AddGameMenuOption("cu_people_claim",
                "cu_family_petition",
                "Petition clan head for a secondary family holding",
                args=>successionInterest.IsRecorded &&
                    !inheritanceNegotiation.Authorized &&
                    inheritanceNegotiation.Attempts<3 &&
                    inheritanceNegotiation.Status!=
                        "NativeGrantCompleted" &&
                    inheritanceNegotiation.Status!=
                        "ClaimWithdrawn" &&
                    CandidateClanGrant()!=null,
                args=>PetitionFamilyCouncil(),false);
            starter.AddGameMenuOption("cu_people_claim",
                "cu_family_mediation",
                "Offer the clan's cultural mediation contribution",
                args=>CanOfferFamilyMediation(),
                args=>OfferFamilyMediation(),false);
            starter.AddGameMenuOption("cu_people_claim",
                "cu_family_withdraw",
                "Withdraw this family's outstanding claim",
                args=>SandboxInheritanceNegotiationPolicy.MayWithdraw(
                    inheritanceNegotiation.Status),
                args=>WithdrawFamilyClaim(),false);
            starter.AddGameMenuOption("cu_people_claim",
                "cu_family_grant",
                "Confirm negotiated native family estate grant",
                args=>CanConfirmFamilyGrant(),
                args=>ConfirmEstateInNativeDialog(),false);
            starter.AddGameMenuOption("cu_people_claim",
                "cu_family_claim_back","Return to People & Lineage",
                args=>true,
                args=>GameMenu.SwitchToMenu("cu_people_lineage"),false);
        }
    }
}

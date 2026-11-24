using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
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
            return target!=null && target.IsActive && !target.IsPrisoner &&
                target.Age>=18f && SamePlace(Hero.MainHero,target)?target:null;
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
                .OrderBy(a=>agencyDecisions.For(a.Id)==null?0:
                    agencyDecisions.For(a.Id).RequestsMade)
                .ThenBy(a=>agencyDecisions.For(a.Id)==null?-1:
                    agencyDecisions.For(a.Id).LastDay)
                .ThenBy(a=>a.Id,StringComparer.Ordinal))
            {
                var hero=FindLivingPerson(record.Id);
                if(hero==null || hero==player ||
                    !SamePlace(player,hero) || hero.Age<18f ||
                    !hero.IsActive || hero.IsPrisoner)
                    continue;
                if(!livingPersons.People.Any(p=>p.Id==hero.StringId))
                    continue;
                var prior=agencyDecisions.For(hero.StringId);
                int nativeRelation=player.GetRelation(hero);
                if(!SandboxAgencyDecisionPolicy.CanReapproach(
                    socialBonds.For(hero.StringId),nativeRelation))continue;
                string kind=SandboxAgencyDecisionPolicy.ChooseInitiative(
                    record.Goal,true,true,nativeRelation,
                    prior==null?0:prior.RequestsMade,
                    prior==null?1000:
                        agencyDecisions.Day-prior.LastDay,
                    hero.GetTraitLevel(DefaultTraits.Honor),
                    hero.GetTraitLevel(DefaultTraits.Generosity),
                    hero.GetTraitLevel(DefaultTraits.Calculating),
                    hero.Gold);
                if(kind.Length==0)continue;
                if(agencyDecisions.Propose(hero.StringId,kind))
                {
                    RememberPetitionStory(hero,kind);
                    livingPersons.Remember("IndependentCharacterRequest",
                        hero.StringId,player.StringId,
                        "kind="+kind+
                        "|source=modeled_npc_initiative"+
                        "|native_evidence=goal_traits_wealth_presence"+
                        "|not_a_claim_of_native_ai_dialogue=true");
                    DiagnosticJournal.Event("cu_character_request_offered",
                        "hero="+hero.StringId+"|kind="+kind+
                        "|day="+agencyDecisions.Day);
                    break;
                }
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
                actor.Name.ToString()+" asks for "+
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
            if(player==null || actor==null ||
                Hero.OneToOneConversationHero!=actor)
            {
                Notify("Speak with the person directly to answer "+
                    "their petition. No remote decision was made.");
                return;
            }
            string kind=agencyDecisions.PendingKind;
            int amount=SandboxAgencyDecisionPolicy.Amount(kind);
            if(amount<0)return;
            int requestedRelation=SandboxAgencyDecisionPolicy.RelationDelta(
                kind,accept);
            string giftProof="not_requested";
            if(accept && amount>0)
            {
                if(!SandboxAgencyDecisionPolicy.NativeTransferPermitted(
                    kind,true,player.Gold,actor.IsAlive))
                {
                    Notify("You lack the denars for this support request.");
                    return;
                }
                int payerBefore=player.Gold;
                int recipientBefore=actor.Gold;
                try
                {
                    GiveGoldAction.ApplyBetweenCharacters(
                        player,actor,amount,false);
                }
                catch(Exception error)
                {
                    DiagnosticJournal.Exception(
                        "cu_character_native_gift_failed",error);
                }
                int payerAfter=player.Gold;
                int recipientAfter=actor.Gold;
                giftProof=SandboxNativeEffectProof.GiftResult(
                    payerBefore,payerAfter,recipientBefore,recipientAfter,
                    amount);
                if(giftProof!="confirmed")
                {
                    livingPersons.Remember("CharacterGiftUnverified",
                        actor.StringId,player.StringId,
                        "gift_status="+giftProof+
                        "|payer_before="+payerBefore+
                        "|payer_after="+payerAfter+
                        "|recipient_before="+recipientBefore+
                        "|recipient_after="+recipientAfter+
                        "|no_accepted_request_claim=true");
                    DiagnosticJournal.Event("cu_character_native_gift_unverified",
                        "actor="+actor.StringId+
                        "|status="+giftProof+
                        "|payer_delta="+(payerBefore-payerAfter)+
                        "|recipient_delta="+(recipientAfter-recipientBefore));
                    // Never take another payment for an uncertain native
                    // transfer. An entirely unchanged transfer can be
                    // attempted later at the player's choice.
                    if(giftProof!="no_native_effect")
                        agencyDecisions.Dismiss("UnverifiedNativeGift");
                    Notify("Both native gold balances did not confirm "+
                        "the intended gift. No acceptance was recorded.");
                    return;
                }
            }
            int relationBefore=player.GetRelation(actor);
            if(requestedRelation!=0)
            {
                try
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                        player,actor,requestedRelation,false);
                }
                catch(Exception error)
                {
                    DiagnosticJournal.Exception(
                        "cu_character_request_relation_failed",error);
                }
            }
            int relationAfter=player.GetRelation(actor);
            bool exactRelation=SandboxNativeEffectProof.RelationConfirmed(
                relationBefore,relationAfter,requestedRelation);
            bool relationProof=SandboxNativeEffectProof.RelationDirectionObserved(
                relationBefore,relationAfter,requestedRelation);
            int actualRelation=SandboxNativeEffectProof.ObservedDelta(
                relationBefore,relationAfter);
            livingPersons.Remember(
                relationProof?
                    (accept?"CharacterRequestAccepted":
                        "CharacterRequestDeclined"):
                    "CharacterRequestRelationUnverified",
                actor.StringId,player.StringId,
                "kind="+kind+
                "|choice="+(accept?"accept":"decline")+
                "|native_gift_status="+giftProof+
                "|verified_denars="+(giftProof=="confirmed"?amount:0)+
                "|relation_before="+relationBefore+
                "|relation_after="+relationAfter+
                "|actual_relation_delta="+actualRelation+
                "|relation_confirmed="+relationProof+
                "|relation_exact="+exactRelation);
            DiagnosticJournal.Event("cu_character_request_resolved",
                "actor="+actor.StringId+
                "|choice="+(accept?"accepted":"declined")+
                "|type="+kind+
                "|gift_proof="+giftProof+
                "|relation_proof="+relationProof+
                "|relation_exact="+exactRelation+
                "|actual_relation_delta="+actualRelation);
            ResolvePetitionStory(actor,accept,kind,
                giftProof=="confirmed",actualRelation,relationProof);
            // A confirmed payment must never be duplicated if a following
            // native relation action fails. The choice is terminal.
            agencyDecisions.Dismiss(relationProof?
                (accept?"Accepted":"Declined"):
                "NativeRelationUnverified");
            Notify(actor.Name.ToString()+": "+
                (relationProof?
                    (actualRelation>0?
                        "your standing has improved.":
                        actualRelation<0?
                        "your standing has declined.":
                        "your answer has been recorded."):
                    "the change in standing could not be confirmed.")+
                (giftProof=="confirmed"?
                    " The promised denars were transferred.":""));
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
                ". Negotiation readiness relation threshold: "+
                SandboxInheritanceNegotiationPolicy.ConsentThreshold(
                    successionInterest.CultureId,
                    successionInterest.EarlierSonsAtStart)+
                " native relation. Cultural mediation payment: "+
                SandboxInheritanceNegotiationPolicy
                    .FamilySettlementContribution(
                        successionInterest.CultureId)+
                " denars (maximum two, made at a native family court)."+
                " A cadet must join the native kingdom through normal "+
                "politics. Favorable relations alone are NOT game-backed "+
                "clan-head consent. Property transfer is disabled until "+
                "an actual native clan-head decision can be observed.");
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
                    "The family model finds this claim negotiable. "+
                    "This is NOT observed consent from the native clan "+
                    "head. No estate transfer is authorized.":
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
            int payerBefore=player.Gold;
            int recipientBefore=leader.Gold;
            try
            {
                GiveGoldAction.ApplyBetweenCharacters(
                    player,leader,amount,false);
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_family_mediation_native_payment_failed",error);
            }
            int payerAfter=player.Gold;
            int recipientAfter=leader.Gold;
            string paymentProof=SandboxNativeEffectProof.GiftResult(
                payerBefore,payerAfter,recipientBefore,recipientAfter,
                amount);
            if(paymentProof!="confirmed")
            {
                // If any native funds changed, consume a mediation slot.
                // The player must not be silently charged again.
                if(paymentProof!="no_native_effect")
                    inheritanceNegotiation.RecordMediation();
                livingPersons.Remember("CulturalMediationPaymentUnverified",
                    player.StringId,leader.StringId,
                    "status="+paymentProof+
                    "|payer_before="+payerBefore+
                    "|payer_after="+payerAfter+
                    "|recipient_before="+recipientBefore+
                    "|recipient_after="+recipientAfter+
                    "|native_fief_transferred=false");
                Notify("The native transfer did not confirm both "+
                    "the payer and recipient balances. "+
                    "No relation benefit has been claimed.");
                return;
            }
            // The two native Hero balances confirm delivery. Record once
            // even if relation change fails, preventing repeat payment.
            if(!inheritanceNegotiation.RecordMediation())
            {
                livingPersons.Remember("CulturalMediationNativePaymentOrphaned",
                    player.StringId,leader.StringId,
                    "verified_denars="+amount+
                    "|mediation_slot_not_recorded=true");
                Notify("Native funds moved, but the mediation limit "+
                    "could not be updated. Do not pay again.");
                return;
            }
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
            int relationAfter=player.GetRelation(leader);
            bool relationConfirmed=SandboxNativeEffectProof.RelationConfirmed(
                relationBefore,relationAfter,4);
            livingPersons.Remember("CulturalClanMediationPaid",
                player.StringId,leader.StringId,
                "verified_denars="+amount+
                "|native_relation_before="+relationBefore+
                "|native_relation_after="+relationAfter+
                "|actual_relation_delta="+
                    SandboxNativeEffectProof.ObservedDelta(
                        relationBefore,relationAfter)+
                "|relation_confirmed="+relationConfirmed+
                "|culture="+successionInterest.CustomaryModel+
                "|native_estate_transferred=false");
            DiagnosticJournal.Event("cu_cultural_family_mediation",
                "player="+player.StringId+"|head="+leader.StringId+
                "|verified_payment="+amount+
                "|mediations="+inheritanceNegotiation.Mediations+
                "|relation_confirmed="+relationConfirmed+
                "|native_fief_transferred=false");
            Notify(relationConfirmed?
                "Native denar delivery and clan-head relation gain "+
                    "confirmed. Review the family council terms.":
                "Native denars were delivered. The intended clan-head "+
                    "relation gain was not verified.");
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
                !inheritanceNegotiation.NativeHeadConsentObserved ||
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
        private string claimReturnMenu="cu_people_lineage";

        private void RegisterPeopleAndLineageMenus(
            CampaignGameStarter starter)
        {
            starter.AddGameMenu("cu_people_lineage",
                "You gather the people of your own house at court. "+
                "Consult the household roll, receive your officers "+
                "and review matters of land and inheritance. "+
                "Other families must be spoken to in person.",
                args=>{OfferLocalCharacterRequest(Hero.MainHero);});
            starter.AddGameMenu("cu_people_household",
                "The household roll. Names, places and ambitions "+
                "belong to people living in your campaign.",
                args=>{});
            starter.AddGameMenu("cu_people_requests",
                "Private petitions from people close to your house. "+
                "Your reply can change actual denars and relationships.",
                args=>{OfferLocalCharacterRequest(Hero.MainHero);});
            starter.AddGameMenu("cu_people_claim",
                "Family council. Hear claims to estates, review "+
                "inheritance custom and seek a lawful settlement.",
                args=>{});
            foreach(string location in new[]{"town","castle"})
            {
                string from=location;
                starter.AddGameMenuOption(from,
                    "cu_people_lineage_entry_"+from,
                    "Hold household court",
                    args=>applied && Hero.MainHero!=null &&
                        Clan.PlayerClan!=null &&
                        Settlement.CurrentSettlement!=null &&
                        Settlement.CurrentSettlement.IsFortification &&
                        !Settlement.CurrentSettlement.IsUnderSiege &&
                        Settlement.CurrentSettlement.OwnerClan==
                            Clan.PlayerClan,
                    args=>{
                        affairsReturnMenu=from;
                        GameMenu.SwitchToMenu("cu_people_lineage");
                    },false);
                // A cadet claiming an estate must still reach the real
                // birth family's hall, not a foreign neutral court or
                // a fictional remote-clan command.
                starter.AddGameMenuOption(from,
                    "cu_birth_family_council_entry_"+from,
                    "Request an audience with my birth family",
                    args=>successionInterest.IsRecorded &&
                        RecordedBirthClan()!=null &&
                        Settlement.CurrentSettlement!=null &&
                        Settlement.CurrentSettlement.IsFortification &&
                        !Settlement.CurrentSettlement.IsUnderSiege &&
                        Settlement.CurrentSettlement.OwnerClan==
                            RecordedBirthClan(),
                    args=>{
                        claimReturnMenu=from;
                        GameMenu.SwitchToMenu("cu_people_claim");
                    },false);
            }
            // Recoverable failures are still accessible, but never
            // occupy a separate top-level settlement option.
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_partial_family_review",
                "Review incomplete starting household",
                args=>familyRecovery.NeedsReview &&
                    Hero.MainHero!=null,
                args=>ShowNativeDetail(
                    "Starting Life | Native family review",
                    familyRecovery.Describe()),false);
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_role_economy_observed",
                "My livelihood and obligations",
                args=>roleEconomy.Started,
                args=>ShowNativeDetail(
                    "Starting Role | Verified livelihood record",
                    roleEconomy.Describe()),false);
            // This is a real, read-only native inquiry. Its spouse,
            // child, companion and service status comes from live Heroes
            // and saved campaign records, not an old creator .ini file.
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_starting_household_native_status",
                "My household and present calling",
                args=>Hero.MainHero!=null &&
                    (knightOath.IsConfigured ||
                     lordInheritance.IsRecorded ||
                     kingSuccession.IsRecorded ||
                     roleEconomy.Started),
                args=>ShowStartingHouseholdStatus(),false);
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_household_duty_ledger",
                "My household officers and their duties",
                args=>Hero.MainHero!=null &&
                    Clan.PlayerClan!=null &&
                    Clan.PlayerClan.Companions!=null &&
                    Clan.PlayerClan.Companions.Count>0,
                args=>ShowHouseholdDutyLedger(),false);
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_family_native_audiences",
                "Arrange a family audience",
                args=>Hero.MainHero!=null &&
                    (Hero.MainHero.Spouse!=null ||
                     (Hero.MainHero.Children!=null &&
                      Hero.MainHero.Children.Any(x=>x!=null &&
                          x.IsAlive && x.Age>=18f))),
                args=>ShowNativeDetail(
                    "Household | Family audiences",
                    "Meet your living spouse or grown child in "+
                    "the game's actual conversation. "+
                    "A witnessed family audience can improve "+
                    "your real relationship once per thirty "+
                    "days. Gifts of 100 native denars are "+
                    "separate voluntary decisions. Children "+
                    "under eighteen are not adult courtiers."),
                false);
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_people_open_household",
                "People of my household",
                args=>livingPersons.People.Count>0,
                args=>GameMenu.SwitchToMenu("cu_people_household"),false);
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_people_open_requests",
                "Private petitions and favors",
                args=>false,
                args=>GameMenu.SwitchToMenu("cu_people_requests"),false);
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_people_open_claim",
                "Family council and inheritance",
                args=>successionInterest.IsRecorded,
                args=>{
                    claimReturnMenu="cu_people_lineage";
                    GameMenu.SwitchToMenu("cu_people_claim");
                },false);
            starter.AddGameMenuOption("cu_people_lineage",
                "cu_people_exit","Return to settlement",
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
                "cu_people_request","Read the pending correspondence",
                args=>agencyDecisions.HasPending,
                args=>HearDossierPetition(),false);
            starter.AddGameMenuOption("cu_people_requests",
                "cu_people_accept","Provide the requested support",
                args=>false,
                args=>{},false);
            starter.AddGameMenuOption("cu_people_requests",
                "cu_people_decline","Decline the request",
                args=>false,
                args=>{},false);
            starter.AddGameMenuOption("cu_people_requests",
                "cu_people_no_requests","Read private correspondence",
                args=>!agencyDecisions.HasPending,
                args=>HearDossierPetition(),false);
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
                args=>GameMenu.SwitchToMenu(claimReturnMenu),false);
        }
    }
}

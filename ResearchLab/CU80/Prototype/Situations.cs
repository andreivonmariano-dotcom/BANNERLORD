using System;
namespace CU80.Lab
{
    public enum SituationKind { None, GovernorFood, GovernorLoyalty, QuartermasterSupply, FamilyAdulthood, FamilyReunion, FinancialPetition }
    [Flags] public enum ResponseOptions { None=0, ReviewNative=1, ExistingAppointment=2, Acknowledge=4, AcceptExistingPetition=8, Decline=16, Defer=32 }
    public sealed class Situation
    {
        public readonly SituationKind Kind;
        public readonly ResponseOptions Options;
        public readonly string Player, Speaker, Subject, Evidence;
        internal Situation(SituationKind kind,ResponseOptions options,string player,string speaker,string subject,string evidence)
        {Kind=kind;Options=options;Player=player;Speaker=speaker;Subject=subject;Evidence=evidence;}
    }
    // Pure contextual policy. No native mutators, no invented reward, no parallel economy.
    public static class Situations
    {
        static bool Id(string s){return !String.IsNullOrWhiteSpace(s);}
        static bool Pair(string p,string a){return Id(p)&&Id(a)&&p!=a;}
        static bool Finite(double n){return !Double.IsNaN(n)&&!Double.IsInfinity(n);}
        const ResponseOptions Exit=ResponseOptions.Decline|ResponseOptions.Defer;
        // Thresholds are UI relevance hypotheses, not native economic laws.
        public static Situation Governor(string p,string a,string town,bool known,bool owned,bool reciprocalGovernor,
            bool local,bool safe,double food,double loyalty)
        {
            if(!Pair(p,a)||!Id(town)||!known||!owned||!reciprocalGovernor||!local||!safe||
                !Finite(food)||!Finite(loyalty)||food<0||loyalty<0||loyalty>100)return null;
            if(food<20)return new Situation(SituationKind.GovernorFood,ResponseOptions.ReviewNative|Exit,p,a,town,"low observed food stocks");
            if(loyalty<30)return new Situation(SituationKind.GovernorLoyalty,ResponseOptions.ReviewNative|Exit,p,a,town,"low observed loyalty");
            return null;
        }
        // foodChange must be the active model's verified current daily quote, not a reconstructed base formula.
        public static Situation Quartermaster(string p,string a,string party,bool known,bool existingGuard,
            bool vacantRaw,bool actorHasNoRole,int candidateSteward,int effectiveSteward,double food,double foodChange)
        {
            if(!Pair(p,a)||!Id(party)||!known||!existingGuard||!vacantRaw||!actorHasNoRole||
                candidateSteward<0||effectiveSteward<0||candidateSteward<=effectiveSteward||
                !Finite(food)||!Finite(foodChange)||food<0||foodChange>=0)return null;
            if(food/-foodChange>3)return null;
            return new Situation(SituationKind.QuartermasterSupply,ResponseOptions.ExistingAppointment|Exit,p,a,party,"observed food and current consumption quote");
        }
        // Input from existing life-history exact pair evidence. A baseline is never a milestone.
        public static Situation Family(string p,string a,string relative,string eventId,string transition,
            bool witnessed,bool reciprocalKin,bool localAdultSpeaker,bool currentStateMatches,bool alreadyAcknowledged,int daysSince)
        {
            if(!Pair(p,a)||!Id(relative)||relative==p||!Id(eventId)||!witnessed||!reciprocalKin||
                !localAdultSpeaker||!currentStateMatches||alreadyAcknowledged||daysSince<0||daysSince>=7)return null;
            // Narrow first scope: the speaker is the relative, not an inferred third-party witness.
            if(a!=relative)return null;
            if(transition=="Matured"||transition=="Adulthood")
                return new Situation(SituationKind.FamilyAdulthood,ResponseOptions.Acknowledge|Exit,p,a,relative,eventId);
            if(transition=="Reunited")
                return new Situation(SituationKind.FamilyReunion,ResponseOptions.Acknowledge|Exit,p,a,relative,eventId);
            return null;
        }
        public static Situation Petition(string p,string a,string request,string kind,bool known,bool pending,
            bool local,bool exactSpeaker,bool previouslyAttempted,long payerGold,long recipientGold,int quotedCost)
        {
            if(!Pair(p,a)||!Id(request)||!known||!pending||!local||!exactSpeaker||previouslyAttempted||
                payerGold<0||recipientGold<0||quotedCost<=0||
                (kind!="HouseholdAid"&&kind!="LivelihoodAid"))return null;
            var options=Exit;
            if(payerGold>=quotedCost)options|=ResponseOptions.AcceptExistingPetition;
            return new Situation(SituationKind.FinancialPetition,options,p,a,request,"existing local financial request");
        }
        // Only exact native debit and credit in one independently attributed command window.
        // Relation delta and subsequent livelihood are intentionally not inferred.
        public static bool ConfirmsPetition(string expectedRequest,string actualRequest,string expectedPayer,string actualPayer,
            string expectedRecipient,string actualRecipient,bool commandWasInvokedOnce,bool pendingBefore,bool resolvedAfter,
            long payerBefore,long payerAfter,long recipientBefore,long recipientAfter,int cost)
        {
            return Id(expectedRequest)&&expectedRequest==actualRequest&&Pair(expectedPayer,expectedRecipient)&&
                expectedPayer==actualPayer&&expectedRecipient==actualRecipient&&commandWasInvokedOnce&&pendingBefore&&resolvedAfter&&
                cost>0&&payerBefore>=cost&&payerAfter>=0&&recipientBefore>=0&&recipientAfter>=recipientBefore&&
                payerBefore-payerAfter==cost&&recipientAfter-recipientBefore==cost;
        }
    }
}
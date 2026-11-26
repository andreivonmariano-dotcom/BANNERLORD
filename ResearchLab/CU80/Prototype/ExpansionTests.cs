using System;
using CU80.Lab;
public static class ExpansionTests
{
    static int count;
    static void C(string n,bool ok){if(!ok)throw new Exception("FAIL "+n);count++;Console.WriteLine("PASS "+n);}
    static Situation G(double f=18,double l=24,bool owner=true,bool governor=true,bool local=true,bool safe=true,bool known=true)
    {return Situations.Governor("p","g","town",known,owner,governor,local,safe,f,l);}
    static Situation Q(double food=3,double delta=-1,int skill=80,int current=20,bool guard=true,bool vacant=true,bool noRole=true,bool known=true)
    {return Situations.Quartermaster("p","a","party",known,guard,vacant,noRole,skill,current,food,delta);}
    static Situation F(string change="Matured",bool witnessed=true,bool kin=true,bool local=true,bool state=true,bool ack=false,int age=1,string speaker="child",string relative="child")
    {return Situations.Family("p",speaker,relative,"event1",change,witnessed,kin,local,state,ack,age);}
    static Situation P(long gold=1000,int cost=100,bool pending=true,bool local=true,bool speaker=true,bool attempted=false,string kind="HouseholdAid")
    {return Situations.Petition("p","a","req",kind,true,pending,local,speaker,attempted,gold,10,cost);}
    static bool R(long payerAfter=900,long recipientAfter=110,string request="req",string recipient="a",bool once=true,bool pending=true,bool resolved=true)
    {return Situations.ConfirmsPetition("req",request,"p","p","a",recipient,once,pending,resolved,1000,payerAfter,10,recipientAfter,100);}
    public static void Main()
    {
        C("governor actual food concern",G().Kind==SituationKind.GovernorFood);
        C("governor actual loyalty concern",G(f:50).Kind==SituationKind.GovernorLoyalty);
        C("governor review not mutation",G().Options==(ResponseOptions.ReviewNative|ResponseOptions.Decline|ResponseOptions.Defer));
        C("governor healthy state no filler",G(f:50,l:60)==null);
        C("governor other owner",G(owner:false)==null);
        C("governor nonreciprocal role",G(governor:false)==null);
        C("governor absent",G(local:false)==null);
        C("governor unsafe",G(safe:false)==null);
        C("governor unknown",G(known:false)==null);
        C("governor invalid values",G(f:Double.NaN)==null&&G(l:101)==null);
        C("quartermaster supply concern",Q().Kind==SituationKind.QuartermasterSupply);
        C("quartermaster existing appointment only",Q().Options==(ResponseOptions.ExistingAppointment|ResponseOptions.Decline|ResponseOptions.Defer));
        C("quartermaster no shortage",Q(food:4)==null);
        C("quartermaster no consumption",Q(delta:0)==null);
        C("quartermaster unknown quote",Q(known:false)==null&&Q(delta:Double.NaN)==null);
        C("quartermaster no skill improvement",Q(skill:20)==null);
        C("quartermaster raw vacancy",Q(vacant:false)==null);
        C("quartermaster another role",Q(noRole:false)==null);
        C("quartermaster host authority",Q(guard:false)==null);
        C("family real adulthood",F().Kind==SituationKind.FamilyAdulthood);
        C("family actual reunion",F(change:"Reunited").Kind==SituationKind.FamilyReunion);
        C("family baseline not event",F(change:"Baseline")==null);
        C("family unknown event",F(witnessed:false)==null);
        C("family unrelated",F(kin:false)==null);
        C("family absent speaker",F(local:false)==null);
        C("family current state changed",F(state:false)==null);
        C("family duplicate acknowledgement",F(ack:true)==null);
        C("family stale or future",F(age:7)==null&&F(age:-1)==null);
        C("family third-party speaker excluded",F(speaker:"other")==null);
        C("family acknowledgement only",F().Options==(ResponseOptions.Acknowledge|ResponseOptions.Decline|ResponseOptions.Defer));
        C("petition actual affordable request",(P().Options&ResponseOptions.AcceptExistingPetition)!=0);
        C("petition unaffordable no accept",(P(gold:99).Options&ResponseOptions.AcceptExistingPetition)==0);
        C("petition exact cost required",P(cost:0)==null);
        C("petition no pending request",P(pending:false)==null);
        C("petition remote no action",P(local:false)==null);
        C("petition wrong speaker",P(speaker:false)==null);
        C("petition repeat forbidden",P(attempted:true)==null);
        C("petition non-financial not conflated",P(kind:"Recognition")==null);
        C("petition exact receipt",R());
        C("petition prepayment callback fails",!R(payerAfter:1000,recipientAfter:10));
        C("petition debit only fails",!R(recipientAfter:10));
        C("petition wrong request fails",!R(request:"different"));
        C("petition wrong recipient fails",!R(recipient:"other"));
        C("petition repeat fails",!R(once:false));
        C("petition missing pending evidence fails",!R(pending:false));
        C("petition unresolved fails",!R(resolved:false));
        Console.WriteLine("TOTAL_PASS="+count);
    }
}
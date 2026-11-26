using System;
using System.Linq;
using CU80.Lab;
public static class LedgerTests
{
    static int n;
    static void C(string name,bool pass){if(!pass)throw new Exception("FAIL "+name);n++;Console.WriteLine("PASS "+name);}
    static Snapshot S(string session="s",string player="p",string actor="a",string party="party",int wounded=17,bool assigned=false)
    {
        return new Snapshot(session,player,actor,"clan",party,"a:1;p:1;t:20","model",100,960,wounded,80,assigned?80:20,
            assigned?1:0,1,Authority.All,new[]{"","",assigned?actor:"","","",""},new[]{player,player,assigned?actor:player,player,player,player});
    }
    public static void Main()
    {
        var l=DurableLedger.Load("",true);string k=DurableLedger.SurgeonKey(S());
        C("legacy missing additive key",l.Valid&&l.Count==0);
        C("present empty key invalid",!DurableLedger.Load("",false).Valid);
        C("stable across session",k==DurableLedger.SurgeonKey(S(session:"reloaded")));
        C("changed wounds distinct",k!=DurableLedger.SurgeonKey(S(wounded:18)));
        C("different player distinct",k!=DurableLedger.SurgeonKey(S(player:"heir")));
        C("different speaker distinct",k!=DurableLedger.SurgeonKey(S(actor:"other")));
        C("different party distinct",k!=DurableLedger.SurgeonKey(S(party:"other")));
        C("begin one attempt",l.Begin(k,"p","a","party","Surgeon"));
        C("repeat blocked",!l.Begin(k,"p","a","party","Surgeon"));
        string k2=DurableLedger.SurgeonKey(S(wounded:18));
        C("uncertain player scope blocks new",!l.CanBegin(k2,"p"));
        C("another player separate",l.CanBegin(DurableLedger.SurgeonKey(S(player:"heir")),"heir"));
        string packed=l.Pack();var reload=DurableLedger.Load(packed,false);
        C("canonical roundtrip",reload.Valid&&reload.Pack()==packed);
        C("started survives reload",reload.State(k)==AttemptState.Started&&reload.HasUncertain("p"));
        C("vacancy cannot reconcile",!reload.ReconcileObservedSurgeon(S()));
        C("wrong actor cannot reconcile",!reload.ReconcileObservedSurgeon(S(actor:"other",assigned:true)));
        C("wrong party cannot reconcile",!reload.ReconcileObservedSurgeon(S(party:"other",assigned:true)));
        C("observed exact holder reconciles",reload.ReconcileObservedSurgeon(S(session:"new",assigned:true)));
        C("observed not confirmed",reload.State(k)==AttemptState.ObservedAssigned);
        C("observed entry still consumed",!reload.CanBegin(k,"p"));
        C("material change after observation",reload.CanBegin(k2,"p"));
        C("no repeated reconciliation",!reload.ReconcileObservedSurgeon(S(assigned:true)));
        C("finish only started",!reload.Finish(k,AttemptState.Confirmed));
        C("decline persists",reload.Begin(k2,"p","a","party","Surgeon")&&reload.Finish(k2,AttemptState.Declined)&&
            DurableLedger.Load(reload.Pack(),false).State(k2)==AttemptState.Declined);
        string pk=DurableLedger.PetitionKey("p","a","HouseholdAid",30,1,30);
        C("request key exact counter",pk!=null&&pk!=DurableLedger.PetitionKey("p","a","HouseholdAid",30,2,30));
        C("request key exact day",pk!=DurableLedger.PetitionKey("p","a","HouseholdAid",31,1,31));
        C("request key invalid day or quota",DurableLedger.PetitionKey("p","a","HouseholdAid",30,0,30)==null&&DurableLedger.PetitionKey("p","a","HouseholdAid",30,1,31)==null);
        C("request kind scope",DurableLedger.PetitionKey("p","a","Recognition",30,1,30)==null);
        C("request exact player",pk!=DurableLedger.PetitionKey("heir","a","HouseholdAid",30,1,30));
        C("request exact recipient",pk!=DurableLedger.PetitionKey("p","b","HouseholdAid",30,1,30));
        C("petition unresolved blocks",reload.Begin(pk,"p","a","request","HouseholdAid")&&reload.Finish(pk,AttemptState.Uncertain)&&reload.HasUncertain("p"));
        C("financial cannot reconcile with role",!reload.ReconcileObservedSurgeon(S(assigned:true))&&reload.HasUncertain("p"));
        C("financial unresolved persists",DurableLedger.Load(reload.Pack(),false).HasUncertain("p"));
        var broken=DurableLedger.Load("wrong",false);
        C("invalid payload preserved",!broken.Valid&&broken.Pack()=="wrong"&&!broken.CanBegin(k,"p"));
        C("duplicate row rejected",!DurableLedger.Load(packed+"\n"+packed.Split('\n')[1],false).Valid);
        C("invalid base64 rejected",!DurableLedger.Load(packed.Replace("|cA==|","|!|"),false).Valid);
        C("invalid status rejected",!DurableLedger.Load(packed.Substring(0,packed.Length-1)+"9",false).Valid);
        C("extra blank row rejected",!DurableLedger.Load(packed+"\n",false).Valid);
        l=new DurableLedger();
        bool ok=true;
        for(int i=0;i<DurableLedger.Capacity;i++)
        {string p="p"+i;string key=DurableLedger.SurgeonKey(S(player:p));ok&=l.Begin(key,p,"a","party","Surgeon")&&l.Finish(key,AttemptState.Deferred);}
        C("bounded 128 entries",ok&&l.Count==128);
        C("capacity refuses no eviction",!l.CanBegin(k,"p")&&l.Count==128);
        C("capacity reload",DurableLedger.Load(l.Pack(),false).Count==128);
        Console.WriteLine("TOTAL_PASS="+n);
    }
}
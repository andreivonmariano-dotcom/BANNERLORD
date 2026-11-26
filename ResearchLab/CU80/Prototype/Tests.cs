using System;
using CU80.Lab;
public static class Tests
{
    static int count;
    static void Check(string name,bool b){if(!b)throw new Exception("FAIL "+name);count++;Console.WriteLine("PASS "+name);}
    static Snapshot S(int wounded=17,int med=80,int effectiveMed=20,int roles=0,int roster=1,
        Authority flags=Authority.All,string player="p",string actor="a",string session="s",string party="party",
        string rosterKey="p:1;a:1;troop:20",long tick=100,long gold=960,string surgeon="",
        string effectiveSurgeon="p",string other="",string effectiveOther="p",string provider="native",bool unknown=false)
    {
        return new Snapshot(session,player,actor,"clan",party,rosterKey,provider,tick,gold,wounded,med,effectiveMed,roles,roster,
            flags,new[]{other,"",surgeon,"","",""},new[]{effectiveOther,"p",effectiveSurgeon,"p","p",unknown?null:"p"});
    }
    static Snapshot Assigned(int wounded=17,long tick=100,string rosterKey="p:1;a:1;troop:20",long gold=960,string other="",string effectiveOther="p")
    {return S(wounded:wounded,effectiveMed:80,roles:1,tick:tick,gold:gold,rosterKey:rosterKey,surgeon:"a",effectiveSurgeon:"a",other:other,effectiveOther:effectiveOther);}
    sealed class Fake:ISurgeonPort
    {
        public Snapshot Current,After;
        public int Calls,Reads;
        public bool ThrowRead,ThrowAssign;
        public Action During;
        public Snapshot Read(){Reads++;if(ThrowRead)throw new Exception();return Current;}
        public void Assign(string p,string a,string party)
        {if(p!="p"||a!="a"||party!="party")throw new Exception("wrong identity");Calls++;if(During!=null)During();Current=After;if(ThrowAssign)throw new Exception();}
    }
    static Fake F(Snapshot s=null,Snapshot after=null){return new Fake{Current=s??S(),After=after??Assigned()};}
    static bool NoOffer(Snapshot s){return new Encounter(F(s),"s").Present()==null;}
    public static void Main()
    {
        Check("no wounds",NoOffer(S(wounded:0)));
        Check("no skill improvement",NoOffer(S(med:20)));
        Check("unknown skill",NoOffer(S(med:-1)));
        Check("unknown slot",NoOffer(S(unknown:true)));
        Check("raw incumbent even if effective leader",NoOffer(S(surgeon:"absent")));
        Check("actor has other role",NoOffer(S(roles:1)));
        Check("actor raw other slot",NoOffer(S(other:"a")));
        Check("actor absent",NoOffer(S(roster:0)));
        Check("actor duplicate roster",NoOffer(S(roster:2)));
        Check("self speaker",NoOffer(S(actor:"p")));
        Check("wrong session",NoOffer(S(session:"other")));
        Check("no known provider",NoOffer(S(provider:"")));
        foreach(Authority flag in new[]{Authority.Known,Authority.Sandbox,Authority.PlayerLeadsClan,Authority.PlayerLeadsParty,Authority.AdultHousehold,Authority.ActorAliveActive,Authority.ActorLocal,Authority.RelationAllowed,Authority.NativeCapacity,Authority.SafeLandParty,Authority.ServiceAllowed,Authority.NoGovernorship})
            Check("missing "+flag,NoOffer(S(flags:Authority.All&~flag)));
        Fake f=F();Encounter e=new Encounter(f,"s");Offer o=e.Present();
        Check("meaningful offer facts",o!=null&&o.Wounded==17&&o.CandidateMedicine==80&&o.CurrentMedicine==20);
        Check("decline no action",e.Resolve(o,Choice.LeaveAsIs).Status==Outcome.Declined&&f.Calls==0);
        Check("decline replay blocked",e.Resolve(o,Choice.Assign).Status==Outcome.InvalidOffer&&f.Calls==0);
        Check("unchanged situation suppressed",e.Present()==null);
        f.Current=S(tick:101);Check("tick alone does not resurface",e.Present()==null);
        f.Current=S(wounded:18);Check("material change resurfaces",e.Present()!=null);
        f=F();e=new Encounter(f,"s");o=e.Present();Check("defer no action",e.Resolve(o,Choice.Defer).Status==Outcome.Deferred&&f.Calls==0);
        f=F();e=new Encounter(f,"s");o=e.Present();f.Current=S(actor:"different");
        Check("changed actor stale",e.Resolve(o,Choice.Assign).Status==Outcome.Stale&&f.Calls==0);
        f=F();e=new Encounter(f,"s");o=e.Present();f.Current=S(tick:101);
        Check("changed tick stale",e.Resolve(o,Choice.Assign).Status==Outcome.Stale&&f.Calls==0);
        f=F();e=new Encounter(f,"s");o=e.Present();f.Current=S(surgeon:"incumbent");
        Check("changed vacancy stale",e.Resolve(o,Choice.Assign).Status==Outcome.Stale&&f.Calls==0);
        f=F();e=new Encounter(f,"s");o=e.Present();f.Current=S(gold:959);
        Check("changed gold stale",e.Resolve(o,Choice.Assign).Status==Outcome.Stale&&f.Calls==0);
        f=F();e=new Encounter(f,"s");o=e.Present();f.ThrowRead=true;
        Check("pre-read error no mutation",e.Resolve(o,Choice.Assign).Status==Outcome.Stale&&f.Calls==0);
        f=F();e=new Encounter(f,"s");o=e.Present();var otherController=new Encounter(f,"s");
        Check("cross controller token invalid",otherController.Resolve(o,Choice.Assign).Status==Outcome.InvalidOffer&&f.Calls==0);
        f=F();e=new Encounter(f,"s");o=e.Present();Outcome reentrant=Outcome.NoOffer;
        f.During=()=>{reentrant=e.Resolve(o,Choice.Assign).Status;};
        Receipt r=e.Resolve(o,Choice.Assign);
        Check("one call and exact receipt",r.Confirmed&&f.Calls==1);
        Check("reentrant blocked",reentrant==Outcome.Locked);
        Check("replay blocked",e.Resolve(o,Choice.Assign).Status==Outcome.InvalidOffer&&f.Calls==1);
        Check("occupied after assignment no offer",e.Present()==null);
        Check("descriptive recovery",Encounter.ObservedWoundedDifference(r,Assigned(wounded:12,tick:110))==5);
        Check("wounds increase described",Encounter.ObservedWoundedDifference(r,Assigned(wounded:19,tick:110))==-2);
        Check("same tick not a later observation",Encounter.ObservedWoundedDifference(r,Assigned(wounded:12))==null);
        Check("changed roster invalidates",Encounter.ObservedWoundedDifference(r,Assigned(wounded:12,tick:110,rosterKey:"changed"))==null);
        Check("changed role invalidates",Encounter.ObservedWoundedDifference(r,S(wounded:12,tick:110))==null);
        f=F(after:S());e=new Encounter(f,"s");o=e.Present();
        Check("no actual mutation unconfirmed",e.Resolve(o,Choice.Assign).Status==Outcome.Unconfirmed&&f.Calls==1);
        Check("uncertain locked",e.Resolve(o,Choice.Assign).Status==Outcome.Locked&&e.Present()==null&&f.Calls==1);
        f=F(after:Assigned(other:"b"));e=new Encounter(f,"s");o=e.Present();
        Check("other raw role changed",e.Resolve(o,Choice.Assign).Status==Outcome.Unconfirmed&&f.Calls==1);
        f=F(after:Assigned(effectiveOther:"b"));e=new Encounter(f,"s");o=e.Present();
        Check("other effective role changed",e.Resolve(o,Choice.Assign).Status==Outcome.Unconfirmed&&f.Calls==1);
        f=F(after:S(roles:1,surgeon:"a",effectiveSurgeon:"p"));e=new Encounter(f,"s");o=e.Present();
        Check("raw only is insufficient",e.Resolve(o,Choice.Assign).Status==Outcome.Unconfirmed);
        f=F(after:Assigned(gold:959));e=new Encounter(f,"s");o=e.Present();
        Check("unexpected gold change unconfirmed",e.Resolve(o,Choice.Assign).Status==Outcome.Unconfirmed);
        f=F();f.ThrowAssign=true;e=new Encounter(f,"s");o=e.Present();
        Check("exception after side effect unconfirmed",e.Resolve(o,Choice.Assign).Status==Outcome.Unconfirmed&&f.Calls==1);
        Check("exception no retry",e.Present()==null&&e.Resolve(o,Choice.Assign).Status==Outcome.Locked&&f.Calls==1);
        f=F();e=new Encounter(f,"s");o=e.Present();
        Check("repeated presentation preserves same token",Object.ReferenceEquals(o,e.Present()));
        Check("repeated rendering still allows assignment",e.Resolve(o,Choice.Assign).Confirmed&&f.Calls==1);
        f=F();e=new Encounter(f,"s");o=e.Present();
        f.Current=S(actor:"other");Offer other=e.Present();
        Check("speaker switch invalidates previous token",other!=null&&e.Resolve(o,Choice.Assign).Status==Outcome.InvalidOffer&&f.Calls==0);
        e.Resolve(other,Choice.Defer);f.Current=S();
        Check("speaker switch does not revive same concern",e.Present()==null);
        f=F();e=new Encounter(f,"s");bool filled=true;
        for(int i=0;i<64;i++){f.Current=S(actor:"actor"+i);Offer x=e.Present();if(x==null){filled=false;break;}e.Resolve(x,Choice.Defer);}
        Check("64 distinct contexts supported",filled);
        f.Current=S(actor:"overflow");Check("bounded capacity refuses new context",e.Present()==null&&f.Calls==0);
        f.Current=S(actor:"actor0");Check("no eviction revives earlier concern",e.Present()==null);
        Console.WriteLine("TOTAL_PASS="+count);
    }
}
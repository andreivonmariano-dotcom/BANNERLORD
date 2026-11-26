using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaUnified.CU80
{
    [Flags] public enum Authority
    {
        None=0, Known=1, Sandbox=2, PlayerLeadsClan=4, PlayerLeadsParty=8,
        AdultHousehold=16, ActorAliveActive=32, ActorLocal=64, RelationAllowed=128,
        NativeCapacity=256, SafeLandParty=512, ServiceAllowed=1024, NoGovernorship=2048,
        All=4095
    }
    public enum Choice { Assign, LeaveAsIs, Defer }
    public enum Outcome { NoOffer, InvalidOffer, Declined, Deferred, Stale, Confirmed, Unconfirmed, Locked }
    // Empty string means known vacant. Null is unknown and is rejected.
    public sealed class Snapshot
    {
        public const int Surgeon=2; // local array layout, NOT TaleWorlds enum numeric values
        public readonly string Session, Player, Actor, Clan, Party, RosterKey, Provider;
        public readonly long Tick, HeroGold;
        public readonly int Wounded, ActorMedicine, EffectiveMedicine, ActorRoleCount, ActorRosterCount;
        public readonly Authority Flags;
        private readonly string[] raw, effective;
        public Snapshot(string session,string player,string actor,string clan,string party,string rosterKey,
            string provider,long tick,long gold,int wounded,int actorMedicine,int effectiveMedicine,
            int actorRoleCount,int actorRosterCount,Authority flags,string[] rawSlots,string[] effectiveSlots)
        {
            Session=session; Player=player; Actor=actor; Clan=clan; Party=party; RosterKey=rosterKey; Provider=provider;
            Tick=tick; HeroGold=gold; Wounded=wounded; ActorMedicine=actorMedicine; EffectiveMedicine=effectiveMedicine;
            ActorRoleCount=actorRoleCount; ActorRosterCount=actorRosterCount; Flags=flags;
            raw=rawSlots==null?null:(string[])rawSlots.Clone();
            effective=effectiveSlots==null?null:(string[])effectiveSlots.Clone();
        }
        public string Raw(int index) { return raw[index]; }
        public string Effective(int index) { return effective[index]; }
        private static bool Id(string s) { return !String.IsNullOrWhiteSpace(s); }
        private static bool Slots(string[] a) { return a!=null && a.Length==6 && a.All(x=>x!=null && (x.Length==0 || Id(x))); }
        public bool Complete
        {
            get { return Id(Session)&&Id(Player)&&Id(Actor)&&Player!=Actor&&Id(Clan)&&Id(Party)&&Id(RosterKey)&&Id(Provider)
                && Tick>=0&&HeroGold>=0&&Wounded>=0&&ActorMedicine>=0&&EffectiveMedicine>=0
                && ActorRoleCount>=0&&ActorRoleCount<=6&&ActorRosterCount>=0&&Slots(raw)&&Slots(effective)
                && Id(effective[Surgeon]) && (Flags&Authority.Known)!=0; }
        }
        public bool Eligible
        {
            get { return Complete && (Flags&Authority.All)==Authority.All && Wounded>0
                && ActorRosterCount==1 && ActorRoleCount==0 && !raw.Contains(Actor)
                && Raw(Surgeon)=="" && ActorMedicine>EffectiveMedicine; }
        }
        public bool SameIdentity(Snapshot b)
        {
            return b!=null && Session==b.Session && Player==b.Player && Actor==b.Actor && Clan==b.Clan
                && Party==b.Party && Provider==b.Provider;
        }
        public bool SameOfferState(Snapshot b)
        {
            return Complete && b!=null && b.Complete && SameIdentity(b) && Tick==b.Tick && RosterKey==b.RosterKey
                && HeroGold==b.HeroGold && Wounded==b.Wounded && ActorMedicine==b.ActorMedicine
                && EffectiveMedicine==b.EffectiveMedicine && ActorRoleCount==b.ActorRoleCount
                && ActorRosterCount==b.ActorRosterCount && Flags==b.Flags
                && raw.SequenceEqual(b.raw)&&effective.SequenceEqual(b.effective);
        }
        public bool ConfirmsAppointment(Snapshot b)
        {
            if(!Eligible || b==null || !b.Complete || !SameIdentity(b) || b.Tick!=Tick
                || RosterKey!=b.RosterKey || HeroGold!=b.HeroGold || Wounded!=b.Wounded
                || ActorMedicine!=b.ActorMedicine || b.EffectiveMedicine!=ActorMedicine
                || b.ActorRosterCount!=1 || b.ActorRoleCount!=1
                || b.Raw(Surgeon)!=Actor || b.Effective(Surgeon)!=Actor) return false;
            // Native capacity may cease to be available after assignment; other guards must still hold.
            Authority required=Authority.All & ~Authority.NativeCapacity;
            if((b.Flags&required)!=required) return false;
            for(int i=0;i<6;i++) if(i!=Surgeon && (Raw(i)!=b.Raw(i)||Effective(i)!=b.Effective(i))) return false;
            return true;
        }
    }
    // Adapter contract: live readback on the campaign thread; never pass a stale cached snapshot.
    // Assign delegates once to the existing CU native command. This isolated lab has only a fake adapter.
    public interface ISurgeonPort
    {
        Snapshot Read();
        void Assign(string expectedPlayer,string expectedActor,string expectedParty);
    }
    public sealed class Offer
    {
        internal readonly Snapshot Before;
        internal Offer(Snapshot before) { Before=before; }
        public int Wounded { get { return Before.Wounded; } }
        public int CandidateMedicine { get { return Before.ActorMedicine; } }
        public int CurrentMedicine { get { return Before.EffectiveMedicine; } }
    }
    public sealed class Receipt
    {
        public readonly Outcome Status;
        internal readonly Snapshot After;
        internal Receipt(Outcome status,Snapshot after) { Status=status; After=after; }
        public bool Confirmed { get { return Status==Outcome.Confirmed; } }
    }
    // One pending offer per controller, single-threaded; an uncertain native attempt locks the controller.
    // In-memory only; a future production adapter must invalidate on reload and reconcile native state.
    public sealed class Encounter
    {
        private readonly ISurgeonPort port;
        private readonly string session;
        private Offer pending;
        private bool locked;
        private const int MaxContexts=64;
        private readonly List<Snapshot> presented=new List<Snapshot>();
        public Encounter(ISurgeonPort port,string session)
        {
            if(port==null||String.IsNullOrWhiteSpace(session)) throw new ArgumentException();
            this.port=port;this.session=session;
        }
        public Offer Present()
        {
            if(locked) return null;
            Snapshot s;
            try { s=port.Read(); } catch { pending=null; return null; }
            if(s==null || s.Session!=session || !s.Eligible) { pending=null; return null; }
            // Repeated rendering is idempotent; a changed snapshot invalidates the prior token.
            if(pending!=null && pending.Before.SameOfferState(s)) return pending;
            pending=null;
            int index=presented.FindIndex(x=>x.SameIdentity(s));
            if(index>=0)
            {
                Snapshot old=presented[index];
                if(old.RosterKey==s.RosterKey && old.Wounded==s.Wounded
                    && old.ActorMedicine==s.ActorMedicine && old.EffectiveMedicine==s.EffectiveMedicine) return null;
                presented[index]=s;
            }
            else
            {
                // No silent eviction that would revive old encounters.
                if(presented.Count>=MaxContexts) return null;
                presented.Add(s);
            }
            pending=new Offer(s);
            return pending;
        }
        public Receipt Resolve(Offer offer,Choice choice)
        {
            if(locked) return new Receipt(Outcome.Locked,null);
            if(offer==null || !Object.ReferenceEquals(pending,offer)) return new Receipt(Outcome.InvalidOffer,null);
            pending=null; // consume before any possible native call, including reentrant calls
            if(choice==Choice.LeaveAsIs) return new Receipt(Outcome.Declined,null);
            if(choice==Choice.Defer) return new Receipt(Outcome.Deferred,null);
            if(choice!=Choice.Assign) return new Receipt(Outcome.InvalidOffer,null);
            Snapshot now;
            try { now=port.Read(); } catch { return new Receipt(Outcome.Stale,null); }
            if(now==null || !now.Eligible || !offer.Before.SameOfferState(now)) return new Receipt(Outcome.Stale,null);
            // lock across invocation and verification; no blind retry after exception/unknown partial effect.
            locked=true;
            try
            {
                port.Assign(now.Player,now.Actor,now.Party);
                Snapshot after=port.Read();
                if(now.ConfirmsAppointment(after)) { locked=false; return new Receipt(Outcome.Confirmed,after); }
                return new Receipt(Outcome.Unconfirmed,null);
            }
            catch { return new Receipt(Outcome.Unconfirmed,null); }
        }
        public static int? ObservedWoundedDifference(Receipt receipt,Snapshot later)
        {
            if(receipt==null || !receipt.Confirmed || receipt.After==null || later==null || !later.Complete) return null;
            Snapshot a=receipt.After;
            if(!a.SameIdentity(later)||later.Tick<=a.Tick||a.RosterKey!=later.RosterKey
                ||later.ActorRosterCount!=1||later.ActorRoleCount!=1||later.Raw(Snapshot.Surgeon)!=a.Actor
                ||later.Effective(Snapshot.Surgeon)!=a.Actor||later.ActorMedicine!=a.ActorMedicine
                ||later.EffectiveMedicine!=a.ActorMedicine) return null;
            for(int i=0;i<6;i++) if(a.Raw(i)!=later.Raw(i)||a.Effective(i)!=later.Effective(i)) return null;
            return a.Wounded-later.Wounded; // signed observed count, NOT attributable healing
        }
    }
}
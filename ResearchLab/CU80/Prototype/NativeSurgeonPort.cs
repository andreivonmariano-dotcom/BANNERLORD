using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace CU80.Lab
{
    // Host must be the existing SandboxOriginBehavior on the campaign thread.
    // Bind the four delegates to its existing scope, service authority, CanAssignHouseholdSpecialty
    // and AssignHouseholdSpecialty. Never bypass those methods or invoke the native setter here.
    public sealed class NativeSurgeonPort : ISurgeonPort
    {
        private readonly Campaign campaign;
        private readonly string session;
        private readonly int thread;
        private readonly Func<bool> hostScope,serviceAllowed,canAssign;
        private readonly Action existingAssign;
        private static readonly PartyRole[] Slots = { PartyRole.Quartermaster,PartyRole.Scout,PartyRole.Surgeon,PartyRole.Engineer,PartyRole.FirstMate,PartyRole.Navigator };
        public NativeSurgeonPort(string session,Func<bool> hostScope,Func<bool> serviceAllowed,Func<bool> canAssign,Action existingAssign)
        {
            if(String.IsNullOrWhiteSpace(session)||hostScope==null||serviceAllowed==null||canAssign==null||existingAssign==null||Campaign.Current==null)
                throw new ArgumentException("Missing campaign or host binding");
            this.session=session;this.hostScope=hostScope;this.serviceAllowed=serviceAllowed;this.canAssign=canAssign;
            this.existingAssign=existingAssign;campaign=Campaign.Current;thread=Thread.CurrentThread.ManagedThreadId;
        }
        private void RequireScope()
        {
            if(Thread.CurrentThread.ManagedThreadId!=thread||Campaign.Current!=campaign)throw new InvalidOperationException("Campaign scope changed");
        }
        private static string Id(Hero h) { return h==null?"":h.StringId; }
        private static bool Adult(Hero p,Hero a,Clan c)
        {
            return a!=p&&a.Clan==c&&a.IsAlive&&a.IsActive&&a.Age>=18f&&!a.IsPrisoner&&!a.IsTraveling&&!a.IsFugitive&&
                ((a==p.Spouse&&a.Spouse==p)||(p.Children!=null&&p.Children.Contains(a)&&(a.Father==p||a.Mother==p))||
                 (c.Companions!=null&&c.Companions.Contains(a)&&a.CompanionOf==c));
        }
        private static string Roster(MobileParty party)
        {
            var r=party.MemberRoster;
            if(r.Count<1||r.Count>4096)throw new InvalidOperationException("Roster unavailable or exceeds bound");
            var counts=new SortedDictionary<string,int>(StringComparer.Ordinal);
            for(int i=0;i<r.Count;i++)
            {
                var e=r.GetElementCopyAtIndex(i);
                if(e.Character==null||String.IsNullOrWhiteSpace(e.Character.StringId)||e.Number<=0||counts.ContainsKey(e.Character.StringId))
                    throw new InvalidOperationException("Ambiguous roster");
                counts.Add(e.Character.StringId,e.Number);
            }
            return String.Join(";",counts.Select(x=>Uri.EscapeDataString(x.Key)+":"+x.Value.ToString(CultureInfo.InvariantCulture)));
        }
        // Positive public campaign hours are monotonic in one loaded campaign; IEEE bits
        // preserve ordering and equality for finite nonnegative doubles, not native tick units.
        private static long TimeRevision()
        {
            double h=CampaignTime.Now.ToHours;
            if(Double.IsNaN(h)||Double.IsInfinity(h)||h<0)throw new InvalidOperationException("Unknown time");
            return BitConverter.DoubleToInt64Bits(h==0?0:h);
        }
        public Snapshot Read()
        {
            RequireScope();
            var p=Hero.MainHero;var a=Hero.OneToOneConversationHero;var c=Clan.PlayerClan;var party=MobileParty.MainParty;
            if(p==null||a==null||c==null||party==null||party.MemberRoster==null||a.CharacterObject==null||campaign.Models==null) return null;
            var m=campaign.Models;
            if(m.PartyHealingModel==null||m.ClanMemberPartyRoleModel==null) return null;
            string provider=m.PartyHealingModel.GetType().AssemblyQualifiedName+"|"+m.ClanMemberPartyRoleModel.GetType().AssemblyQualifiedName;
            var raw=Slots.Select(x=>Id(party.GetRoleHolder(x))).ToArray();
            var effective=Slots.Select(x=>Id(party.GetEffectiveRoleHolder(x))).ToArray();
            var surgeon=party.GetEffectiveRoleHolder(PartyRole.Surgeon);
            if(surgeon==null)return null;
            int actorCount=party.MemberRoster.GetTroopCount(a.CharacterObject);
            var roles=party.GetHeroPartyRoles(a);
            if(roles==null)return null;
            Authority flags=Authority.Known;
            if(hostScope())flags|=Authority.Sandbox;
            if(p.IsAlive&&p.IsActive&&!p.IsPrisoner&&c.Leader==p)flags|=Authority.PlayerLeadsClan;
            if(party.IsActive&&party.LeaderHero==p&&party.ActualClan==c)flags|=Authority.PlayerLeadsParty;
            if(Adult(p,a,c))flags|=Authority.AdultHousehold;
            if(a.IsAlive&&a.IsActive&&!a.IsPrisoner&&!a.IsTraveling&&!a.IsFugitive)flags|=Authority.ActorAliveActive;
            if(a.PartyBelongedTo==party&&actorCount==1)flags|=Authority.ActorLocal;
            if(a.GetRelation(p)>=0)flags|=Authority.RelationAllowed;
            if(party.CanAssignMoreRolesToHero(a))flags|=Authority.NativeCapacity;
            var seat=Settlement.CurrentSettlement;
            if(party.Army==null&&party.MapEvent==null&&!party.IsCurrentlyAtSea&&(seat==null||!seat.IsUnderSiege))flags|=Authority.SafeLandParty;
            if(serviceAllowed())flags|=Authority.ServiceAllowed;
            if(a.GovernorOf==null)flags|=Authority.NoGovernorship;
            return new Snapshot(session,p.StringId,a.StringId,c.StringId,party.StringId,Roster(party),provider,
                TimeRevision(),p.Gold,party.MemberRoster.TotalWounded,a.GetSkillValue(DefaultSkills.Medicine),
                surgeon.GetSkillValue(DefaultSkills.Medicine),roles.Count,actorCount,flags,raw,effective);
        }
        public void Assign(string expectedPlayer,string expectedActor,string expectedParty)
        {
            RequireScope();
            var s=Read();
            if(s==null||!s.Eligible||s.Player!=expectedPlayer||s.Actor!=expectedActor||s.Party!=expectedParty||!canAssign())
                throw new InvalidOperationException("Native eligibility changed");
            existingAssign(); // only the existing CU handler owns the actual native action
        }
    }
}
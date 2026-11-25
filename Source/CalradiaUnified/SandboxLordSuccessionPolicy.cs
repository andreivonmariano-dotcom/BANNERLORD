using System;
using System.Collections.Generic;
using System.Linq;

namespace CalradiaUnified
{
    /// <summary>
    /// A requested HOUSEHOLD inheritance order, not a native kingdom vote,
    /// clan-head appointment, regency or property-transfer action.
    /// Real Hero identities and both native parent links are mandatory.
    /// </summary>
    public static class SandboxLordSuccessionPolicy
    {
        public const string EldestChild="EldestChild";
        public const string MalePreference="MalePreference";
        public static readonly string[] Rules={
            EldestChild,MalePreference
        };
        public static bool IsRule(string value)
        {
            return value==EldestChild || value==MalePreference;
        }
        public sealed class Candidate
        {
            public string Id;
            public int Age,SourceOrder;
            public bool Female,Alive,InFoundingClan,NativeParentsVerified;
            public Candidate(string id,int age,bool female,
                int order,bool alive,bool sameClan,bool realParents)
            {
                Id=id??"";Age=age;Female=female;
                SourceOrder=order;Alive=alive;
                InFoundingClan=sameClan;
                NativeParentsVerified=realParents;
            }
            public bool Minor{get{return Age<18;}}
            public bool Verified{get{return
                !String.IsNullOrWhiteSpace(Id) && Age>=0 && Age<=120 &&
                SourceOrder>=0 && Alive && InFoundingClan &&
                NativeParentsVerified;}}
        }
        public static Candidate[] Order(string rule,
            IEnumerable<Candidate> candidates)
        {
            if(!IsRule(rule) || candidates==null)
                return new Candidate[0];
            // Sorting NEVER elects a clan leader or awards land.
            // Age is the observed native birth order; ties retain the
            // stable source sequence and Hero.StringId.
            return candidates.Where(c=>c!=null && c.Verified)
                .GroupBy(c=>c.Id,StringComparer.Ordinal)
                .Select(g=>g.OrderBy(c=>c.SourceOrder)
                    .First())
                .OrderBy(c=>rule==MalePreference && !c.Female?0:
                    rule==MalePreference?1:0)
                .ThenByDescending(c=>c.Age)
                .ThenBy(c=>c.SourceOrder)
                .ThenBy(c=>c.Id,StringComparer.Ordinal)
                .Take(SandboxOriginProfile.MaxChildren+12)
                .ToArray();
        }
        public const int BranchBudget=128, BranchDepth=5;
        public sealed class BranchMember
        {
            public Candidate Person;
            public int Depth;
            public string Branch;
        }
        // Read-only branch representation. A deceased or married-out
        // ancestor can connect living descendants; only verified living
        // same-clan members appear as possible household claimants.
        // Separate from the original direct-child inheritance record.
        public static BranchMember[] Branches(string rule,string founderId,
            IEnumerable<Candidate> children,IEnumerable<Candidate> siblings,
            Func<string,IEnumerable<Candidate>> descendants)
        {
            var result=new List<BranchMember>();
            if(!IsRule(rule) || String.IsNullOrEmpty(founderId) || descendants==null)
                return result.ToArray();
            var seen=new HashSet<string>(StringComparer.Ordinal){founderId};
            int remaining=BranchBudget;
            Action<IEnumerable<Candidate>,int,string> visit=null;
            visit=(nodes,depth,branch)=>
            {
                if(nodes==null || depth>BranchDepth || remaining<=0)return;
                var sample=nodes.Take(remaining).ToArray();
                remaining-=sample.Length;
                foreach(var person in sample.Where(x=>x!=null && x.NativeParentsVerified &&
                    !String.IsNullOrWhiteSpace(x.Id) && x.Age>=0 && x.Age<=120 && x.SourceOrder>=0)
                    .OrderBy(x=>rule==MalePreference && x.Female?1:0)
                    .ThenByDescending(x=>x.Age).ThenBy(x=>x.SourceOrder)
                    .ThenBy(x=>x.Id,StringComparer.Ordinal))
                {
                    if(!seen.Add(person.Id))continue;
                    if(person.Verified)result.Add(new BranchMember{Person=person,Depth=depth,Branch=branch});
                    if(depth<BranchDepth && remaining>0)visit(descendants(person.Id),depth+1,branch);
                }
            };
            visit(children,1,"Descendant");
            visit(siblings,1,"Collateral");
            return result.ToArray();
        }

        public static string Status(bool founderAlive,
            bool founderLeadsNativeClan,string nativeLeaderId,
            Candidate heir)
        {
            if(heir==null)return "NoVerifiedLivingChild";
            if(founderAlive)
                return founderLeadsNativeClan?
                    (heir.Minor?"MinorHouseholdClaimOnly":
                        "AdultHouseholdClaimOnly"):
                    "NativeLeadershipChangedDuringFounderLife";
            if(nativeLeaderId==heir.Id)
                return "NativeHeirLeadershipObserved";
            if(heir.Minor)return "MinorClaimNativeDecisionPending";
            return "NativeSuccessionNotFollowingHouseholdOrder";
        }
        public static bool CanSetNativeClanLeader(){return false;}
        public static bool CanTransferNativeFief(){return false;}
        public static bool HasNativeRegency(){return false;}
    }
}

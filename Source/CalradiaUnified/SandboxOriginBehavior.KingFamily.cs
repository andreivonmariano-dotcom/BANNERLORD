using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        /// <summary>
        /// Require the actual native spouse and each requested child for
        /// the new guided royal household, before appointing the minister
        /// or granting any other CU court appointment.
        /// </summary>
        private static string VerifyGuidedKingNativeFamily(
            SandboxOriginProfile profile,Hero monarch)
        {
            if(profile==null || !profile.KingGuidedStart)return "";
            var request=SandboxKingStartPolicy.ValidateRequiredRoyalFamily(
                profile.SpouseEnabled,profile.ChildCount);
            if(request.Length>0)return "royal_family:"+request;
            var spouse=monarch==null?null:monarch.Spouse;
            var clan=Clan.PlayerClan;
            bool married=monarch!=null && spouse!=null &&
                spouse.IsAlive && spouse.Spouse==monarch;
            bool sameClan=married && clan!=null &&
                monarch.Clan==clan && spouse.Clan==clan;
            var mother=monarch==null?null:
                (monarch.IsFemale?monarch:spouse);
            var father=monarch==null?null:
                (monarch.IsFemale?spouse:monarch);
            var nativeChildren=new List<Hero>();
            if(mother!=null && father!=null &&
                monarch.Children!=null && clan!=null)
                nativeChildren=monarch.Children.Where(child=>
                    child!=null && child.IsAlive &&
                    child.Clan==clan &&
                    child.Mother==mother &&
                    child.Father==father &&
                    child.Name!=null &&
                    mother.Children!=null &&
                    father.Children!=null &&
                    mother.Children.Contains(child) &&
                    father.Children.Contains(child)).ToList();
            int verified=0;
            foreach(var planned in profile.Children.Take(profile.ChildCount))
            {
                var child=nativeChildren.FirstOrDefault(h=>
                    h.Name.ToString()==planned.Name &&
                    h.IsFemale==(planned.Gender=="Female") &&
                    SandboxFamilyAgePolicy.MatchesRequestedAge(
                        h.Age,planned.Age));
                if(child==null)continue;
                verified++;
                nativeChildren.Remove(child);
            }
            string status=SandboxKingStartPolicy.ValidateObservedRoyalFamily(
                married,sameClan,profile.ChildCount,verified);
            DiagnosticJournal.Event("cu_king_native_dynasty_verification",
                "result="+(status.Length==0?"PASS":"FAIL")+
                "|reason="+status+
                "|spouse="+(spouse==null?"none":spouse.StringId)+
                "|reciprocal_marriage="+married+
                "|same_player_clan="+sameClan+
                "|requested_native_children="+profile.ChildCount+
                "|verified_distinct_parented_children="+verified+
                "|no_royal_family_fabrication=true");
            return status.Length==0?"":"royal_family:"+status;
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace CalradiaUnified
{
    public sealed partial class SandboxOriginBehavior
    {
        /// <summary>
        /// Check the actual Heroes after native marriage and child creation.
        /// A spouse label, child-count setting or partial receipt is not
        /// sufficient proof of a guided Lord's mandatory living household.
        /// This method does not change the native family.
        /// </summary>
        private static string VerifyGuidedLordNativeFamily(
            SandboxOriginProfile profile,Hero player)
        {
            if(profile==null || !profile.LordGuidedStart)
                return "";
            string request=SandboxLordStartPolicy.ValidateRequiredFamily(
                profile.SpouseEnabled,profile.ChildCount);
            if(request.Length>0)return request;
            var spouse=player==null?null:player.Spouse;
            var clan=Clan.PlayerClan;
            bool married=player!=null && spouse!=null &&
                spouse.IsAlive && spouse.Spouse==player;
            bool sameClan=married && clan!=null &&
                player.Clan==clan && spouse.Clan==clan;
            Hero mother=player==null?null:(player.IsFemale?player:spouse);
            Hero father=player==null?null:(player.IsFemale?spouse:player);
            var matchingChildren=new List<Hero>();
            if(player!=null && spouse!=null && clan!=null &&
                mother!=null && father!=null && player.Children!=null)
                matchingChildren=player.Children.Where(child=>
                    child!=null && child.IsAlive &&
                    child.Clan==clan &&
                    child.Mother==mother && child.Father==father &&
                    child.Name!=null &&
                    mother.Children!=null && father.Children!=null &&
                    mother.Children.Contains(child) &&
                    father.Children.Contains(child)).ToList();
            int matched=0;
            foreach(var desired in profile.Children.Take(profile.ChildCount))
            {
                var actual=matchingChildren.FirstOrDefault(child=>
                    child.Name.ToString()==desired.Name &&
                    child.IsFemale==(desired.Gender=="Female") &&
                    SandboxFamilyAgePolicy.MatchesRequestedAge(
                        child.Age,desired.Age));
                if(actual==null)continue;
                matched++;
                // Each native child can satisfy only one requested child.
                matchingChildren.Remove(actual);
            }
            string result=SandboxLordStartPolicy.ValidateObservedFamily(
                married,sameClan,profile.ChildCount,matched);
            // A noble-sounding surname is insufficient. Both native
            // parents must still exist, remain reciprocal spouses and
            // acknowledge the generated spouse as their real child.
            var bornClan=spouse==null?null:spouse.OriginClan;
            var nativeFather=spouse==null?null:spouse.Father;
            var nativeMother=spouse==null?null:spouse.Mother;
            bool distinctNobleBirth=bornClan!=null &&
                bornClan!=clan && !bornClan.IsEliminated &&
                !bornClan.IsMinorFaction && !bornClan.IsBanditFaction &&
                !bornClan.IsRebelClan && bornClan.Kingdom!=null &&
                bornClan.Kingdom.StringId==profile.StartingKingdomId;
            bool marriedNativeParents=nativeFather!=null &&
                nativeMother!=null && nativeFather.IsAlive &&
                nativeMother.IsAlive && nativeFather.IsLord &&
                nativeMother.IsLord &&
                nativeFather.Clan==bornClan &&
                nativeMother.Clan==bornClan &&
                nativeFather.Spouse==nativeMother &&
                nativeMother.Spouse==nativeFather;
            bool reciprocalChild=spouse!=null && nativeFather!=null &&
                nativeMother!=null &&
                nativeFather.Children!=null &&
                nativeMother.Children!=null &&
                nativeFather.Children.Contains(spouse) &&
                nativeMother.Children.Contains(spouse);
            bool matchesOrigin=bornClan!=null &&
                nativeFather!=null && nativeMother!=null &&
                nativeFather.Clan==bornClan &&
                nativeMother.Clan==bornClan;
            bool namedClan=profile.SpouseBackground!="NobleChosen" ||
                (bornClan!=null && String.Equals(bornClan.StringId,
                    profile.SpouseBirthClanId,
                    StringComparison.OrdinalIgnoreCase));
            string noble=SandboxLordStartPolicy.ValidateNativeNobleSpouse(
                distinctNobleBirth,marriedNativeParents,
                reciprocalChild,matchesOrigin,namedClan);
            if(result.Length==0)result=noble;
            DiagnosticJournal.Event("cu_lord_native_noble_inlaws",
                "result="+(noble.Length==0?"PASS":"FAIL")+
                "|reason="+noble+
                "|birth_clan="+(bornClan==null?"none":bornClan.StringId)+
                "|father="+(nativeFather==null?"none":
                    nativeFather.StringId)+
                "|mother="+(nativeMother==null?"none":
                    nativeMother.StringId)+
                "|native_married_parents="+marriedNativeParents+
                "|reciprocal_native_child="+reciprocalChild+
                "|no_existing_npc_rewritten=true");
            DiagnosticJournal.Event("cu_lord_native_family_verification",
                "result="+(result.Length==0?"PASS":"FAIL")+
                "|reason="+result+
                "|spouse_id="+(spouse==null?"none":spouse.StringId)+
                "|marriage_reciprocal="+married+
                "|same_native_clan="+sameClan+
                "|expected_children="+profile.ChildCount+
                "|verified_named_native_children="+matched+
                "|both_native_parent_links_checked=true"+
                "|no_fictional_lineage_used=true");
            return result.Length==0?"":"lord_family:"+result;
        }
    }
}

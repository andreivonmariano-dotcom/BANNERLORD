using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace CalradiaUnified
{
    /// <summary>
    /// Bind a volunteered younger-sibling origin to an EXISTING older noble.
    /// This is a real native parent/child genealogy on both Heroes, not
    /// a fictional allegiance label or a new clone of a famous ruler.
    /// The character remains a legitimate separate vassal clan. Creating
    /// native army orders is a different, opt-in game mechanic.
    /// </summary>
    public static class SandboxDynasticStart
    {
        private static int OlderSons(Hero elder,Hero player)
        {
            if(elder.Father==null || elder.Mother==null ||
                elder.Father.Children==null)return 0;
            return elder.Father.Children.Count(h=>h!=null &&
                h!=player && !h.IsFemale && h.IsAlive &&
                h.Age>player.Age && h.Mother==elder.Mother);
        }

        private static bool Eligible(
            SandboxOriginProfile profile,Hero player,Hero elder,
            bool requireNativeAffiliation)
        {
            if(elder==null || elder==player || !elder.IsAlive ||
                !elder.IsActive || !elder.IsLord ||
                elder.Clan==null || elder.Clan.Kingdom==null ||
                elder.Clan.Kingdom.StringId!=profile.StartingKingdomId ||
                (requireNativeAffiliation &&
                    (Clan.PlayerClan==null ||
                     (profile.Role=="Lord" &&
                        elder.Clan.Kingdom!=Clan.PlayerClan.Kingdom) ||
                     (profile.Role=="NobleCadet" &&
                        (Clan.PlayerClan.Kingdom!=null ||
                         Clan.PlayerClan.Settlements.Count>0)))))
                return false;
            bool royal=elder.Clan.Kingdom.RulingClan==elder.Clan;
            bool leader=elder.Clan.Leader==elder;
            bool sameParents=elder.Father!=null && elder.Mother!=null &&
                elder.Father!=elder.Mother &&
                elder.Father.Children!=null &&
                elder.Mother.Children!=null;
            bool canShare= (player.Father==null ||
                player.Father==elder.Father) &&
                (player.Mother==null ||
                player.Mother==elder.Mother);
            return canShare && SandboxNobleStartPolicy.CanBindElder(
                profile.NobleBirthPosition,profile.Role,
                player.IsFemale,elder.Age>player.Age,
                sameParents,elder.Culture==player.Culture,true,
                leader,royal,OlderSons(elder,player));
        }

        private static Hero FindElder(SandboxOriginProfile profile,
            Hero player,bool requireNativeAffiliation)
        {
            if(profile==null || player==null ||
                !SandboxNobleStartPolicy.RequiresNobleElder(
                    profile.NobleBirthPosition) ||
                !SandboxNobleStartPolicy.ValidForRole(
                    profile.NobleBirthPosition,profile.Role))
                return null;
            if(requireNativeAffiliation &&
                (Clan.PlayerClan==null ||
                 (profile.Role=="Lord" &&
                    (Clan.PlayerClan.Kingdom==null ||
                     Clan.PlayerClan.Kingdom.RulingClan==Clan.PlayerClan)) ||
                 (profile.Role=="NobleCadet" &&
                    (Clan.PlayerClan.Kingdom!=null ||
                     Clan.PlayerClan.Settlements.Count>0))))
                return null;
            var candidates=Hero.AllAliveHeroes.Where(h=>
                Eligible(profile,player,h,requireNativeAffiliation));
            if(!String.IsNullOrEmpty(profile.NobleElderHeroId))
                candidates=candidates.Where(h=>h.StringId==
                    profile.NobleElderHeroId);
            return candidates.OrderBy(h=>h.StringId,
                StringComparer.Ordinal).FirstOrDefault();
        }

        /// <summary>
        /// Pre-campaign House can name a projected native elder, but native
        /// Advanced Start has not yet granted player kingdom allegiance.
        /// This is a read-only preview, not a family mutation.
        /// </summary>
        public static bool HasNativeElder(SandboxOriginProfile profile,
            Hero player)
        {
            return profile!=null && player!=null &&
                FindElder(profile,player,true)!=null;
        }

        public static string PreviewElderName(SandboxOriginProfile profile,
            Hero player)
        {
            if(profile==null ||
                profile.NobleBirthPosition=="Independent")
                return "Independent origin; no elder selected";
            if(player==null || player.Culture==null)
                return "Native culture and age are not ready";
            if(SandboxDynasticSonStart.IsSonOrigin(
                profile.NobleBirthPosition))
                return SandboxDynasticSonStart.Preview(profile,player);
            var elder=FindElder(profile,player,false);
            if(elder==null)
                return "No eligible native elder found for this background";
            return elder.Name.ToString()+" of "+
                elder.Clan.Name.ToString()+" ("+
                elder.Clan.Kingdom.Name.ToString()+
                "; provisional until native start)";
        }

        public static string TryBind(SandboxOriginProfile profile,
            Hero player,out Hero chosen)
        {
            chosen=null;
            if(profile==null || player==null ||
                profile.NobleBirthPosition=="Independent")
                return "independent";
            // Second/third SON differs from younger BROTHER. The noble
            // lord or king is the father, with real older male offspring.
            if(SandboxDynasticSonStart.IsSonOrigin(
                profile.NobleBirthPosition))
                return SandboxDynasticSonStart.TryBind(
                    profile,player,out chosen);
            if(Clan.PlayerClan==null ||
                (profile.Role=="Lord" &&
                    (Clan.PlayerClan.Kingdom==null ||
                     Clan.PlayerClan.Kingdom.RulingClan==Clan.PlayerClan)) ||
                (profile.Role=="NobleCadet" &&
                    (Clan.PlayerClan.Kingdom!=null ||
                     Clan.PlayerClan.Settlements.Count>0)))
                return "refused:wrong_native_noble_station";
            if(!SandboxNobleStartPolicy.ValidForRole(
                profile.NobleBirthPosition,profile.Role))
                return "refused:role_must_be_lord";
            var elder=FindElder(profile,player,true);
            if(elder==null)
            {
                DiagnosticJournal.Event("cu_dynastic_native_kin_refused",
                    "requested="+profile.NobleBirthPosition+
                    "|elder_id="+profile.NobleElderHeroId+
                    "|reason=no_matching_older_clan_leader_with_two_parents"+
                    "|fake_titles_prevented=true");
                return "refused:no_eligible_older_noble";
            }
            try
            {
                if(player.Father==null)player.Father=elder.Father;
                if(player.Mother==null)player.Mother=elder.Mother;
                if(!elder.Father.Children.Contains(player))
                    elder.Father.Children.Add(player);
                if(!elder.Mother.Children.Contains(player))
                    elder.Mother.Children.Add(player);
                if(player.Father!=elder.Father ||
                   player.Mother!=elder.Mother ||
                   !elder.Father.Children.Contains(player) ||
                   !elder.Mother.Children.Contains(player))
                    throw new InvalidOperationException(
                        "Player not linked to the existing noble parents");
                // Native relation action, not a custom fictional counter.
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    player,elder,35,false);
                chosen=elder;
                DiagnosticJournal.Event("cu_dynastic_native_sibling_bound",
                    "elder="+elder.StringId+
                    "|elder_name="+elder.Name.ToString()+
                    "|elder_clan="+elder.Clan.StringId+
                    "|birth_position="+profile.NobleBirthPosition+
                    "|father="+elder.Father.StringId+
                    "|mother="+elder.Mother.StringId+
                    "|kingdom="+elder.Clan.Kingdom.StringId+
                    "|native_parentage=true"+
                    "|ruler="+(elder.Clan.Leader==elder &&
                    elder.Clan.Kingdom.RulingClan==elder.Clan));
                return "bound:"+elder.StringId;
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception(
                    "cu_dynastic_native_sibling_failed",e);
                return "failed:"+e.GetType().Name;
            }
        }
    }
}

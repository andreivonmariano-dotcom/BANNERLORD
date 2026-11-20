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
            SandboxOriginProfile profile,Hero player,Hero elder)
        {
            if(elder==null || elder==player || !elder.IsAlive ||
                !elder.IsActive || !elder.IsLord ||
                elder.Clan==null || elder.Clan.Kingdom==null ||
                Clan.PlayerClan==null ||
                elder.Clan.Kingdom!=Clan.PlayerClan.Kingdom ||
                elder.Clan.Kingdom.StringId!=profile.StartingKingdomId)
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

        public static string TryBind(SandboxOriginProfile profile,
            Hero player,out Hero chosen)
        {
            chosen=null;
            if(profile==null || player==null ||
                profile.NobleBirthPosition=="Independent")
                return "independent";
            if(Clan.PlayerClan==null ||
                Clan.PlayerClan.Kingdom==null ||
                Clan.PlayerClan.Kingdom.RulingClan==Clan.PlayerClan)
                return "refused:must_start_as_native_vassal";
            if(!SandboxNobleStartPolicy.ValidForRole(
                profile.NobleBirthPosition,profile.Role))
                return "refused:role_must_be_lord";
            var eligible=Hero.AllAliveHeroes.Where(
                    h=>Eligible(profile,player,h));
            if(!String.IsNullOrEmpty(profile.NobleElderHeroId))
                eligible=eligible.Where(h=>h.StringId==
                    profile.NobleElderHeroId);
            var elder=eligible.OrderBy(h=>h.StringId,
                StringComparer.Ordinal).FirstOrDefault();
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

using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace CalradiaUnified
{
    /// <summary>
    /// A native political commission: the player clan retains the game's
    /// legitimate vassal/mercenary affiliation, and a real clan leader is
    /// recorded as a personal commanding lord. This is not troop enlistment
    /// or automated army orders.
    /// </summary>
    public static class SandboxServicePatron
    {
        private static Hero Select(SandboxOriginProfile p,Hero player,
            bool requireExistingKingdom)
        {
            if(p==null || player==null || player.Culture==null ||
                !SandboxServicePolicy.CanSelectPatron(
                    p.Role,p.StartingKingdomId))
                return null;
            var kingdom=Kingdom.All.FirstOrDefault(k=>
                k!=null && !k.IsEliminated &&
                k.StringId==p.StartingKingdomId);
            if(kingdom==null)return null;
            if(requireExistingKingdom &&
                (Clan.PlayerClan==null ||
                 ((p.KnightGuidedStart && p.Role=="Knight") ||
                   SandboxCustomLifeRolePolicy.IsLandless(p.Role)?
                    (Clan.PlayerClan.Kingdom!=null ||
                     Clan.PlayerClan.Settlements.Count>0):
                    Clan.PlayerClan.Kingdom!=kingdom)))
                return null;
            var candidates=Hero.AllAliveHeroes.Where(h=>
                h!=null && h!=player && h.IsAlive &&
                h.IsActive && h.IsLord &&
                h.Clan!=null && h.Clan!=Clan.PlayerClan &&
                h.Clan.Kingdom==kingdom &&
                h.Clan.Leader==h &&
                !h.Clan.IsEliminated && !h.Clan.IsBanditFaction &&
                !h.Clan.IsRebelClan && !h.Clan.IsMinorFaction);
            if(!String.IsNullOrEmpty(p.ServicePatronHeroId))
                candidates=candidates.Where(h=>
                    h.StringId==p.ServicePatronHeroId);
            // Prefer a genuine active field commander to a lord without
            // a party, then an ordinary lord before the ruling king.
            return candidates.OrderBy(h=>
                    h.PartyBelongedTo==null?1:0)
                .ThenBy(h=>h.Clan==kingdom.RulingClan?1:0)
                .ThenBy(h=>h.Culture==player.Culture?0:1)
                .ThenBy(h=>h.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
        }

        public static bool HasNativePatron(SandboxOriginProfile p,
            Hero player)
        {
            if(p!=null && p.SoldierGuidedStart)
            {
                var native=SandboxSaSBridge.Read();
                return native.Available && native.Enlisted &&
                    native.LordIsNativeLord &&
                    native.NativeHostPartyLedByLord &&
                    SandboxSoldierEnlistmentPolicy.ValidateEnlisted(
                        native.Available,native.Enlisted,
                        p.StartingKingdomId,p.ServicePatronHeroId,
                        native.LordId,native.LordKingdomId,
                        native.RankId,native.NativeHostPartyLedByLord,
                        Clan.PlayerClan!=null &&
                            Clan.PlayerClan.IsUnderMercenaryService,
                        Clan.PlayerClan!=null &&
                            Clan.PlayerClan.Settlements.Count>0)=="";
            }
            return Select(p,player,true)!=null;
        }

        public static string Preview(SandboxOriginProfile p,Hero player)
        {
            if(p==null || !SandboxServicePolicy.NeedsPatron(p.Role))
                return "No personal lord required for this start";
            if(p.SoldierGuidedStart)
                return String.IsNullOrEmpty(p.ServicePatronHeroId)?
                    "Actual SaS commanding lord: verified at enlistment":
                    "Must match native SaS lord ID: "+
                        p.ServicePatronHeroId;
            var lord=Select(p,player,false);
            if(lord==null)
                return "No eligible native commanding lord found";
            return lord.Name.ToString()+" of "+
                lord.Clan.Name.ToString()+
                " (provisional personal superior)";
        }

        public static string TryAssign(SandboxOriginProfile p,Hero player,
            out Hero patron)
        {
            patron=null;
            if(p==null || player==null ||
                !SandboxServicePolicy.NeedsPatron(p.Role))
                return "not_requested";
            if(p.SoldierGuidedStart)
            {
                var native=SandboxSaSBridge.Read();
                string status=SandboxSoldierEnlistmentPolicy.ValidateEnlisted(
                    native.Available,native.Enlisted,p.StartingKingdomId,
                    p.ServicePatronHeroId,native.LordId,
                    native.LordKingdomId,native.RankId,
                    native.NativeHostPartyLedByLord,
                    Clan.PlayerClan!=null &&
                        Clan.PlayerClan.IsUnderMercenaryService,
                    Clan.PlayerClan!=null &&
                        Clan.PlayerClan.Settlements.Count>0);
                if(status.Length>0)
                    return "refused:"+status;
                patron=Hero.AllAliveHeroes.FirstOrDefault(h=>
                    h!=null && h.StringId==native.LordId &&
                    h.IsAlive && h.IsActive && h.IsLord &&
                    h.Clan!=null && h.Clan.Kingdom!=null &&
                    h.Clan.Kingdom.StringId==p.StartingKingdomId);
                if(patron==null)
                    return "refused:actual_lord_not_resolved";
                DiagnosticJournal.Event("cu_soldier_real_lord_bound",
                    "result=PASS|lord_id="+patron.StringId+
                    "|lord_name="+patron.Name.ToString()+
                    "|clan="+patron.Clan.StringId+
                    "|kingdom="+patron.Clan.Kingdom.StringId+
                    "|real_enlisted_rank="+native.RankId+
                    "|real_daily_wage="+native.Wage+
                    "|host_party_leader_verified=true"+
                    "|no_fake_relation_or_command_granted=true");
                return "enlisted:"+patron.StringId;
            }
            var lord=Select(p,player,true);
            if(lord==null)
            {
                DiagnosticJournal.Event("cu_service_patron_refused",
                    "role="+p.Role+"|kingdom="+p.StartingKingdomId+
                    "|requested_id="+p.ServicePatronHeroId+
                    "|reason=no_native_clan_leader");
                return "refused:no_eligible_commanding_lord";
            }
            try
            {
                // Native relation action creates real personal standing.
                // The existing official kingdom contract is not altered.
                int before=player.GetRelation(lord);
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    player,lord,20,false);
                patron=lord;
                DiagnosticJournal.Event("cu_service_patron_bound",
                    "role="+p.Role+
                    "|lord_id="+lord.StringId+
                    "|lord_name="+lord.Name.ToString()+
                    "|clan="+lord.Clan.StringId+
                    "|kingdom="+lord.Clan.Kingdom.StringId+
                    "|relation_before="+before+
                    "|relation_after="+player.GetRelation(lord)+
                    "|native_contract="+(p.KnightGuidedStart?
                        "none_personal_patron_only":"retained")+
                    "|enlisted_troop=false|automatic_orders=false");
                return "bound:"+lord.StringId;
            }
            catch(Exception ex)
            {
                DiagnosticJournal.Exception("cu_service_patron_failed",ex);
                return "failed:"+ex.GetType().Name;
            }
        }
    }
}

using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;

namespace CalradiaUnified
{
    /// <summary>
    /// Read-only gate after native phase 8 and before any custom grants.
    /// A refusal leaves the native campaign intact and reports a failed
    /// selected start. It cannot roll back native phase 8 mutations.
    /// </summary>
    internal static class SandboxStartingLifePreflight
    {
        internal static string Check(SandboxOriginProfile p,Hero player)
        {
            if(p==null || player==null)return "missing_start_context";
            // Native Advanced Start has already run at phase 8. Check its
            // actual political and fief result BEFORE changing any CU
            // kinship, relation, property, officer or starter resource.
            var campaign=Campaign.Current;
            var clan=Clan.PlayerClan;
            if(campaign==null || campaign.AdvancedStartData==null ||
                clan==null)return "native_start:missing_native_context";
            var kingdom=clan.Kingdom;
            string actualType=campaign.AdvancedStartData.GetStartType();
            string expectedType=SandboxNativeStartOptions
                .StartTypeForProfile(p);
            int fiefs=clan.Settlements==null?0:clan.Settlements.Count;
            bool ruling=kingdom!=null && kingdom.RulingClan==clan;
            // Collect requested versus native-observed values even if the
            // gate fails. Older session reports omitted the native type,
            // preventing diagnosis of default/changed-option differences.
            DiagnosticJournal.Event("cu_native_start_preflight_snapshot",
                "role="+p.Role+
                "|expected_type="+(expectedType??"<null>")+
                "|actual_type="+(String.IsNullOrEmpty(actualType)?
                    "<empty>":actualType)+
                "|native_type_matches="+
                    SandboxRoleConsistencyPolicy.NativeStartTypeMatches(
                        expectedType,actualType,kingdom!=null,ruling,fiefs)+
                "|kingdom="+(kingdom==null?"none":kingdom.StringId)+
                "|fiefs="+fiefs+
                "|ruling="+ruling);
            var nativeErrors=SandboxStartContract.Validate(
                expectedType,
                actualType,
                (p.SoldierGuidedStart || p.KnightGuidedStart)?
                    "":p.StartingKingdomId,
                kingdom==null?"":kingdom.StringId,
                ruling,fiefs,
                false,false,0,0);
            if(nativeErrors.Length>0)
                return "native_start:"+nativeErrors[0];
            string stationError=p.KnightGuidedStart?
                SandboxKnightStartPolicy.ValidateNativeSwornHouse(
                    actualType,kingdom!=null,fiefs,
                    clan.IsUnderMercenaryService,clan.Leader==player):
                p.SoldierGuidedStart?
                SandboxSoldierEnlistmentPolicy.ValidateIndependentStart(
                    actualType,kingdom!=null,fiefs!=0,
                    clan.IsUnderMercenaryService):
                SandboxRoleConsistencyPolicy.CheckNativeStation(
                    p.Role,campaign.AdvancedStartData.GetStartType(),
                    kingdom!=null,kingdom!=null &&
                    kingdom.RulingClan==clan,
                    clan.Settlements==null?0:clan.Settlements.Count);
            if(stationError.Length>0)
                return "native_station:"+stationError;
            if(p.SoldierGuidedStart)
            {
                // SaS owns the real soldier role and the commander. A
                // kingdom or clan affiliation does NOT prove enlistment.
                var native=SandboxSaSBridge.Read();
                string service=SandboxSoldierEnlistmentPolicy
                    .ValidateEnlisted(native.Available,native.Enlisted,
                        p.StartingKingdomId,p.ServicePatronHeroId,
                        native.LordId,native.LordKingdomId,
                        native.RankId,
                        native.LordIsNativeLord &&
                            native.NativeHostPartyLedByLord,
                        clan.IsUnderMercenaryService,fiefs>0);
                string rank=SandboxSoldierEnlistmentPolicy
                    .ValidateRequestedEntryRank(
                        p.SoldierEntryRank,native.RankId);
                if(service.Length==0 && rank.Length>0)
                    service=rank;
                // RankId is the stable SaS public proof. Integer Rank
                // indexing is not yet verified for installed v1.5.7,
                // so never reject a genuine rank on a 0/1-base guess.
                DiagnosticJournal.Event("cu_soldier_real_enlistment_preflight",
                    "result="+(service.Length==0?"PASS":"FAIL")+
                    "|reason="+service+
                    "|requested_rank="+p.SoldierEntryRank+
                    "|actual_rank="+native.RankId+
                    "|lord_id="+native.LordId+
                    "|lord_kingdom="+native.LordKingdomId+
                    "|host_party_led_by_lord="+
                        native.NativeHostPartyLedByLord+
                    "|native_wage="+native.Wage+
                    "|native_progression_owned_by_sas=true");
                if(service.Length>0)return "soldier:"+service;
            }
            // The native mercenary START ID alone is insufficient:
            // verify genuine mercenary-clan service, own commander,
            // correct employer and no landed lord status.
            if(p.MercenaryGuidedStart)
            {
                string contract=SandboxMercenaryStartPolicy.Validate(
                    p.Role,p.StartingKingdomId,
                    kingdom==null?"":kingdom.StringId,
                    clan.IsUnderMercenaryService,
                    clan.Leader==player,
                    MobileParty.MainParty!=null &&
                    MobileParty.MainParty.LeaderHero==player,
                    ruling,fiefs);
                DiagnosticJournal.Event("cu_mercenary_native_preflight",
                    "result="+(contract.Length==0?"PASS":"FAIL")+
                    "|reason="+contract+
                    "|native_type="+actualType+
                    "|contract_kingdom="+
                        (kingdom==null?"none":kingdom.StringId)+
                    "|mercenary_service="+
                        clan.IsUnderMercenaryService+
                    "|player_leads_clan="+(clan.Leader==player)+
                    "|player_leads_party="+(
                        MobileParty.MainParty!=null &&
                        MobileParty.MainParty.LeaderHero==player)+
                    "|fiefs="+fiefs+
                    "|native_salary_not_invented=true");
                if(contract.Length>0)
                    return "mercenary:"+contract;
            }
            if(SandboxCustomLifeRolePolicy.IsLandless(p.Role) &&
                (Clan.PlayerClan==null ||
                 !SandboxCustomLifeRolePolicy.NativeLandlessContract(
                    Clan.PlayerClan.Kingdom!=null,
                    Clan.PlayerClan.Settlements.Count)))
                return "landless:unexpected_fief_or_kingdom_contract";
            var ages=SandboxHouseholdLineagePolicy.ValidateHousePlan(
                p,(int)player.Age);
            if(ages.Length>0)return "invalid_house_birth_order:"+ages;
            if(SandboxNobleStartPolicy.IsSonOrigin(p.NobleBirthPosition))
            {
                var reason=SandboxDynasticSonStart.Preflight(p,player);
                if(reason.Length>0)return "noble_son:"+reason;
            }
            else if(SandboxNobleStartPolicy.RequiresNobleElder(
                p.NobleBirthPosition) &&
                !SandboxDynasticStart.HasNativeElder(p,player))
                return "noble_sibling:no_eligible_native_elder";
            if(SandboxServicePolicy.NeedsPatron(p.Role) &&
                !SandboxServicePatron.HasNativePatron(p,player))
                return "service:no_eligible_native_patron";
            if(p.SpouseEnabled && !player.CanMarry())
                return "marriage:native_player_not_eligible";
            return "";
        }
    }
}

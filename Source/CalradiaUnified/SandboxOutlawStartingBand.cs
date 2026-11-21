using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Phase-9 new-game grant for one SMALL outlaw band.
    /// Uses genuine installed Bandit-occupation troops, native party limits,
    /// actual native wages, native denars and one real food item. Does not
    /// exempt anybody from payroll, stop desertion or rewrite bandit clans.
    /// </summary>
    public static class SandboxOutlawStartingBand
    {
        public static string TryGrant(SandboxOriginProfile profile,Hero player)
        {
            if(profile==null || !profile.OutlawGuidedStart ||
                profile.Role!="Outlaw")return "not_requested";
            var party=MobileParty.MainParty;
            if(player==null || Campaign.Current==null || party==null ||
                party.MemberRoster==null || party.ItemRoster==null ||
                party.Party==null || Clan.PlayerClan==null ||
                player.Culture==null)return "refused:native_party_missing";

            // Do not turn standard Vlandian recruits into a fake bandit
            // class. Discover a real native low-tier Bandit unit in the
            // installed game's registered CharacterObject catalogue.
            string preferred=SandboxOutlawBandPolicy.PreferredBanditCulture(
                player.Culture.StringId);
            var native=CharacterObject.All.Where(c=>c!=null &&
                !c.IsHero && c.Occupation==Occupation.Bandit &&
                c.Culture!=null && c.Tier>=1 && c.Tier<=2 &&
                c.TroopWage>=0 &&
                SandboxOutlawBandPolicy.CulturalBanditFactions.Contains(
                    c.Culture.StringId,StringComparer.Ordinal))
                .OrderBy(c=>c.Culture.StringId==preferred?0:
                    c.Culture.StringId=="looters"?1:2)
                .ThenBy(c=>c.Tier)
                .ThenBy(c=>c.TroopWage)
                .ThenBy(c=>c.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
            if(native==null)return "refused:native_bandit_troop_missing";

            var grain=MBObjectManager.Instance.GetObject<ItemObject>("grain");
            if(grain==null)return "refused:native_grain_missing";
            int nativeRegulars=0,nativeBandits=0;
            for(int i=0;i<party.MemberRoster.Count;i++)
            {
                var unit=party.MemberRoster.GetCharacterAtIndex(i);
                int count=party.MemberRoster.GetElementNumber(i);
                if(unit==null || count<0)
                    return "refused:invalid_native_troop_roster";
                if(unit.IsHero)continue;
                nativeRegulars+=count;
                if(unit.Occupation==Occupation.Bandit)
                    nativeBandits+=count;
            }
            int add=SandboxOutlawBandPolicy.MissingBandits(nativeBandits);
            if(add<0 || nativeRegulars+add>
                SandboxOutlawBandPolicy.MaximumInitialParty)
                return "refused:oversized_native_band";
            int limit=party.Party.PartySizeLimit;
            if(limit<=0 || party.MemberRoster.TotalManCount+add>limit)
                return "refused:native_party_size_limit";
            int nativeWage=(int)Math.Ceiling(
                Campaign.Current.Models.PartyWageModel.GetTotalWage(
                    party).ResultNumber);
            int troopWage=Campaign.Current.Models.PartyWageModel
                .GetCharacterWage(native);
            int expectedWage=nativeWage+add*Math.Max(0,troopWage);
            string preflight=SandboxOutlawBandPolicy.ValidateBeforeGrant(
                nativeRegulars,nativeBandits,limit,expectedWage);
            if(preflight.Length>0)return "refused:"+preflight;

            int grainBefore=party.ItemRoster.GetItemNumber(grain);
            int banditsBefore=party.MemberRoster.GetTroopCount(native);
            int goldBefore=player.Gold;
            int addedFood=0;
            try
            {
                if(add>0)
                    party.MemberRoster.AddToCounts(native,add);
                if(party.MemberRoster.GetTroopCount(native)!=
                    banditsBefore+add)
                    throw new InvalidOperationException(
                        "Native bandit roster did not accept the full grant");

                int actualWage=(int)Math.Ceiling(
                    Campaign.Current.Models.PartyWageModel.GetTotalWage(
                        party).ResultNumber);
                int minGold=SandboxOutlawBandPolicy.RequiredStartingGold(
                    actualWage);
                if(minGold<0)
                    throw new InvalidOperationException(
                        "Actual native party wage outside small-band budget");

                int foodMissing=Math.Max(0,
                    SandboxOutlawBandPolicy.StartingGrain-grainBefore);
                if(foodMissing>0)
                    party.ItemRoster.AddToCounts(grain,foodMissing);
                addedFood=foodMissing;
                if(party.ItemRoster.GetItemNumber(grain)<
                    SandboxOutlawBandPolicy.StartingGrain)
                    throw new InvalidOperationException(
                        "Native starting grain not verified");
                int topUp=Math.Max(0,minGold-player.Gold);
                if(topUp>0)
                    GiveGoldAction.ApplyBetweenCharacters(
                        null,player,topUp,false);
                if(player.Gold<minGold ||
                    party.MemberRoster.TotalManCount>limit)
                    throw new InvalidOperationException(
                        "Starting payroll or party size not verified");

                DiagnosticJournal.Event("cu_outlaw_band_wage_buffer",
                    "result=PASS|native_bandit_id="+native.StringId+
                    "|native_bandit_culture="+native.Culture.StringId+
                    "|native_bandits_before="+nativeBandits+
                    "|bandits_added="+add+
                    "|bandits_after="+(nativeBandits+add)+
                    "|native_regulars_before="+nativeRegulars+
                    "|native_regulars_after="+(nativeRegulars+add)+
                    "|daily_native_party_wage="+actualWage+
                    "|wage_runway_days="+
                        SandboxOutlawBandPolicy.PayrollDays+
                    "|minimum_gold="+minGold+
                    "|actual_gold="+player.Gold+
                    "|gold_topped_up="+(player.Gold-goldBefore)+
                    "|native_grain="+party.ItemRoster.GetItemNumber(grain)+
                    "|grain_added="+addedFood+
                    "|party_size="+party.MemberRoster.TotalManCount+
                    "|native_party_limit="+limit+
                    "|normal_wages_and_desertion_preserved=true"+
                    "|one_shot_phase9=true");
                return "bandits="+(nativeBandits+add)+
                    "|daily_wage="+actualWage+
                    "|payroll_days="+
                        SandboxOutlawBandPolicy.PayrollDays+
                    "|grain="+party.ItemRoster.GetItemNumber(grain)+
                    "|native_verified=true";
            }
            catch(Exception error)
            {
                // The native roster and gold actions are not one atomic
                // engine transaction. Never claim success after a partial
                // action or silently delete a registered native soldier.
                DiagnosticJournal.Exception(
                    "cu_outlaw_band_wage_buffer_failed",error);
                DiagnosticJournal.Event(
                    "cu_outlaw_band_partial_reconciliation",
                    "native_bandit_id="+native.StringId+
                    "|bandits_before="+banditsBefore+
                    "|bandits_now="+
                        party.MemberRoster.GetTroopCount(native)+
                    "|grain_before="+grainBefore+
                    "|grain_now="+party.ItemRoster.GetItemNumber(grain)+
                    "|gold_before="+goldBefore+
                    "|gold_now="+player.Gold+
                    "|native_mutations_not_replayed=true");
                return "partial:outlaw_band="+error.GetType().Name;
            }
        }
    }
}

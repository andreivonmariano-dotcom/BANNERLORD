using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace CalradiaUnified
{
    /// <summary>
    /// Real royal governor of a REAL player-owned vacant fortification.
    /// Never dispossesses an incumbent or confers a new settlement.
    /// RoyalMinister is a clan companion in the settlement, not a
    /// fictional officer traveling in the main party.
    /// </summary>
    internal static class SandboxKingNativeMinistry
    {
        /// <summary>
        /// Only existing native settlement ownership is authoritative.
        /// Bannerlord has no verified public pinned "capital" setter.
        /// Read the kingdom's native initial homeland when available;
        /// otherwise designate a REAL player-owned town/castle as the
        /// CU royal court seat, never as an engine capital mutation.
        /// The designation is persisted by the court behavior.
        /// </summary>
        internal static Settlement VerifiedRoyalCourtSeat()
        {
            var clan=Clan.PlayerClan;
            var kingdom=clan==null?null:clan.Kingdom;
            if(clan==null || kingdom==null ||
                kingdom.RulingClan!=clan || clan.Settlements==null)
                return null;
            var owned=clan.Settlements.Where(s=>s!=null &&
                s.OwnerClan==clan &&
                s.IsFortification && s.Town!=null &&
                !s.IsUnderSiege).ToArray();
            // The native capital name varies across supported versions.
            // Reflection is read-only and never assumes that a fictional
            // property exists. A found initial home is not silently
            // renamed in the kingdom model.
            Settlement nativeHome=null;
            foreach(string propertyName in new[]{
                "InitialHomeSettlement","InitialHomeLand"})
            {
                try
                {
                    var prop=kingdom.GetType().GetProperty(propertyName);
                    if(prop!=null && prop.CanRead)
                    {
                        nativeHome=prop.GetValue(kingdom,null) as Settlement;
                        if(nativeHome!=null)break;
                    }
                }
                catch(Exception e)
                {
                    DiagnosticJournal.Exception(
                        "cu_royal_home_reflection_unavailable",e);
                }
            }
            if(nativeHome!=null && owned.Contains(nativeHome))
                return nativeHome;
            // No game-native capital designation is asserted for fallback.
            return owned.OrderBy(s=>s.IsTown?0:1)
                .ThenBy(s=>s.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
        }

        internal static Settlement VacantOwnedFief()
        {
            var preferred=VerifiedRoyalCourtSeat();
            if(preferred!=null && preferred.Town.Governor==null)
                return preferred;
            // Prefer the native homeland when vacant, otherwise use
            // another REAL unbesieged player-owned vacant seat. Do not
            // fire a governor to make a ceremonial capital.
            var clan=Clan.PlayerClan;
            return clan==null || clan.Settlements==null?
                null:clan.Settlements.Where(s=>s!=null &&
                    s.OwnerClan==clan && s.IsFortification &&
                    s.Town!=null && !s.IsUnderSiege &&
                    s.Town.Governor==null)
                    .OrderBy(s=>s.IsTown?0:1)
                    .ThenBy(s=>s.StringId,StringComparer.Ordinal)
                    .FirstOrDefault();
        }

        internal static string TryAppoint(Hero minister,
            out string settlementId)
        {
            settlementId="";
            if(minister==null || minister.CharacterObject==null ||
               !minister.IsActive || !minister.IsAlive ||
               minister.Clan!=Clan.PlayerClan ||
               minister.CompanionOf!=Clan.PlayerClan)
                return "native_minister_hero_not_registered";
            var seat=VacantOwnedFief();
            if(seat==null)return "native_royal_governor_seat_missing";
            if(minister.GovernorOf!=null)
                return "native_minister_already_governs";
            if(seat.OwnerClan!=Clan.PlayerClan ||
               seat.Town==null || seat.Town.Governor!=null)
                return "native_governorship_no_longer_vacant";
            // This is the engine action, never a Town.Governor setter.
            // The minister is not a MemberRoster troop.
            ChangeGovernorAction.Apply(seat.Town,minister);
            bool accepted=seat.Town.Governor==minister &&
                minister.GovernorOf==seat.Town &&
                minister.Clan==Clan.PlayerClan &&
                minister.CompanionOf==Clan.PlayerClan;
            DiagnosticJournal.Event(
                "cu_king_royal_minister_native_governor",
                "result="+(accepted?"PASS":"FAIL")+
                "|minister="+minister.StringId+
                "|settlement="+seat.StringId+
                "|town_governor="+(seat.Town.Governor==null?
                    "none":seat.Town.Governor.StringId)+
                "|hero_governor_of="+(minister.GovernorOf==null?
                    "none":minister.GovernorOf.StringId)+
                "|native_change_governor_action=true"+
                "|previous_governor=none"+
                "|no_estate_owner_change=true"+
                "|court_seat_designation_only=true"+
                "|native_capital_property_unchanged=true");
            if(!accepted)return "native_governor_action_unconfirmed";
            settlementId=seat.StringId;
            return "native_royal_minister_governs:"+settlementId;
        }

        internal static bool VerifiedGovernor(Hero minister)
        {
            if(minister==null || minister.GovernorOf==null ||
               minister.Clan!=Clan.PlayerClan ||
               minister.CompanionOf!=Clan.PlayerClan)
                return false;
            var town=minister.GovernorOf;
            return town.Governor==minister &&
                town.Settlement!=null &&
                town.Settlement.OwnerClan==Clan.PlayerClan &&
                town.Settlement.IsFortification;
        }
    }
}

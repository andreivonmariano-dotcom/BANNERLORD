using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    /// <summary>
    /// Resolve the ancestry of a NEW generated spouse. No existing hero becomes
    /// the player's spouse. For a noble, the newborn family relationship is
    /// applied to a newly generated adult before the game's MarriageAction.
    /// </summary>
    internal static class SandboxGeneratedSpouseOrigin
    {
        internal static string PreflightGuidedLord(
            SandboxOriginProfile profile,Hero player)
        {
            if(profile==null || !profile.LordGuidedStart)return "";
            var request=SandboxLordStartPolicy.ValidateNobleSpouseRequest(
                profile.SpouseBackground,profile.SpouseBirthClanId);
            if(request.Length>0)return request;
            Clan birthClan;
            Hero father,mother;
            string refused;
            // Only scans the already existing kingdom/clans. The
            // temporary random choice does not create or edit a native
            // Hero, marriage, clan or estate.
            if(!TryResolve(profile,player,out birthClan,
                out father,out mother,out refused))
                return refused.Length==0?
                    "native_noble_parents_unavailable":refused;
            return father!=null && mother!=null &&
                birthClan!=null?"":"native_noble_parentage_unavailable";
        }
        internal static bool TryResolve(SandboxOriginProfile profile, Hero player,
            out Clan birthClan, out Hero father, out Hero mother,
            out string refusal)
        {
            birthClan = Clan.PlayerClan;
            father = null;
            mother = null;
            refusal = "";
            if (profile.SpouseBackground == "Commoner")
                return true;

            Kingdom kingdom = player.Clan == null ? null : player.Clan.Kingdom;
            if (kingdom == null || kingdom.IsEliminated)
            {
                refusal = "noble_birth_requires_existing_starting_kingdom";
                return false;
            }

            // An outsider spouse must never be assigned to the native
            // household that just became the player's own birth family.
            // Exclude both current and origin clans of living parents.
            var closeFamilyClans=new HashSet<Clan>();
            foreach(var parent in new[]{player.Father,player.Mother})
            {
                if(parent==null)continue;
                if(parent.Clan!=null)closeFamilyClans.Add(parent.Clan);
                if(parent.OriginClan!=null)
                    closeFamilyClans.Add(parent.OriginClan);
            }
            var candidates = kingdom.Clans.Where(clan =>
                clan != null && clan != Clan.PlayerClan &&
                !closeFamilyClans.Contains(clan) &&
                !clan.IsEliminated && !clan.IsBanditFaction &&
                !clan.IsRebelClan && !clan.IsMinorFaction)
                .OrderBy(clan => clan.StringId, StringComparer.Ordinal)
                .ToArray();

            if (profile.SpouseBackground == "NobleChosen")
            {
                candidates = candidates.Where(clan =>
                    String.Equals(clan.StringId, profile.SpouseBirthClanId,
                        StringComparison.OrdinalIgnoreCase)).ToArray();
                if (candidates.Length == 0)
                {
                    refusal = closeFamilyClans.Any(c=>
                        c.StringId==profile.SpouseBirthClanId)?
                        "chosen_clan_is_player_close_family":
                        "chosen_noble_clan_not_in_starting_kingdom";
                    return false;
                }
            }

            // Select a pair from the real birth clan, not a random husband
            // or unrelated Hero. The two native parents must be married.
            var families = candidates.SelectMany(clan =>
                clan.Heroes.Where(h =>
                    h != null && h.Clan == clan && h.IsAlive &&
                    h.IsLord && !h.IsFemale &&
                    h!=player.Father && h!=player.Mother &&
                    h.Spouse != null && h.Spouse!=player.Father &&
                    h.Spouse!=player.Mother &&
                    h.Spouse.IsAlive && h.Spouse.IsFemale &&
                    h.Spouse.Clan == clan &&
                    SandboxFamilyOriginPolicy.CanBeNobleParents(
                        (int)h.Age,(int)h.Spouse.Age,profile.SpouseAge,
                        h.Spouse.Spouse == h,true))
                .Select(h => new { Clan = clan, Father = h, Mother = h.Spouse }))
                .OrderBy(f => f.Clan.StringId, StringComparer.Ordinal)
                .ThenBy(f => f.Father.StringId, StringComparer.Ordinal)
                .ToArray();
            if (families.Length == 0)
            {
                // The guided Lord has an explicit living noble in-law
                // contract. A named noble clan without both real native
                // married parents is not enough to establish that lineage.
                if(profile.LordGuidedStart)
                {
                    refusal="guided_lord_requires_native_married_noble_parents";
                    return false;
                }
                // No invented parents. NobleRandom still permits an actual
                // native noble birth clan when no married couple is old
                // enough to parent the requested adult spouse (e.g. age 48).
                // A missing ancestor must not cancel a genuine family.
                if (candidates.Length == 0)
                {
                    refusal = "no_valid_noble_clan_in_selected_kingdom";
                    return false;
                }
                birthClan = profile.SpouseBackground == "NobleRandom" ?
                    candidates[MBRandom.RandomInt(candidates.Length)] :
                    candidates[0];
                DiagnosticJournal.Event("cu_family_noble_origin_selected",
                    "mode=" + profile.SpouseBackground +
                    "|birth_clan=" + birthClan.StringId +
                    "|father=unknown|mother=unknown" +
                    "|kingdom=" + kingdom.StringId +
                    "|ancestry=unrecorded" +
                    "|reason=no_valid_parent_couple_in_selected_kingdom_or_clan");
                return true;
            }

            var selected = profile.SpouseBackground == "NobleRandom" ?
                families[MBRandom.RandomInt(families.Length)] : families[0];
            birthClan = selected.Clan;
            father = selected.Father;
            mother = selected.Mother;
            DiagnosticJournal.Event("cu_family_noble_origin_selected",
                "mode=" + profile.SpouseBackground +
                "|birth_clan=" + birthClan.StringId +
                "|father=" + father.StringId +
                "|mother=" + mother.StringId +
                "|kingdom=" + kingdom.StringId);
            return true;
        }
    }
}

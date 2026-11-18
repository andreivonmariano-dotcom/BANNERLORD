using System;
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

            var candidates = kingdom.Clans.Where(clan =>
                clan != null && clan != Clan.PlayerClan &&
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
                    refusal = "chosen_noble_clan_not_in_starting_kingdom";
                    return false;
                }
            }

            // Select a pair from the real birth clan, not a random husband
            // or unrelated Hero. The two native parents must be married.
            var families = candidates.SelectMany(clan =>
                clan.Heroes.Where(h =>
                    h != null && h.Clan == clan && h.IsAlive &&
                    h.IsLord && !h.IsFemale && h.Spouse != null &&
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
                refusal = "no_valid_parent_couple_in_selected_kingdom_or_clan";
                return false;
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

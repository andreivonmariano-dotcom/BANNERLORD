using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Explicitly opted-in creation of real, clan-owned relatives.
    /// Uses native HeroCreator and MarriageAction, not fabricated roster units.
    /// Must pass new-game and save/reload acceptance before production use.
    /// </summary>
    public static class SandboxFamilyCreator
    {
        public static string TryCreate(SandboxOriginProfile profile, Hero player)
        {
            if (profile == null || !profile.RealFamilyEnabled || player == null ||
                Clan.PlayerClan == null) return "not_enabled";
            string reason;
            if (!profile.IsValid((int)player.Age, out reason))
                return "invalid_family:" + reason;
            string guard = SandboxFamilyPolicy.Guard(profile.SpouseEnabled,
                player.Spouse != null, profile.ChildCount,
                player.Children != null && player.Children.Count > 0,
                profile.SiblingCount, player.Father != null ||
                    player.Mother != null, profile.SpouseGender == "Female" ==
                    player.IsFemale, true);
            if (guard.Length > 0) return guard;
            for (int i = 0; i < profile.ChildCount; i++)
                if (!SandboxFamilyPolicy.ValidChildAge(
                    profile.Children[i].Age, (int)player.Age,
                    profile.SpouseEnabled, profile.SpouseAge))
                    return "invalid_child_age:" + (i+1);
            // Do not start creating unrelated heroes if the correct source
            // templates cannot be resolved up front.
            var spouseTemplate = profile.SpouseEnabled ?
                FindTemplate(player,profile.SpouseCulture,
                    profile.SpouseGender == "Female",profile.SpouseFaceSeed) : null;
            if (profile.SpouseEnabled && spouseTemplate == null)
                return "spouse_template_missing";

            var templates = new List<CharacterObject>();
            for (int i = 0; i < profile.ChildCount; i++)
            {
                var child = profile.Children[i];
                var source = FindTemplate(player,child.Culture,
                    child.Gender == "Female",child.FaceSeed);
                if (source == null) return "child_template_missing:" + (i+1);
                templates.Add(source);
            }
            // Siblings need the player's actual parents. If Bannerlord has no
            // registered parent heroes, creating strangers and calling them
            // siblings is not acceptable.
            // The policy gate refuses fake siblings with no real native parent.
            var siblingTemplates = new List<CharacterObject>();
            for (int i = 0; i < profile.SiblingCount; i++)
            {
                var sibling = profile.Siblings[i];
                var source = FindTemplate(player,sibling.Culture,
                    sibling.Gender == "Female",sibling.FaceSeed);
                if (source == null)
                    return "sibling_template_missing:" + (i+1);
                siblingTemplates.Add(source);
            }

            Hero spouseHero = null;
            int madeChildren=0;
            int madeSiblings=0;
            try
            {
                if (profile.SpouseEnabled)
                {
                    spouseHero = Create(spouseTemplate,profile.SpouseName,
                        profile.SpouseAge);
                    // Apply the game's marriage action. This synchronizes clan,
                    // spouse references and the native marriage event.
                    MarriageAction.Apply(player,spouseHero,false);
                    if (player.Spouse != spouseHero ||
                        spouseHero.Spouse != player)
                        throw new InvalidOperationException(
                            "Native marriage did not establish both spouse references");
                    DiagnosticJournal.Event("cu_family_spouse_created",
                        "id=" + spouseHero.StringId);
                }
                for (int i = 0; i < profile.ChildCount; i++)
                {
                    var childProfile=profile.Children[i];
                    Hero child=Create(templates[i],childProfile.Name,
                        childProfile.Age);
                    Hero mother = player.IsFemale ? player : spouseHero;
                    Hero father = player.IsFemale ? spouseHero : player;
                    child.Mother=mother;
                    child.Father=father;
                    if (mother != null && !mother.Children.Contains(child))
                        mother.Children.Add(child);
                    if (father != null && !father.Children.Contains(child))
                        father.Children.Add(child);
                    madeChildren++;
                    DiagnosticJournal.Event("cu_family_child_created",
                        "id=" + child.StringId + "|age=" + childProfile.Age);
                }
                for (int i = 0; i < profile.SiblingCount; i++)
                {
                    var siblingProfile=profile.Siblings[i];
                    Hero sibling=Create(siblingTemplates[i],siblingProfile.Name,
                        siblingProfile.Age);
                    sibling.Father=player.Father;
                    sibling.Mother=player.Mother;
                    if (sibling.Father != null &&
                        !sibling.Father.Children.Contains(sibling))
                        sibling.Father.Children.Add(sibling);
                    if (sibling.Mother != null &&
                        !sibling.Mother.Children.Contains(sibling))
                        sibling.Mother.Children.Add(sibling);
                    madeSiblings++;
                    DiagnosticJournal.Event("cu_family_sibling_created",
                        "id=" + sibling.StringId);
                }
            }
            catch (Exception e)
            {
                // One-shot application is committed before creation.
                // Never retry a partial family on another game tick.
                DiagnosticJournal.Exception("cu_family_partial_creation",e);
                return "partial:" + madeChildren + " children," +
                    madeSiblings + " siblings";
            }

            return "spouse=" + (spouseHero == null ? "none" : spouseHero.StringId) +
                "|children=" + madeChildren + "|siblings=" + madeSiblings;
        }

        private static Hero Create(CharacterObject source,string name,int age)
        {
            var result=HeroCreator.CreateSpecialHero(source,null,
                Clan.PlayerClan,null,age);
            if (result == null)
                throw new InvalidOperationException("HeroCreator returned null");
            result.SetName(new TextObject(name),new TextObject(name));
            result.SetNewOccupation(Occupation.Lord);
            return result;
        }

        private static CharacterObject FindTemplate(Hero player,string cultureId,
            bool female,string requestedTemplateId)
        {
            CultureObject culture=player.Culture;
            if (!String.IsNullOrEmpty(cultureId))
            {
                var resolved=MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
                if (resolved == null) return null;
                culture=resolved;
            }
            if (!String.IsNullOrEmpty(requestedTemplateId))
            {
                var explicitTemplate=
                    MBObjectManager.Instance.GetObject<CharacterObject>(requestedTemplateId);
                if (explicitTemplate == null || !explicitTemplate.IsTemplate ||
                    explicitTemplate.IsFemale != female ||
                    explicitTemplate.Culture != culture) return null;
                return explicitTemplate;
            }
            return CharacterObject.All.FirstOrDefault(c =>
                c != null && c.IsTemplate && c.IsFemale == female &&
                c.Culture == culture &&
                (c.Occupation == Occupation.Lord ||
                 c.Occupation == Occupation.Wanderer));
        }
    }
}

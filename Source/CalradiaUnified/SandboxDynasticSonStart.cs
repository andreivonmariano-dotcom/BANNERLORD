using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace CalradiaUnified
{
    /// <summary>
    /// A second/third son has a real native LORD as father, not merely an
    /// older sibling who happens to be lord. The elder sons share both parents.
    /// Native lords are never cloned or given invented titles.
    /// </summary>
    public static class SandboxDynasticSonStart
    {
        public static bool IsSonOrigin(string position)
        {
            return position!=null &&
                (position.StartsWith("SecondSon",StringComparison.Ordinal) ||
                 position.StartsWith("ThirdSon",StringComparison.Ordinal));
        }

        private static Hero[] OlderSons(Hero father,Hero mother,Hero player)
        {
            return father.Children.Where(h=>h!=null && h.IsAlive &&
                h!=player && !h.IsFemale && h.Mother==mother &&
                h.Age>player.Age)
                .OrderByDescending(h=>h.Age)
                .ThenBy(h=>h.StringId,StringComparer.Ordinal)
                .ToArray();
        }

        private static bool Eligible(SandboxOriginProfile p,Hero player,
            Hero father,bool strictKingdom)
        {
            if(father==null || father==player || !father.IsAlive ||
                !father.IsActive || !father.IsLord || father.IsFemale ||
                father.Clan==null || father.Clan.Leader!=father ||
                father.Clan.Kingdom==null || player.Culture==null ||
                father.Culture!=player.Culture ||
                father.Clan.Kingdom.StringId!=p.StartingKingdomId ||
                (father.Clan.Kingdom.RulingClan==father.Clan)!=
                    SandboxNobleStartPolicy.RoyalElder(p.NobleBirthPosition))
                return false;
            if(strictKingdom && (Clan.PlayerClan==null ||
                father.Clan.Kingdom!=Clan.PlayerClan.Kingdom))
                return false;
            var mother=father.Spouse;
            if(mother==null || !mother.IsAlive || !mother.IsFemale ||
                mother.Spouse!=father || mother.Culture!=player.Culture ||
                mother==player || mother==father)
                return false;
            if((player.Father!=null && player.Father!=father) ||
               (player.Mother!=null && player.Mother!=mother) ||
               player.IsFemale)return false;
            int needed=SandboxNobleStartPolicy.RequiredOlderSons(
                p.NobleBirthPosition);
            if(needed<=0)return false;
            var existing=OlderSons(father,mother,player);
            int planned=SandboxHouseholdLineagePolicy.PlannedOlderBrothers(
                p,(int)player.Age);
            int missing=SandboxHouseholdLineagePolicy.AdditionalOlderBrothers(
                needed,existing.Length,planned);
            if(missing<0)return false;
            // Siblings requested in House count as actual sons in the
            // same genealogy. Both parents must be old enough for ALL
            // selected siblings, not only automatically created brothers.
            int oldest=SandboxHouseholdLineagePolicy.OldestPlannedSiblingAge(
                p,(int)player.Age);
            if(existing.Length>0)
                oldest=Math.Max(oldest,(int)existing[0].Age);
            if(missing>0)
                oldest=Math.Max(oldest,(int)player.Age+missing*2);
            return father.Age>=oldest+16 && mother.Age>=oldest+16;
        }

        private static Hero FindFather(SandboxOriginProfile p,Hero player,
            bool strictKingdom)
        {
            if(p==null || player==null || !IsSonOrigin(p.NobleBirthPosition) ||
                p.Role!="Lord")return null;
            if(strictKingdom &&
                (Clan.PlayerClan==null || Clan.PlayerClan.Kingdom==null ||
                 Clan.PlayerClan.Kingdom.RulingClan==Clan.PlayerClan))
                return null;
            var q=Hero.AllAliveHeroes.Where(h=>
                Eligible(p,player,h,strictKingdom));
            if(!String.IsNullOrEmpty(p.NobleElderHeroId))
                q=q.Where(h=>h.StringId==p.NobleElderHeroId);
            // Prefer a complete existing family before making brothers.
            return q.OrderByDescending(h=>
                    OlderSons(h,h.Spouse,player).Length)
                .ThenBy(h=>h.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
        }

        public static string Preview(SandboxOriginProfile p,Hero player)
        {
            if(player==null || player.Culture==null)
                return "Native age and culture are not yet available";
            var father=FindFather(p,player,false);
            if(father==null)
                return "No eligible native father found for this requested birth";
            int existing=OlderSons(father,father.Spouse,player).Length;
            int requested=SandboxNobleStartPolicy.RequiredOlderSons(
                p.NobleBirthPosition);
            return "Father: "+father.Name.ToString()+
                " | Mother: "+father.Spouse.Name.ToString()+
                " | Older sons: "+existing+
                (existing<requested?" (missing brothers generated at start)":"")+
                " | provisional native-family preview";
        }

        private static CharacterObject BrotherSource(Hero player)
        {
            var native=CharacterObject.All.Where(c=>c!=null &&
                c.Culture==player.Culture && !c.IsFemale &&
                !c.IsBasicTroop && c.IsTemplate &&
                c.Occupation==Occupation.Lord)
                .OrderBy(c=>c.StringId,StringComparer.Ordinal)
                .FirstOrDefault();
            if(native!=null)return native;
            return Hero.AllAliveHeroes.Where(h=>h!=player && h!=null &&
                h.IsActive && h.IsLord && !h.IsFemale &&
                h.Culture==player.Culture && h.CharacterObject!=null)
                .OrderBy(h=>h.StringId,StringComparer.Ordinal)
                .Select(h=>h.CharacterObject).FirstOrDefault();
        }

        public static string TryBind(SandboxOriginProfile p,Hero player,
            out Hero fatherChosen)
        {
            fatherChosen=null;
            var father=FindFather(p,player,true);
            if(father==null)
            {
                DiagnosticJournal.Event("cu_dynastic_son_refused",
                    "birth="+p.NobleBirthPosition+
                    "|reason=no_valid_native_lord_father_and_mother");
                return "refused:no_eligible_native_father";
            }
            var mother=father.Spouse;
            var before=OlderSons(father,mother,player);
            int needed=SandboxNobleStartPolicy.RequiredOlderSons(
                p.NobleBirthPosition);
            int planned=SandboxHouseholdLineagePolicy.PlannedOlderBrothers(
                p,(int)player.Age);
            int missing=SandboxHouseholdLineagePolicy.AdditionalOlderBrothers(
                needed,before.Length,planned);
            if(missing<0)
                return "refused:older_sons_exceed_selected_birth_rank";
            var source=missing>0?BrotherSource(player):null;
            if(missing>0 && source==null)
                return "refused:native_older_brother_template_missing";
            var generated=new List<Hero>();
            var reserved=new List<string>{player.Name.ToString(),
                father.Name.ToString(),mother.Name.ToString()};
            reserved.AddRange(before.Select(h=>h.Name.ToString()));
            reserved.AddRange(p.Siblings.Take(p.SiblingCount)
                .Select(h=>h.Name));
            try
            {
                // Create and verify missing older sons before binding the
                // player. Native family links, not custom relationship text.
                for(int i=0;i<missing;i++)
                {
                    int age=(int)player.Age+2+i*2;
                    string name=SandboxCulturalNames.Suggest(
                        player.Culture.StringId,false,6+i,reserved);
                    var brother=HeroCreator.CreateSpecialHero(
                        source,null,father.Clan,null,age);
                    if(brother==null)
                        throw new InvalidOperationException(
                            "Native brother HeroCreator returned null");
                    generated.Add(brother);
                    brother.SetName(new TextObject(name),
                        new TextObject(name));
                    brother.Culture=player.Culture;
                    brother.SetBirthDay(CampaignTime.YearsFromNow(-age));
                    brother.SetNewOccupation(Occupation.Lord);
                    if(!brother.IsActive)
                        brother.ChangeState(Hero.CharacterStates.Active);
                    brother.Father=father;
                    brother.Mother=mother;
                    if(!father.Children.Contains(brother))
                        father.Children.Add(brother);
                    if(!mother.Children.Contains(brother))
                        mother.Children.Add(brother);
                    if(brother.Father!=father || brother.Mother!=mother ||
                        brother.Age<=player.Age ||
                        brother.Clan!=father.Clan)
                        throw new InvalidOperationException(
                            "Generated firstborn genealogy failed");
                    reserved.Add(name);
                    DiagnosticJournal.Event("cu_dynastic_older_son_created",
                        "father="+father.StringId+"|mother="+mother.StringId+
                        "|brother="+brother.StringId+"|name="+name+
                        "|age="+brother.Age+"|clan="+brother.Clan.StringId);
                }
                var older=OlderSons(father,mother,player);
                if(!SandboxHouseholdLineagePolicy.ExactFinalRank(
                    needed,older.Length+planned))
                    throw new InvalidOperationException(
                        "Existing and planned older brothers contradict birth rank");
                player.Father=father;
                player.Mother=mother;
                if(!father.Children.Contains(player))
                    father.Children.Add(player);
                if(!mother.Children.Contains(player))
                    mother.Children.Add(player);
                if(player.Father!=father || player.Mother!=mother ||
                    !father.Children.Contains(player) ||
                    !mother.Children.Contains(player))
                    throw new InvalidOperationException(
                        "Player native father/mother links failed");
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                    player,father,30,false);
                fatherChosen=father;
                DiagnosticJournal.Event("cu_dynastic_native_son_bound",
                    "birth="+p.NobleBirthPosition+
                    "|father="+father.StringId+
                    "|father_name="+father.Name.ToString()+
                    "|mother="+mother.StringId+
                    "|mother_name="+mother.Name.ToString()+
                    "|older_brothers="+String.Join(",",older.Select(
                        h=>h.StringId))+
                    "|generated_older_brothers="+generated.Count+
                    "|pending_house_older_brothers="+planned+
                    "|projected_total_older_brothers="+
                        (older.Length+planned)+
                    "|real_native_lineage=true");
                return "bound:father="+father.StringId+
                    "|verified_older_brothers="+older.Length+
                    "|house_older_brothers_pending="+planned;
            }
            catch(Exception ex)
            {
                DiagnosticJournal.Exception("cu_dynastic_son_failed",ex);
                // Safe cleanup only for newly created heroes. Existing
                // native lords and relatives are never destroyed.
                foreach(var brother in generated)
                {
                    try
                    {
                        if(brother.Father!=null)
                            brother.Father.Children.Remove(brother);
                        if(brother.Mother!=null)
                            brother.Mother.Children.Remove(brother);
                        brother.Father=null;
                        brother.Mother=null;
                        DisableHeroAction.Apply(brother);
                        brother.Clan=null;
                    }
                    catch(Exception cleanup)
                    {
                        DiagnosticJournal.Exception(
                            "cu_dynastic_son_cleanup_failed",cleanup);
                    }
                }
                return "failed:"+ex.GetType().Name;
            }
        }
    }
}

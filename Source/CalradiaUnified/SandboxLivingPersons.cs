using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace CalradiaUnified
{
    /// <summary>
    /// Snapshots verified living Bannerlord Heroes into a portable
    /// biographical record. It never creates NPCs, forges history,
    /// changes parties or rewrites relationships.
    /// </summary>
    internal static class SandboxLivingPersons
    {
        private static void Add(List<Hero> list,Hero hero)
        {
            if(hero!=null && !list.Contains(hero))list.Add(hero);
        }
        private static string NativeTraits(Hero hero)
        {
            return "honor="+hero.GetTraitLevel(DefaultTraits.Honor)+
                ", valor="+hero.GetTraitLevel(DefaultTraits.Valor)+
                ", mercy="+hero.GetTraitLevel(DefaultTraits.Mercy)+
                ", generosity="+hero.GetTraitLevel(DefaultTraits.Generosity)+
                ", calculation="+hero.GetTraitLevel(DefaultTraits.Calculating);
        }
        private static string Office(Hero hero)
        {
            var name=hero.Name.ToString();
            int start=name.LastIndexOf('(');
            if(start<0 || !name.EndsWith(")",StringComparison.Ordinal))
                return "";
            var office=name.Substring(start+1,name.Length-start-2);
            return SandboxRetinueOfficePolicy.Skills(office).Length>0?
                office:"";
        }
        internal static string Capture(SandboxOriginProfile profile,
            Hero player,Hero patron,SandboxLifeHistoryState state)
        {
            if(profile==null || player==null || state==null)
                return "refused:missing_origin";
            var people=new List<Hero>();
            Add(people,player);
            Add(people,player.Father);
            Add(people,player.Mother);
            Add(people,player.Spouse);
            if(player.Children!=null)
                foreach(var child in player.Children)Add(people,child);
            if(player.Father!=null && player.Father.Children!=null)
                foreach(var sibling in player.Father.Children)
                    if(sibling!=null && sibling.Mother==player.Mother)
                        Add(people,sibling);
            if(Clan.PlayerClan!=null)
            {
                foreach(var member in Clan.PlayerClan.Heroes)
                    Add(people,member);
                foreach(var companion in Clan.PlayerClan.Companions)
                    Add(people,companion);
            }
            Add(people,patron);
            foreach(var person in people.Take(SandboxLifeHistoryState.MaxPeople))
            {
                var isPlayer=person==player;
                var isSpouse=person==player.Spouse && !isPlayer;
                var isParent=person==player.Father ||
                    person==player.Mother;
                var isChild=player.Children!=null &&
                    player.Children.Contains(person);
                bool sibling=person!=player && player.Father!=null &&
                    person.Father==player.Father &&
                    person.Mother==player.Mother && !isParent;
                var office=Office(person);
                string station=isPlayer?
                    SandboxStartingLifePolicy.Station(profile.Role,
                        profile.NobleBirthPosition):
                    person==patron?"KingdomClanLeader":
                    isParent?"RecordedParent":
                    isSpouse?"SpouseInPlayerClan":
                    isChild?"RecordedOffspring":
                    sibling?"RecordedSibling":
                    office.Length>0?"HouseholdOfficer":
                    person.IsLord?"NativeClanNoble":"ClanCompanion";
                string occupation=isPlayer?
                    SandboxStartingLifePolicy.Occupation(profile.Role):
                    person==patron?"NativeLord":
                    office.Length>0?office:
                    isChild?"FamilyChild":
                    sibling?"FamilyMember":
                    isSpouse?"HouseholdSpouse":
                    isParent?"Parent":
                    person.Occupation.ToString();
                string clan=person.Clan==null?"":person.Clan.StringId;
                string culture=person.Culture==null?"":
                    person.Culture.StringId;
                var name=person.Name.ToString();
                string description=name+" (age "+(int)person.Age+
                    ", "+culture+"). Recorded station: "+station+
                    ", occupation: "+occupation+
                    ", clan: "+(clan.Length==0?"none":clan)+
                    ". Native character traits: "+NativeTraits(person)+".";
                if(isPlayer && player.Father!=null &&
                    player.Mother!=null)
                    description+=" Native parents: "+
                        player.Father.Name.ToString()+" and "+
                        player.Mother.Name.ToString()+".";
                if(isPlayer && patron!=null)
                    description+=" Personal patron: "+
                        patron.Name.ToString()+
                        " (not proof of troop enlistment).";
                state.Record(new SandboxLifeHistoryState.Person{
                    Id=person.StringId,Name=name,Culture=culture,
                    Age=(int)person.Age,Station=station,
                    Occupation=occupation,HouseholdId=clan,
                    Origin=isPlayer?"CreatorSelected:"+
                        SandboxStartingLifePolicy.Household(
                            profile.Role,profile.EarlyHousehold):
                        "PriorHistoryNotEstablished",
                    Ambition=isPlayer?
                        SandboxStartingLifePolicy.Ambition(
                            profile.Role,profile.NobleBirthPosition,
                            profile.PersonalAmbition):
                        "NotYetModeled",
                    Biography=description
                });
            }
            foreach(var person in people)
            {
                if(person==null)continue;
                if(person.Father!=null)
                    state.Tie(person.StringId,person.Father.StringId,
                        "Father","VerifiedNativeParent");
                if(person.Mother!=null)
                    state.Tie(person.StringId,person.Mother.StringId,
                        "Mother","VerifiedNativeParent");
                if(person.Spouse!=null)
                    state.Tie(person.StringId,person.Spouse.StringId,
                        "Spouse","VerifiedNativeMarriage");
            }
            if(patron!=null)
                state.Tie(player.StringId,patron.StringId,
                    "PersonalPatron","VerifiedNativeLordRelationshipNotEnlistment");
            if(Clan.PlayerClan!=null)
                foreach(var companion in Clan.PlayerClan.Companions)
                    if(companion!=null)
                        state.Tie(player.StringId,companion.StringId,
                            "ClanCompanion","NativeClanRoster");
            state.Remember("StartingLifeRecorded",player.StringId,
                patron==null?"":patron.StringId,
                "role="+profile.Role+
                "|native_station="+SandboxStartingLifePolicy.Station(
                    profile.Role,profile.NobleBirthPosition)+
                "|selected_early_household="+
                    SandboxStartingLifePolicy.Household(
                        profile.Role,profile.EarlyHousehold));
            DiagnosticJournal.Event("cu_living_persons_snapshot",
                "people="+state.People.Count+
                "|bonds="+state.Bonds.Count+
                "|events="+state.Events.Count+
                "|native_heroes_only=true"+
                "|fabricated_prior_memories=false"+
                "|future_dialogue_hooks_not_active=true");
            return "captured:"+state.People.Count;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
            Hero player,Hero patron,SandboxLifeHistoryState state,
            bool verifiedSoldierStart=false)
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
                    SandboxStartingLifePolicy.Station(profile,verifiedSoldierStart):
                    person==patron?"KingdomClanLeader":
                    isParent?"RecordedParent":
                    isSpouse?"SpouseInPlayerClan":
                    isChild?"RecordedOffspring":
                    sibling?"RecordedSibling":
                    office.Length>0?"HouseholdOfficer":
                    person.IsLord?"NativeClanNoble":"ClanCompanion";
                string occupation=isPlayer?
                    SandboxStartingLifePolicy.Occupation(profile,verifiedSoldierStart):
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

        /// <summary>
        /// Daily read-only observation of real native relation scores,
        /// witnessed marriage links and clan moves. No reputation,
        /// loyalty or backstory is invented from a score change.
        /// </summary>
        private static string RealHome(Hero hero,out string evidence)
        {
            evidence="Unverified";
            if(hero==null)return "";
            var residence=hero.StayingInSettlement;
            if(residence!=null)
            {
                evidence="NativeCurrentSettlement";
                return residence.StringId;
            }
            residence=hero.HomeSettlement;
            if(residence!=null)
            {
                evidence="NativeHomeSettlement";
                return residence.StringId;
            }
            return "";
        }

        internal static int CaptureAgenda(SandboxOriginProfile profile,
            SandboxLifeHistoryState life,SandboxCharacterAgendaState agenda,
            Hero player)
        {
            if(profile==null || life==null || agenda==null ||
                player==null)return 0;
            var alive=Hero.AllAliveHeroes.Where(h=>h!=null)
                .GroupBy(h=>h.StringId,StringComparer.Ordinal)
                .ToDictionary(g=>g.Key,g=>g.First(),
                    StringComparer.Ordinal);
            int created=0;
            foreach(var known in life.People)
            {
                Hero person;
                if(!alive.TryGetValue(known.Id,out person))continue;
                bool sibling=person!=player &&
                    player.Father!=null && player.Mother!=null &&
                    person.Father==player.Father &&
                    person.Mother==player.Mother;
                bool inParty=person.PartyBelongedTo!=null &&
                    person.PartyBelongedTo==player.PartyBelongedTo;
                string office=Office(person);
                string evidence;
                string home=RealHome(person,out evidence);
                string goal=person==player?
                    SandboxStartingLifePolicy.Ambition(
                        profile.Role,profile.NobleBirthPosition,
                        profile.PersonalAmbition):
                    SandboxGeneratedCharacterGoalPolicy.Goal(
                        known.Culture,known.Station,
                        office.Length>0?office:known.Occupation,
                        sibling,inParty);
                if(agenda.Register(person.StringId,goal,home,evidence,
                    known.Occupation,known.Culture,known.HouseholdId))
                    created++;
            }
            DiagnosticJournal.Event("cu_character_agendas_created",
                "entries="+created+
                "|homes_unverified="+agenda.Entries.Count(x=>
                    x.HomeEvidence=="Unverified")+
                "|generated_motives_are_not_past_memories=true");
            return created;
        }

        internal static void ObserveAgenda(SandboxLifeHistoryState life,
            SandboxCharacterAgendaState agenda,Hero player)
        {
            if(life==null || agenda==null || player==null)return;
            var alive=Hero.AllAliveHeroes.Where(h=>h!=null)
                .GroupBy(h=>h.StringId,StringComparer.Ordinal)
                .ToDictionary(g=>g.Key,g=>g.First(),
                    StringComparer.Ordinal);
            foreach(var entry in agenda.Entries)
            {
                Hero person;
                if(!alive.TryGetValue(entry.Id,out person))continue;
                string evidence;
                string home=RealHome(person,out evidence);
                string homeChange=agenda.ObserveHome(
                    entry.Id,home,evidence);
                if(homeChange=="native_home_changed")
                    life.Remember("NativeResidenceChangedObserved",
                        person.StringId,home,"evidence="+evidence);
                else if(homeChange=="residence_no_longer_verified")
                    life.Remember("NativeResidenceEvidenceLost",
                        person.StringId,"",
                        "no_presently_verified_home"+
                        "|movement_cause_not_assumed=true");
                if(person==player)continue;
                bool siblings=player.Father!=null &&
                    player.Mother!=null &&
                    person.Father==player.Father &&
                    person.Mother==player.Mother;
                bool actualParty=player.PartyBelongedTo!=null &&
                    person.PartyBelongedTo==player.PartyBelongedTo;
                // Reassess a modeled goal only from current native facts.
                // The changed motive is a hypothesis, not a self-reported
                // NPC decision or a fabricated earlier memory.
                var known=life.People.FirstOrDefault(
                    p=>p.Id==person.StringId);
                if(known!=null)
                {
                    string office=Office(person);
                    string currentOccupation=office.Length>0?
                        office:person.Occupation.ToString();
                    string currentClan=person.Clan==null?"":
                        person.Clan.StringId;
                    string inferredGoal=
                        SandboxGeneratedCharacterGoalPolicy.Goal(
                            person.Culture==null?"":person.Culture.StringId,
                            known.Station,currentOccupation,
                            siblings,actualParty);
                    string previousGoal=entry.Goal;
                    if(agenda.ObserveNativeGoal(entry.Id,inferredGoal,
                        currentOccupation,currentClan)==
                        "goal_reassessed_from_native_context")
                    {
                        life.Remember("ObservedGoalReassessment",
                            person.StringId,player.StringId,
                            "prior="+previousGoal+
                            "|current="+inferredGoal+
                            "|evidence=native_party_kinship_occupation"+
                            "|goal_is_inferred_not_self_reported");
                        DiagnosticJournal.Event(
                            "cu_native_character_goal_reassessed",
                            "hero="+entry.Id+"|prior="+previousGoal+
                            "|current="+inferredGoal);
                    }
                }
                int before=player.GetRelation(person);
                int awarded=agenda.ObserveComradeship(entry.Id,actualParty,
                    person.Age>=18f && player.Age>=18f,siblings,before);
                if(awarded<=0)continue;
                try
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                        player,person,awarded,false);
                    int after=player.GetRelation(person);
                    if(!agenda.ConfirmComradeshipAward(
                        entry.Id,before,after))
                    {
                        DiagnosticJournal.Event(
                            "cu_shared_service_relation_unconfirmed",
                            "player="+player.StringId+
                            "|sibling="+person.StringId+
                            "|before="+before+"|after="+after+
                            "|award_not_consumed=true");
                        continue;
                    }
                    life.Remember("VerifiedSiblingComradeship",
                        player.StringId,person.StringId,
                        "days="+entry.ConsecutiveDaysTogether+
                        "|native_relation_before="+before+
                        "|native_relation_after="+after+
                        "|shared_native_party=true");
                    DiagnosticJournal.Event("cu_shared_service_relation",
                        "player="+player.StringId+
                        "|sibling="+person.StringId+
                        "|days="+entry.ConsecutiveDaysTogether+
                        "|native_relation_before="+before+
                        "|native_relation_after="+after+
                        "|award_confirmed=true");
                }
                catch(Exception error)
                {
                    DiagnosticJournal.Exception(
                        "cu_comradeship_native_action_failed",error);
                }
            }
        }

        internal static int ObserveDaily(SandboxLifeHistoryState state,
            SandboxSocialObservationState social,Hero player)
        {
            if(state==null || social==null || player==null ||
                state.People.Count==0)return 0;
            var living=Hero.AllAliveHeroes.Where(h=>h!=null &&
                !String.IsNullOrEmpty(h.StringId))
                .GroupBy(h=>h.StringId,StringComparer.Ordinal)
                .ToDictionary(g=>g.Key,g=>g.First(),
                    StringComparer.Ordinal);
            int events=0;
            foreach(var record in state.People.ToArray())
            {
                Hero hero;
                if(!living.TryGetValue(record.Id,out hero))continue;
                string clan=hero.Clan==null?"":hero.Clan.StringId;
                if(record.HouseholdId!=clan)
                {
                    state.Remember("NativeClanChangedObserved",
                        hero.StringId,"",
                        "previous="+record.HouseholdId+
                        "|current="+clan+"|cause=unverified");
                    record.HouseholdId=clan;
                    events++;
                }
                if(hero.Spouse!=null && hero.Spouse!=hero &&
                    hero.Spouse.Spouse==hero &&
                    !state.Bonds.Any(x=>x.From==hero.StringId &&
                        x.To==hero.Spouse.StringId &&
                        x.Kind=="Spouse"))
                {
                    if(state.ObserveMarriage(hero.StringId,
                        hero.Spouse.StringId,true))events++;
                }
                if(hero!=player)
                {
                    var change=social.Observe(hero.StringId,
                        player.GetRelation(hero));
                    if(change!=null)
                    {
                        state.Remember("NativeRelationChangedObserved",
                            player.StringId,hero.StringId,
                            "before="+change.Before+
                            "|after="+change.After+
                            "|cause=unverified");
                        events++;
                    }
                }
            }
            if(events>0)
                DiagnosticJournal.Event("cu_living_person_events",
                    "count="+events+
                    "|known_people="+state.People.Count+
                    "|native_state_observed=true"+
                    "|causes_not_fabricated=true");
            return events;
        }
    }
}

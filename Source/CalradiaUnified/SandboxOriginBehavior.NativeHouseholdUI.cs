using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TaleWorlds.Library;

namespace CalradiaUnified
{
    /// <summary>
    /// Read-only, native Bannerlord inquiry for the actual household.
    /// Existing game screens remain the authority for family,
    /// governors, parties and kingdom membership. No new persistence.
    /// </summary>
    public sealed partial class SandboxOriginBehavior
    {
        private static Hero ResolveCampaignHero(string id)
        {
            if(String.IsNullOrWhiteSpace(id))return null;
            // Generated and native Hero objects can be registered in the
            // live campaign without resolving through GetObject<Hero>.
            // The saved ID stays authoritative, never the display label.
            var living=Hero.AllAliveHeroes.FirstOrDefault(h=>h!=null &&
                h.StringId==id);
            if(living!=null)return living;
            var manager=MBObjectManager.Instance;
            var hero=manager==null?null:manager.GetObject<Hero>(id);
            return hero!=null && hero.StringId==id?hero:null;
        }
        private static string NativeHeroDisplayName(string id)
        {
            if(String.IsNullOrWhiteSpace(id))return "none";
            var hero=ResolveCampaignHero(id);
            return hero!=null && hero.Name!=null?
                hero.Name.ToString():"Unknown";
        }
        private static string NativeHeroDisplayNames(string csv)
        {
            if(String.IsNullOrWhiteSpace(csv))return "none";
            return String.Join(", ",csv.Split(',')
                .Where(id=>!String.IsNullOrWhiteSpace(id))
                .Take(6).Select(NativeHeroDisplayName));
        }
        private static string NativePersonName(Hero hero)
        {
            return hero==null || hero.Name==null?"none":
                hero.Name.ToString();
        }

        // Entry shared by the required UIExtenderEx mixin buttons in the
        // native Clan, Party and Kingdom views. The actual campaign
        // behavior owns all role facts and save-backed identities.
        public static bool OpenFromNativeBUTRScreen()
        {
            var campaign=Campaign.Current;
            var b=campaign==null?null:
                campaign.GetCampaignBehavior<SandboxOriginBehavior>();
            if(b==null || Hero.MainHero==null ||
                Clan.PlayerClan==null)return false;
            b.ShowStartingHouseholdStatus();
            return true;
        }
        public static string NativeBUTRStatus(string context)
        {
            if(Campaign.Current==null || Hero.MainHero==null ||
                Clan.PlayerClan==null)return "Campaign records unavailable";
            // Short native-tab labels are deliberate. Names, family and
            // duties belong in the full dossier, not a clipped button.
            if(context=="Party")return "Company and officers";
            if(context=="Kingdom")return "Royal and kingdom affairs";
            return "Household and career dossier";
        }
        private static string NativeDossierTitle(string type)
        {
            switch(type)
            {
                case "Wanderer":return "Wanderer | Life and company";
                case "Outlaw":return "Outlaw | Band and allegiance";
                case "Mercenary":return "Mercenary | Company record";
                case "Soldier":return "Soldier | Enlistment record";
                case "Knight":return "Knight | Oath and household";
                case "Lord":return "Lord | House and estates";
                case "King":return "King | Crown and court";
                default:return "Calradia Unified | Campaign record";
            }
        }
        // These buttons inspect real campaign entities. A Hero must
        // still belong to the player's actual family or companion roster.
        internal static string ContextualHouseholdTitle(Hero person)
        {
            if(person==null || person.Clan==null)return "";
            var clan=person.Clan;
            var king=clan.Kingdom==null?null:clan.Kingdom.Leader;
            bool sovereign=king==person && clan.Kingdom.RulingClan==clan && clan.Leader==person;
            bool royal=king!=null && king.Clan==clan && clan.Kingdom.RulingClan==clan;
            bool spouse=royal && king.Spouse==person && person.Spouse==king;
            bool child=royal && king.Children!=null && king.Children.Contains(person) &&
                (person.Father==king || person.Mother==king);
            if(sovereign || spouse || child)
                return SandboxFamilyPolicy.FamilyTitle(person.IsFemale,sovereign,royal,spouse,child,person.IsLord,false);
            var campaign=Campaign.Current;
            var b=campaign==null?null:campaign.GetCampaignBehavior<SandboxOriginBehavior>();
            if(b!=null && clan==Clan.PlayerClan)
            {
                string office=RecordedRetinueOffice(person);
                if(office.Length>0)return SandboxRetinueOfficePolicy.Profession(office);
                if(person.CompanionOf==clan && clan.Companions.Contains(person))
                {
                    string duty=b.HouseholdOfficeFor(person.StringId);
                    string profession=SandboxRetinueOfficePolicy.Profession(b.ActiveRetainerService(person)?b.retainerService.Rank:duty);
                    if(profession.Length>0)return profession;
                    if(person.StringId==b.wandererCompanionId &&
                        !String.IsNullOrEmpty(b.wandererCompanionProfession))
                        return b.wandererCompanionProfession;
                    return "Companion";
                }
                if(person==Hero.MainHero && b.knightOath.HasCurrentOath && b.knightOath.PlayerId==person.StringId)
                    return "Knight";
            }
            return SandboxFamilyPolicy.FamilyTitle(person.IsFemale,false,false,false,false,person.IsLord,false);
        }
        private bool RecordedNobleFounder(Hero player)
        {
            var clan=Clan.PlayerClan;
            return applied && player!=null && clan!=null && player.Clan==clan &&
                ((lordInheritance.IsRecorded && lordInheritance.FounderId==player.StringId && lordInheritance.ClanId==clan.StringId) ||
                 (kingSuccession.IsRecorded && kingSuccession.FounderId==player.StringId && kingSuccession.RulingClanId==clan.StringId) ||
                 (knightOath.IsConfigured && knightOath.PlayerId==player.StringId));
        }
        private Hero[] FamilyOccupationRepairs()
        {
            var player=Hero.MainHero;var clan=Clan.PlayerClan;
            if(!RecordedNobleFounder(player) || player.IsPrisoner || MobileParty.MainParty==null ||
                MobileParty.MainParty.MapEvent!=null || TaleWorlds.MountAndBlade.Mission.Current!=null)
                return new Hero[0];
            var candidates=new System.Collections.Generic.List<Hero>();
            if(player.Spouse!=null && player.Spouse.Spouse==player)candidates.Add(player.Spouse);
            if(player.Children!=null)candidates.AddRange(player.Children.Where(h=>h!=null &&
                (h.Father==player || h.Mother==player) && h.Father!=null && h.Mother!=null &&
                h.Father.Children!=null && h.Mother.Children!=null &&
                h.Father.Children.Contains(h) && h.Mother.Children.Contains(h)));
            if(player.Father!=null && player.Mother!=null && player.Father.Children!=null &&
                player.Mother.Children!=null)
                candidates.AddRange(player.Father.Children.Where(h=>h!=null && h!=player &&
                    h.Father==player.Father && h.Mother==player.Mother && player.Mother.Children.Contains(h)));
            return candidates.Distinct().Where(h=>h.IsAlive && !h.IsPrisoner && h.Clan==clan &&
                h.CompanionOf==null && !clan.Companions.Contains(h) && h.CharacterObject!=null &&
                h.Occupation==Occupation.Wanderer && h.CharacterObject.Occupation==Occupation.Wanderer &&
                livingPersons.People.Any(p=>p.Id==h.StringId && p.HouseholdId==clan.StringId &&
                    (p.Station=="SpouseInPlayerClan" || p.Station=="RecordedOffspring" || p.Station=="RecordedSibling")))
                .Take(16).ToArray();
        }
        private void ReviewFamilyOccupationRepair()
        {
            var candidates=FamilyOccupationRepairs();
            if(candidates.Length==0){ShowNativeDetail("Family occupations","No eligible family corrections.");return;}
            var ids=candidates.Select(h=>h.StringId).ToArray();
            string owner=Hero.MainHero.StringId;
            InformationManager.ShowInquiry(new InquiryData("Correct family occupations",
                String.Join("\n",candidates.Select(h=>h.Name+" | Wanderer → Lord"))+
                "\nOnly recorded family members are affected. No land, office or title grant.",
                true,true,"Correct","Cancel",()=>
                {
                    if(!CanReviewStartDiagnostics() || Hero.MainHero.StringId!=owner)return;
                    int changed=0;
                    foreach(var h in FamilyOccupationRepairs().Where(h=>ids.Contains(h.StringId)))
                    {
                        try
                        {
                            h.SetNewOccupation(Occupation.Lord);
                            bool confirmed=h.Occupation==Occupation.Lord && h.CharacterObject.Occupation==Occupation.Lord;
                            if(confirmed)changed++;
                            DiagnosticJournal.Event("cu_family_occupation_repair","hero="+h.StringId+
                                "|confirmed="+confirmed+"|minor="+h.IsChild+"|state="+h.HeroState);
                        }
                        catch(Exception error){DiagnosticJournal.Exception("cu_family_occupation_repair_failed",error);}
                    }
                    ShowNativeDetail("Family occupations","Corrected: "+changed+" of "+ids.Length+".");
                },()=>{}),true);
        }
        private void InspectDossierHero(Hero person)
        {
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            if(person==null || !person.IsAlive ||
                player==null || clan==null)return;
            bool family=person==player ||
                (person==player.Spouse &&
                 person.Spouse==player) ||
                (player.Children!=null &&
                 player.Children.Contains(person) &&
                 (person.Father==player || person.Mother==player));
            bool companion=clan.Companions!=null &&
                clan.Companions.Contains(person) &&
                person.CompanionOf==clan;
            if(!family && !companion)return;
            var residence=person.StayingInSettlement ??
                person.CurrentSettlement ??
                person.LastKnownClosestSettlement;
            string relationship=person==player?"Household head":
                person==player.Spouse?"Spouse":
                person.Father==player || person.Mother==player?
                    "Child":"Companion of your clan";
            string parentage=person.Father==null &&
                person.Mother==null?"Parentage not recorded":
                "Mother: "+NativePersonName(person.Mother)+
                " | Father: "+NativePersonName(person.Father);
            string location=residence==null?
                "Location unknown":
                "Known place: "+residence.Name;
            string duty=companion?
                "Clan companion.":
                "Family member.";
            string privateOffice=HouseholdOfficeFor(person.StringId);
            string assignedOffice=ExistingCourtDuty(person);
            if(assignedOffice.Length>0)privateOffice="";
            if(companion && privateOffice.Length>0)
                duty="Assigned household duty: "+privateOffice+
                    ". Reports: "+
                    householdDuties.ReportCount(privateOffice)+
                    ". Speak in person to give or review orders.";
            if(companion && lordInheritance.IsRecorded &&
                NativeCourtOfficer("Master of Arms")==person)
                duty="Master of Arms: personal office. Direct CU garrison orders held; use native garrison controls.";
            else if(companion && kingSuccession.IsRecorded &&
                NativeCourtOfficer("Marshal")==person)
                duty="Marshal: personal office. Direct CU garrison orders held; use native garrison controls.";
            else if(companion && person.GovernorOf!=null)
                duty="Governor of "+
                    person.GovernorOf.Settlement.Name+".";
            else if(companion && person.PartyBelongedTo!=null)
                duty="Current field assignment: "+
                    person.PartyBelongedTo.Name+".";
            string nativeAssignment=NativeHouseholdAssignment(person);
            if(nativeAssignment.Length>0)duty=nativeAssignment;
            string title=ContextualHouseholdTitle(person);
            ShowNativeDetail(person.Name+" | "+(title.Length>0?title:relationship),
                "Age "+(int)person.Age+" | "+relationship+" | "+parentage+
                "\n"+location+
                (ActiveRetainerService(person)?"\n"+RetainerServiceSummary(person)+
                    "\nSpecialties: "+String.Join(", ",SandboxRetinueCulturePolicy.Training(
                        person.Culture==null?"":person.Culture.StringId,retainerService.Rank)):"")+
                "\n"+(assignedOffice.Length>0?"Household office: "+assignedOffice+"\n":"")+duty+
                "\nStanding with you: "+
                (person==player?"your own character":
                    player.GetRelation(person).ToString())+
                "\nCurrent reciprocal spouse: "+
                (person.Spouse!=null && person.Spouse.Spouse==person?
                    NativePersonName(person.Spouse):"not verified")+
                (person==player?"":"\n"+SocialBondDescription(person))+
                "\n\n"+PersonalHistoryText(person));
        }

        private string CulturalSocietyText(Hero person)
        {
            string culture=person==null || person.Culture==null?"":person.Culture.StringId;
            string guidance=SandboxCulturalInheritancePolicy.CustomGuidance(culture);
            string education=SandboxCulturalInheritancePolicy.EducationConvention(culture);
            return "Culture: "+(culture.Length==0?"unverified":culture)+
                "\nCustom: "+(guidance.Length==0?"unestablished":guidance)+
                "\nSuggested upbringing: "+(education.Length==0?"none":education)+
                "\nInheritance custom: "+SandboxCulturalInheritancePolicy.Model(culture)+
                "\nInheritance follows native law.";
        }

        private void OpenDossierFamilyPortraits()
        {
            // The gallery displays original CharacterImageIdentifierVM
            // portraits of native Heroes. It cannot create new kin.
            ScreenManager.PushScreen(new CUFamilyPortraitScreen());
        }

        // The campaign dossier is an index, not a remote way of
        // ordering another person's finances or affection.
        // The actual response is spoken to the real Hero in person.
        private void HearDossierPetition()
        {
            if(!agencyDecisions.HasPending)
            {
                ShowNativeDetail("Private petitions",
                    "No petition waits in your household ledger. "+
                    "Speak to your companions and grown family "+
                    "members in person to learn what concerns them.");
                return;
            }
            var actor=ResolveCampaignHero(
                agencyDecisions.PendingHeroId);
            if(actor==null || !actor.IsAlive)
            {
                ShowNativeDetail("Private petitions",
                    "The petitioner cannot currently be found. "+
                    "No choice or money transfer will be made.");
                return;
            }
            int denars=SandboxAgencyDecisionPolicy.Amount(
                agencyDecisions.PendingKind);
            string concern=agencyDecisions.PendingKind=="Recognition"?
                "wishes their service to be acknowledged":
                denars>0?
                    "seeks "+denars+" denars of household help":
                    "has a private matter to discuss";
            var settlement=actor.StayingInSettlement ??
                actor.CurrentSettlement;
            string where=settlement!=null?
                "Last recorded at "+settlement.Name+".":
                actor.PartyBelongedTo!=null?
                    "Traveling with "+actor.PartyBelongedTo.Name+".":
                    "Their present location is not verified.";
            ShowNativeDetail("Private petition | "+actor.Name,
                actor.Name+" "+concern+".\n"+where+
                "\nMeet this person and ask about their affairs "+
                "through the game's normal conversation. "+
                "Only the face-to-face answer can change "+
                "real gold or relationships.");
            DiagnosticJournal.Event("cu_petition_ledger_consulted",
                "actor="+actor.StringId+
                "|location_known="+(settlement!=null ||
                   actor.PartyBelongedTo!=null)+
                "|remote_payment_or_relation_action=false");
        }

        // Role briefings point to real native authorities. They do not
        // issue distant NPC orders, fabricate salaries, or grant lands.
        private string DossierOfficerLabel(Hero hero)
        {
            if(hero==null || !hero.IsAlive)return "UNAVAILABLE";
            string nativeAssignment=NativeHouseholdAssignment(hero);
            if(nativeAssignment.Length>0)return nativeAssignment;
            string office=EffectiveHouseholdDuty(hero);
            if(office.Length>0)return office.ToUpperInvariant();
            if(NativeCourtOfficer("Master of Arms")==hero)
                return "MASTER OF ARMS";
            if(NativeCourtOfficer("Marshal")==hero)return "MARSHAL";
            if(NativeCourtOfficer("Royal Minister")==hero)
                return "ROYAL MINISTER";
            if(NativeCourtOfficer("Kingsguard")==hero)
                return "KINGSGUARD";
            return "COMPANION";
        }

        private void ReviewDossierCalling()
        {
            ObserveCareerChronicleDaily();
            ShowNativeDetail("Career history",
                PersonalChronicleText());
        }

        private void RegisterStartDiagnosticMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("cu_household_records","cu_start_diagnostic_records","Troubleshooting",
                    args=>{args.optionLeaveType=TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType.Submenu;
                        return CanReviewStartDiagnostics();},
                    args=>ShowStartDiagnostics(),false);
        }
        private bool CanReviewStartDiagnostics()
        {
            var c=Campaign.Current;
            return c!=null && c.GetCampaignBehavior<SandboxOriginBehavior>()==this &&
                Hero.MainHero!=null && Hero.MainHero.IsAlive && !Hero.MainHero.IsPrisoner &&
                Clan.PlayerClan!=null && MobileParty.MainParty!=null &&
                MobileParty.MainParty.MapEvent==null && TaleWorlds.MountAndBlade.Mission.Current==null;
        }
        private void ShowStartDiagnostics()
        {
            if(!CanReviewStartDiagnostics())return;
            var player=Hero.MainHero;var clan=Clan.PlayerClan;
            string owner=player.StringId;
            var spouse=player.Spouse;
            bool marriage=spouse!=null && spouse.Spouse==player;
            int children=player.Children==null?0:player.Children.Count(h=>h!=null &&
                (h.Father==player || h.Mother==player));
            int holdings=clan.Settlements==null?0:clan.Settlements.Count;
            string historic=String.IsNullOrEmpty(startAuditOwner)?"No startup record for this campaign.":
                "Starting career: "+startAuditRole+" | Result: "+startAuditStatus+
                "\nOriginal founder: "+(startAuditOwner==player.StringId?"current player":"previous player")+
                "\nReason: "+(String.IsNullOrEmpty(startAuditReason)?"none recorded":startAuditReason);
            string current="Current campaign\nKingdom: "+
                (clan.Kingdom==null?"none":clan.Kingdom.Name.ToString())+
                " | Ruling clan: "+(clan.Kingdom!=null && clan.Kingdom.RulingClan==clan)+
                " | Mercenary: "+clan.IsUnderMercenaryService+
                "\nHoldings: "+holdings+" | Reciprocal marriage: "+marriage+" | Linked children: "+children+
                " | Full siblings: "+CountNativeFullSiblings(player)+
                "\nFounder occupation: "+player.Occupation+
                " | Spouse: "+(spouse==null?"none":spouse.Occupation.ToString())+
                "\nChildren: "+(player.Children==null?"none":String.Join(", ",player.Children.Where(h=>h!=null)
                    .Take(8).Select(h=>h.Name+": "+h.Occupation)))+
                "\nKnight oath recorded: "+knightOath.IsConfigured+" | Lord claim recorded: "+lordInheritance.IsRecorded+
                " | King claim recorded: "+kingSuccession.IsRecorded+
                "\nServe as Soldier: "+SandboxSaSBridge.HasSupportedProvider()+
                " (required for Soldier).";
            DiagnosticJournal.Event("cu_start_diagnostic_open","historic="+(startAuditStatus??"")+
                "|current_owner="+(startAuditOwner==player.StringId)+"|native_read_only=true");
            string detail=historic+"\n\n"+current+
                "\n\n"+(familyRecovery.NeedsReview && familyRecovery.PlayerId==player.StringId?familyRecovery.Describe()+"\n\n":"")+
                "Original requested settings:\n"+(String.IsNullOrEmpty(startAuditRequest)?"not recorded":startAuditRequest.Replace("|","\n"));
            if(FamilyOccupationRepairs().Length==0){ShowNativeDetail("CU diagnostics",detail);return;}
            // Repair is a second, explicit confirmation, never a side effect of viewing the report.
            InformationManager.ShowInquiry(new InquiryData("CU diagnostics",detail,true,true,
                "Close","Family occupations",()=>{},()=>
                {
                    if(CanReviewStartDiagnostics() && Hero.MainHero.StringId==owner)
                        ReviewFamilyOccupationRepair();
                }),true);
        }
        private void ShowStartingHouseholdStatus()
        {
            var player=Hero.MainHero;
            var clan=Clan.PlayerClan;
            if(player==null || clan==null)return;
            var children=player.Children==null?new Hero[0]:
                player.Children.Where(h=>h!=null && h.IsAlive)
                    .OrderBy(h=>h.StringId,StringComparer.Ordinal)
                    .Take(6).ToArray();
            var officers=clan.Companions==null?new Hero[0]:
                clan.Companions.Where(h=>h!=null && h.IsAlive &&
                        h.CompanionOf==clan)
                    .OrderBy(h=>h.StringId,StringComparer.Ordinal)
                    .Take(8).ToArray();
            string family="Family roll: "+children.Length+
                " living children.";
            string assignment=officers.Length==0?
                "No companions.":
                "Select a companion.";
            string role="Career unknown.";
            string type="none";
            if(knightOath.IsConfigured)
            {
                var patron=KnightOathPatron();
                type="Knight";
                role="Sworn to "+NativePersonName(patron)+". "+
                    (knightOath.LastMusterDay<0?
                        "First duty: meet your lord in person.":
                        "Next audience: day "+knightOath.NextMusterDay+".")+
                    "\nStanding with lord: "+(patron==null?
                        "unknown":player.GetRelation(patron).ToString())+
                    " | Escort: "+knightEscort.Status+
                    "\nPersonal standing: "+
                    KnightVerifiedStanding()+
                    " | Service orders completed: "+
                    knightServiceOrders.CompletedOrders+
                    "\nRuler's hearing: "+
                    (knightServiceOrders.RoyalHearingDay>=0?
                        "personally heard":
                        knightServiceOrders.RecommendationDay>=0?
                        "patron's recommendation recorded":
                        "not requested")+
                    "\nCurrent order: "+knightServiceOrders.Status+
                    (knightServiceOrders.Active?
                        " ("+knightServiceOrders.Order+
                        ", deadline day "+knightServiceOrders.DueDay+")":"")+
                    "\nYour household remains independent.";
            }
            else if(lordInheritance.IsRecorded)
            {
                type="Lord";
                role=(CurrentNobleAuthority(false) && clan.StringId==lordInheritance.ClanId?
                    "Native vassal of "+clan.Kingdom.Name+". ":
                    "Founding Lord's household record; current vassal authority not verified. ")+
                    "Household head: "+NativePersonName(clan.Leader)+
                    "\nEstates held: "+(clan.Settlements==null?0:
                        clan.Settlements.Count)+
                    " | Named heir: "+
                    NativeHeroDisplayName(lordInheritance.PrimaryHeirId)+
                    "\nInheritance custom: "+lordInheritance.Rule+
                    ". Claims do not automatically grant land.";
            }
            else if(kingSuccession.IsRecorded)
            {
                type="King";
                var minister=NativeCourtOfficer("Royal Minister");
                var marshal=NativeCourtOfficer("Marshal");
                var guard=NativeCourtOfficer("Kingsguard");
                role=(CurrentNobleAuthority(true)?
                    "You hold the native crown of "+clan.Kingdom.Name+".":
                    "Your claim to the crown is not verified.")+
                    "\nMinister: "+NativePersonName(minister)+
                    " | Marshal: "+NativePersonName(marshal)+
                    "\nKingsguard: "+NativePersonName(guard)+
                    " | Council: "+(NativeRoyalCouncilPresent(false)?
                        "verified officers and seat":royalCouncil.IsRecorded?
                        "recorded; current court incomplete":"not yet appointed")+
                    "\nThe kingdom assembly governs lawful decisions.";
            }
            else if(roleEconomy.Started)
            {
                // These four non-noble paths retain their own identities
                // and verified campaign state, without fake kingdom
                // standing, salaries, commands or noble households.
                type=roleEconomy.Role=="EnlistedSoldier"?
                    "Soldier":roleEconomy.Role;
                if(type!="Wanderer" && type!="Outlaw" &&
                   type!="Mercenary" && type!="Soldier")
                    type="Campaign";
                string employer=clan.Kingdom==null?
                    "no kingdom":clan.Kingdom.Name.ToString();
                string career=roleEconomy.CareerStatus;
                string actualDailyPay=roleEconomy.VerifiedExternalDailyWage<0?
                    "not confirmed":
                    roleEconomy.VerifiedExternalDailyWage+" denars per day";
                switch(type)
                {
                    case "Wanderer":
                        role="Your road is your own. Choose which "+
                            "town, company and opportunity to pursue."+
                            "\nCurrent calling: "+career+
                            " | Purse: "+player.Gold+" denars."+
                            "\nCompanions and provisions are "+
                            "those recorded by your actual party.";
                        break;
                    case "Outlaw":
                        role="You live beyond the protection of "+
                            "the lords. Keep your band supplied."+
                            "\nBand standing: "+career+
                            " | Purse: "+player.Gold+" denars."+
                            "\nThe patrols and settlements still "+
                            "obey Bannerlord's real law and factions.";
                        break;
                    case "Mercenary":
                        role="Your company fights for coin, not "+
                            "a promised noble birth."+
                            "\nEmployer: "+employer+
                            " | Contract: "+(clan.IsUnderMercenaryService?
                                "native mercenary service":"unverified")+
                            "\nStanding: "+career+
                            " | Verified wage: "+actualDailyPay+".";
                        break;
                    case "Soldier":
                        role="Your name is entered among the "+
                            "soldiers of a real host."+
                            "\nEnlistment: "+(serviceCareer
                                .VerifiedExternalEnlistment?
                                "confirmed":"not verified")+
                            " | Commander: "+
                            NativeHeroDisplayName(serviceCareer.PatronId)+
                            "\nRank and pay follow your native "+
                            "enlistment provider, not this ledger.";
                        break;
                    default:
                        role="Present calling: "+career+
                            "\nRealm: "+employer+
                            " | Purse: "+player.Gold+" denars.";
                        break;
                }
            }
            DiagnosticJournal.Event("cu_native_household_display_opened",
                "result=OPENED|role="+type+
                "|spouse_present="+(player.Spouse!=null)+
                "|live_children="+children.Length+
                "|native_companions="+officers.Length+
                "|native_read_only=true");
            // Use a native Gauntlet screen for clear reading rather than
            // sending several paragraphs through a one-button inquiry.
            // The prior native inquiry remains as an exception fallback.
            try
            {
                ScreenManager.PushScreen(new CUNobleDossierScreen(
                    type,
                    NativeDossierTitle(type),
                    "Clan: "+clan.Name,
                    family,assignment,role,
                    "",
                    InspectDossierHero,
                    OpenDossierFamilyPortraits,
                    ()=>OpenCampaignAffairs(),
                    ReviewDossierCalling,
                    DossierOfficerLabel));
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_native_household_dossier_fallback",error);
                ShowNativeDetail("Starting household | "+type,
                    family+"\n\n"+assignment+"\n\n"+role+
                    "\n\nNative Clan, Party and Kingdom screens show "+
                    "the current game-owned state.");
            }
        }
    }
}

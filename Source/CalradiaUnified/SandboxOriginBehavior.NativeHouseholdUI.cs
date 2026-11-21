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
                hero.Name.ToString():"native Hero not currently resolved";
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
                "Present location not recorded":
                "Known place: "+residence.Name;
            string duty=companion?
                "Serving in the actual clan companion roster.":
                "Family belonging verified from native Hero links.";
            ShowNativeDetail(person.Name+" | "+relationship,
                "Age "+(int)person.Age+" | "+parentage+
                "\n"+location+
                "\n"+duty+
                "\nStanding with you: "+
                (person==player?"your own character":
                    player.GetRelation(person).ToString()));
        }

        private void OpenDossierFamilyPortraits()
        {
            // The gallery displays original CharacterImageIdentifierVM
            // portraits of native Heroes. It cannot create new kin.
            ScreenManager.PushScreen(new CUFamilyPortraitScreen());
        }

        private void DeclineDossierPetition()
        {
            var actor=RequestedCharacter();
            if(actor==null)return;
            InformationManager.ShowInquiry(new InquiryData(
                "Decline "+actor.Name,
                "Decline this person's outstanding request? "+
                "The native relationship change will be recorded.",
                true,true,"Decline","Not now",
                ()=>AnswerCharacterRequest(false),()=>{}),true);
        }

        private void HearDossierPetition()
        {
            OfferLocalCharacterRequest(Hero.MainHero);
            var actor=RequestedCharacter();
            if(actor==null)
            {
                ShowNativeDetail("Petitions",
                    "No one currently present has a pending request. "+
                    "Visit your household and companions in person.");
                return;
            }
            int denars=SandboxAgencyDecisionPolicy.Amount(
                agencyDecisions.PendingKind);
            string request=agencyDecisions.PendingKind=="Recognition"?
                "asks you to recognize their work":
                "asks for "+denars+" denars from your actual purse";
            InformationManager.ShowInquiry(new InquiryData(
                actor.Name+" | A private petition",
                actor.Name+" "+request+
                ". You may offer help or decline. "+
                "Both choices follow the campaign's real "+
                "gold and relationship rules.",
                true,true,"Offer help","Decline",
                ()=>ConfirmCharacterSupport(),
                ()=>DeclineDossierPetition()),true);
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
                " living children. View every portrait below.";
            string assignment=officers.Length==0?
                "No appointed companions in your clan.":
                "Tap a portrait to inspect an actual companion.";
            string role="Unverified starting role.";
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
                    "\nYour household remains independent.";
            }
            else if(lordInheritance.IsRecorded)
            {
                type="Lord";
                role="Lord of "+clan.Name+". "+
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
                role=(clan.Kingdom!=null &&
                    clan.Kingdom.RulingClan==clan?
                    "You hold the native crown of "+clan.Kingdom.Name+".":
                    "Your claim to the crown is not verified.")+
                    "\nMinister: "+NativePersonName(minister)+
                    " | Marshal: "+NativePersonName(marshal)+
                    "\nKingsguard: "+NativePersonName(guard)+
                    " | Council: "+(royalCouncil.IsRecorded?
                        "appointed":"not yet appointed")+
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
                            " | Purse: "+player.Gold+" denars.";
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
                    "Live native household records. No fabricated titles.",
                    InspectDossierHero,
                    OpenDossierFamilyPortraits,
                    HearDossierPetition));
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

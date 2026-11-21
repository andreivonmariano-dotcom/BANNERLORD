using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Portrait-led, read-only overview of current native Heroes.
    /// The only interactive decision is forwarded to the existing
    /// physically-present, native-backed household petition handler.
    /// </summary>
    public sealed class CUNobleDossierScreen : ScreenBase
    {
        private readonly string role;
        private readonly string heading;
        private readonly string clan;
        private readonly string family;
        private readonly string officers;
        private readonly string duty;
        private readonly string footnote;
        private readonly Action<Hero> inspectPerson;
        private readonly Action openFamily;
        private readonly Action hearPetition;
        private readonly Action reviewCalling;
        private GauntletLayer layer;
        private CUNobleDossierVM viewModel;

        public CUNobleDossierScreen(string originRole,string title,
            string clanTitle,string familyDetails,
            string officerDetails,string dutyDetails,string note,
            Action<Hero> inspectPersonAction,
            Action openFamilyAction,Action petitionAction,
            Action reviewCallingAction)
        {
            role=originRole??"";
            heading=title??"";
            clan=clanTitle??"";
            family=familyDetails??"";
            officers=officerDetails??"";
            duty=dutyDetails??"";
            footnote=note??"";
            inspectPerson=inspectPersonAction;
            openFamily=openFamilyAction;
            hearPetition=petitionAction;
            reviewCalling=reviewCallingAction;
        }
        protected override void OnInitialize()
        {
            base.OnInitialize();
            viewModel=new CUNobleDossierVM(
                role,heading,clan,family,officers,duty,footnote,
                ()=>CloseSafely(),inspectPerson,openFamily,hearPetition,
                reviewCalling);
            layer=new GauntletLayer("CUNobleDossier",130,false)
            {
                IsFocusLayer=true
            };
            AddLayer(layer);
            layer.InputRestrictions.SetInputRestrictions();
            layer.LoadMovie("CUNobleDossier",viewModel);
            DiagnosticJournal.Event("cu_native_household_dossier_attached",
                "role="+role+
                "|native_gauntlet_movie_loaded=true"+
                "|layout=portrait_family_officers_role_duty"+
                "|native_portraits="+viewModel.FamilyMembers.Count+
                "|native_officers="+viewModel.OfficerMembers.Count+
                "|native_read_only=true");
        }
        protected override void OnActivate()
        {
            base.OnActivate();
            if(layer!=null)ScreenManager.TrySetFocus(layer);
        }
        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            if(ScreenManager.TopScreen==this &&
                Input.IsKeyReleased(InputKey.Escape))
                CloseSafely();
        }
        private void CloseSafely()
        {
            if(ScreenManager.TopScreen==this)
                ScreenManager.PopScreen();
        }
        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            if(layer!=null)
            {
                layer.IsFocusLayer=false;
                ScreenManager.TryLoseFocus(layer);
            }
        }
        protected override void OnFinalize()
        {
            base.OnFinalize();
            if(layer!=null)RemoveLayer(layer);
            layer=null;
            viewModel=null;
        }
    }

    public sealed class CUNobleDossierVM : ViewModel
    {
        private readonly Action close;
        private readonly Action openFamily;
        private readonly Action hearPetition;
        private readonly Action reviewCalling;

        [DataSourceProperty]
        public MBBindingList<CUFamilyPortraitVM> FamilyMembers
            {get;private set;}
        [DataSourceProperty]
        public MBBindingList<CUFamilyPortraitVM> OfficerMembers
            {get;private set;}

        public CUNobleDossierVM(string role,string title,string clan,
            string family,string officers,string duty,string note,
            Action closeAction,Action<Hero> inspectAction,
            Action openFamilyAction,Action petitionAction,
            Action reviewCallingAction)
        {
            Title=title;
            Clan=clan;
            Family=family;
            Officers=officers;
            Duty=duty;
            Footer=note;
            close=closeAction;
            openFamily=openFamilyAction;
            hearPetition=petitionAction;
            reviewCalling=reviewCallingAction;
            FamilyMembers=new MBBindingList<CUFamilyPortraitVM>();
            OfficerMembers=new MBBindingList<CUFamilyPortraitVM>();
            var player=Hero.MainHero;
            if(player!=null)
            {
                FamilyMembers.Add(new CUFamilyPortraitVM(
                    player,"HOUSE HEAD",inspectAction));
                if(player.Spouse!=null && player.Spouse.IsAlive &&
                    player.Spouse.Spouse==player)
                    FamilyMembers.Add(new CUFamilyPortraitVM(
                        player.Spouse,"SPOUSE",inspectAction));
                if(player.Children!=null)
                {
                    foreach(var child in player.Children
                        .Where(h=>h!=null && h.IsAlive &&
                            (h.Father==player || h.Mother==player))
                        .OrderByDescending(h=>h.Age)
                        .ThenBy(h=>h.StringId,StringComparer.Ordinal)
                        .Take(Math.Max(0,3-FamilyMembers.Count)))
                        FamilyMembers.Add(new CUFamilyPortraitVM(
                            child,"CHILD",inspectAction));
                }
            }
            var clanOfPlayer=Clan.PlayerClan;
            if(clanOfPlayer!=null && clanOfPlayer.Companions!=null)
            {
                foreach(var officer in clanOfPlayer.Companions
                    .Where(h=>h!=null && h.IsAlive &&
                        h.CompanionOf==clanOfPlayer)
                    .OrderBy(h=>h.StringId,StringComparer.Ordinal)
                    .Take(3))
                    OfficerMembers.Add(new CUFamilyPortraitVM(
                        officer,"OFFICER",inspectAction));
            }

            RoleActionLabel=role=="Wanderer"?"My road":
                role=="Outlaw"?"My band":
                role=="Mercenary"?"My contract":
                role=="Soldier"?"My enlistment":
                role=="Knight"?"My sworn lord":
                role=="Lord"?"My estates":
                role=="King"?"My council":"My duties";
            switch(role)
            {
                case "Wanderer":
                    SectionFamilyTitle="People on the road";
                    SectionOfficersTitle="Traveling companions";
                    SectionDutyTitle="Your road";
                    break;
                case "Outlaw":
                    SectionFamilyTitle="Your own people";
                    SectionOfficersTitle="Band and lieutenants";
                    SectionDutyTitle="Life beyond the law";
                    break;
                case "Mercenary":
                    SectionFamilyTitle="People of the company";
                    SectionOfficersTitle="Company officers";
                    SectionDutyTitle="Contract and payment";
                    break;
                case "Soldier":
                    SectionFamilyTitle="Family and home";
                    SectionOfficersTitle="Comrades in your clan";
                    SectionDutyTitle="Rank and service";
                    break;
                case "Knight":
                    SectionFamilyTitle="Your household";
                    SectionOfficersTitle="Squires and companions";
                    SectionDutyTitle="Oath and service";
                    break;
                case "Lord":
                    SectionFamilyTitle="Dynasty and heirs";
                    SectionOfficersTitle="Household officers";
                    SectionDutyTitle="Land and succession";
                    break;
                case "King":
                    SectionFamilyTitle="Royal house";
                    SectionOfficersTitle="Court officers";
                    SectionDutyTitle="Crown and council";
                    break;
                default:
                    SectionFamilyTitle="Family and lineage";
                    SectionOfficersTitle="Company and officers";
                    SectionDutyTitle="Current duties";
                    break;
            }
        }

        [DataSourceProperty] public string Title {get;private set;}
        [DataSourceProperty] public string Clan {get;private set;}
        [DataSourceProperty] public string SectionFamilyTitle
            {get;private set;}
        [DataSourceProperty] public string SectionOfficersTitle
            {get;private set;}
        [DataSourceProperty] public string SectionDutyTitle
            {get;private set;}
        [DataSourceProperty] public string Family {get;private set;}
        [DataSourceProperty] public string Officers {get;private set;}
        [DataSourceProperty] public string Duty {get;private set;}
        [DataSourceProperty] public string Footer {get;private set;}
        [DataSourceProperty] public string RoleActionLabel {get;private set;}

        public void ExecuteClose()
        {
            if(close!=null)close();
        }
        public void ExecuteOpenFamily()
        {
            if(openFamily!=null)openFamily();
        }
        public void ExecuteHearPetition()
        {
            if(hearPetition!=null)hearPetition();
        }
        public void ExecuteRoleAction()
        {
            if(reviewCalling!=null)reviewCalling();
        }
    }
}

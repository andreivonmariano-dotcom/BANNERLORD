using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Native Gauntlet dossier. Only observes campaign-owned people,
    /// officers and service status supplied by SandboxOriginBehavior.
    /// It does not create a Hero, change a clan, or enact a title.
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
        private GauntletLayer layer;
        private CUNobleDossierVM viewModel;

        public CUNobleDossierScreen(string originRole,string title,
            string clanTitle,string familyDetails,
            string officerDetails,string dutyDetails,string note)
        {
            role=originRole??"";
            heading=title??"";
            clan=clanTitle??"";
            family=familyDetails??"";
            officers=officerDetails??"";
            duty=dutyDetails??"";
            footnote=note??"";
        }
        protected override void OnInitialize()
        {
            base.OnInitialize();
            viewModel=new CUNobleDossierVM(
                role,heading,clan,family,officers,duty,footnote,
                () => CloseSafely());
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
                "|layout=family_officers_service"+
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
        public CUNobleDossierVM(string role,string title,string clan,
            string family,string officers,string duty,string note,
            Action closeAction)
        {
            Title=title;Clan=clan;Family=family;
            Officers=officers;Duty=duty;Footer=note;
            close=closeAction;
            // The same native Gauntlet language, but contextual
            // headings for all seven distinct career identities.
            switch(role)
            {
                case "Wanderer":
                    SectionFamilyTitle="Personal ties";
                    SectionOfficersTitle="Traveling companions";
                    SectionDutyTitle="Life on the road";
                    break;
                case "Outlaw":
                    SectionFamilyTitle="Personal ties";
                    SectionOfficersTitle="Band and lieutenant";
                    SectionDutyTitle="Outlaw standing";
                    break;
                case "Mercenary":
                    SectionFamilyTitle="Household";
                    SectionOfficersTitle="Company officers";
                    SectionDutyTitle="Contract and service";
                    break;
                case "Soldier":
                    SectionFamilyTitle="Family";
                    SectionOfficersTitle="Party and commanders";
                    SectionDutyTitle="Enlistment and rank";
                    break;
                case "Knight":
                    SectionFamilyTitle="Noble family";
                    SectionOfficersTitle="Squire and companions";
                    SectionDutyTitle="Oath and field service";
                    break;
                case "Lord":
                    SectionFamilyTitle="Dynasty and heirs";
                    SectionOfficersTitle="Household officers";
                    SectionDutyTitle="Fiefs and inheritance";
                    break;
                case "King":
                    SectionFamilyTitle="Royal family";
                    SectionOfficersTitle="Council and Kingsguard";
                    SectionDutyTitle="Crown and government";
                    break;
                default:
                    SectionFamilyTitle="Family and lineage";
                    SectionOfficersTitle="Companions and officers";
                    SectionDutyTitle="Career and current duties";
                    break;
            }
        }
        [DataSourceProperty]
        public string Title {get;private set;}
        [DataSourceProperty]
        public string Clan {get;private set;}
        [DataSourceProperty]
        public string SectionFamilyTitle {get;private set;}
        [DataSourceProperty]
        public string SectionOfficersTitle {get;private set;}
        [DataSourceProperty]
        public string SectionDutyTitle {get;private set;}
        [DataSourceProperty]
        public string Family {get;private set;}
        [DataSourceProperty]
        public string Officers {get;private set;}
        [DataSourceProperty]
        public string Duty {get;private set;}
        [DataSourceProperty]
        public string Footer {get;private set;}
        public void ExecuteClose()
        {
            if(close!=null)close();
        }
    }
}

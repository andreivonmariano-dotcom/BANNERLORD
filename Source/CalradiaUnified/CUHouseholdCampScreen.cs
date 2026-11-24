using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Illustrated 2D household ledger. Its portrait art comes only
    /// from actual native Hero character codes. No mission, Agent,
    /// special camera or scene transition is opened.
    /// </summary>
    public sealed class CUHouseholdCampScreen : ScreenBase
    {
        private readonly List<Hero> present;
        private readonly string banner,roster,officers,letters,service,chronicle;
        private readonly Action<Hero> inspect;
        private GauntletLayer layer;
        private CUHouseholdCampVM data;

        public CUHouseholdCampScreen(List<Hero> presentHeroes,
            string bannerName,string presentRoll,string dutyText,
            string correspondence,string serviceText,string chronicleText,
            Action<Hero> inspectAction)
        {
            present=presentHeroes??new List<Hero>();
            banner=bannerName??"Your company";
            roster=presentRoll??"";
            officers=dutyText??"";
            letters=correspondence??"";
            service=serviceText??"";
            chronicle=chronicleText??"";
            inspect=inspectAction;
        }
        protected override void OnInitialize()
        {
            base.OnInitialize();
            data=new CUHouseholdCampVM(present,banner,roster,
                officers,letters,service,chronicle,inspect,CloseSafely);
            layer=new GauntletLayer("CUHouseholdCamp",130,false)
            {
                IsFocusLayer=true
            };
            AddLayer(layer);
            layer.InputRestrictions.SetInputRestrictions();
            layer.LoadMovie("CUHouseholdCamp",data);
            DiagnosticJournal.Event("cu_illustrated_household_camp_open",
                "layout=two_dimensional_portrait_ledger"+
                "|actual_party_heroes="+data.Attendees.Count+
                "|native_portraits=true|cutscene=false"+
                "|remote_payments=false");
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
            data=null;
        }
    }

    public sealed class CUHouseholdCampVM : ViewModel
    {
        private readonly Action close;
        private readonly string roll,officerText,letterText,serviceText,chronicleText;
        [DataSourceProperty]
        public MBBindingList<CUFamilyPortraitVM> Attendees
            {get;private set;}
        [DataSourceProperty] public string Banner {get;private set;}
        [DataSourceProperty] public string Attendance {get;private set;}
        [DataSourceProperty] public string SectionTitle {get;private set;}
        [DataSourceProperty] public string SectionBody {get;private set;}
        [DataSourceProperty] public string SectionHint {get;private set;}

        public CUHouseholdCampVM(List<Hero> people,string banner,
            string roster,string officers,string letters,
            string service,string chronicle,Action<Hero> inspect,
            Action closeAction)
        {
            close=closeAction;
            Banner=banner;
            roll=roster;officerText=officers;
            letterText=letters;serviceText=service;
            chronicleText=chronicle;
            Attendees=new MBBindingList<CUFamilyPortraitVM>();
            foreach(var person in people)
            {
                if(person==null || !person.IsAlive ||
                    !person.IsActive || person.IsPrisoner ||
                    person.Age<18f || person.CharacterObject==null)
                    continue;
                Attendees.Add(new CUFamilyPortraitVM(person,
                    "TRAVELING WITH YOU",inspect));
                if(Attendees.Count>=6)break;
            }
            Attendance=people.Count>Attendees.Count?
                people.Count+" present (first six pictured)":
                Attendees.Count+" present household Heroes";
            SetSection("People","The company at your side",roll,
                "Select a portrait to inspect a real companion.");
        }
        private void SetSection(string page,string title,string body,
            string hint)
        {
            SectionTitle=title;
            SectionBody=body;
            SectionHint=hint;
            OnPropertyChangedWithValue(SectionTitle,
                "SectionTitle");
            OnPropertyChangedWithValue(SectionBody,
                "SectionBody");
            OnPropertyChangedWithValue(SectionHint,
                "SectionHint");
            DiagnosticJournal.Event("cu_household_camp_page",
                "page="+page+"|read_only=true"+
                "|native_party_members_only=true");
        }
        public void ExecutePeople()
        {
            SetSection("People","The company at your side",roll,
                "Click a portrait for an actual native Hero record.");
        }
        public void ExecuteOfficers()
        {
            SetSection("Officers","Officers of your household",
                officerText,
                "Appointments and duty reports require real conversations.");
        }
        public void ExecuteLetters()
        {
            SetSection("Letters","Private letters and petitions",
                letterText,
                "No distant payment or relation change can be made here.");
        }
        public void ExecuteService()
        {
            SetSection("Service","Your present charge",
                serviceText,
                "Service credit comes only from native campaign evidence.");
        }
        public void ExecuteChronicle()
        {
            SetSection("Chronicle","The record of your household",
                chronicleText,
                "Only observed events and completed petitions are entered.");
        }
        public void ExecuteClose()
        {
            if(close!=null)close();
        }
    }
}

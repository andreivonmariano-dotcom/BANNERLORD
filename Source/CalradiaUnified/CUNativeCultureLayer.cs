using System;
using System.Collections.Generic;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// One CU-controlled culture panel above the still game-owned native
    /// CharacterCreationCultureStageView. The native VM receives every select,
    /// Continue and Back. If any private pinned member is unavailable,
    /// the overlay never opens and the normal native culture screen stays.
    /// </summary>
    internal static class CUNativeCultureLayer
    {
        private static CharacterCreationScreen host;
        private static CharacterCreationScreen failedHost;
        private static GauntletLayer layer;
        private static GauntletMovieIdentifier movie;
        private static CUNativeCultureChooserVM vm;
        private static bool abandonRequested;

        public static void Tick(CharacterCreationScreen nativeScreen,
            CharacterCreationManager manager)
        {
            bool cultureStage=manager!=null &&
                manager.CurrentStage!=null &&
                manager.CurrentStage.GetType().Name==
                    "CharacterCreationCultureStage";
            if (nativeScreen==null || nativeScreen.IsFinalized ||
                !cultureStage)
            {
                Detach();
                if(nativeScreen==null || nativeScreen.IsFinalized)
                    failedHost=null;
                return;
            }

            if (abandonRequested)
            {
                failedHost=nativeScreen;
                Detach();
                RestoreNativeCultureFocus(nativeScreen);
                return;
            }
            if (Object.ReferenceEquals(nativeScreen,host))
            {
                if (vm!=null) vm.Sync();
                return;
            }
            if (Object.ReferenceEquals(nativeScreen,failedHost))
                return;

            CUNativeCultureController controller;
            string reason;
            if (!CUNativeCultureController.TryBind(nativeScreen,
                out controller,out reason))
            {
                failedHost=nativeScreen;
                DiagnosticJournal.Event("cu_native_culture_fallback",
                    "reason="+reason+"|native_controls_retained=true");
                return;
            }

            GauntletLayer nextLayer=null;
            GauntletMovieIdentifier nextMovie=null;
            try
            {
                var nextVM=new CUNativeCultureChooserVM(controller,
                    RequestAbandon);
                nextLayer=new GauntletLayer("CUNativeCultureChooser",60,false);
                nextLayer.IsFocusLayer=true;
                nextLayer.InputRestrictions.SetInputRestrictions();
                nextMovie=nextLayer.LoadMovie("CUNativeCultureChooser",nextVM);
                nativeScreen.AddLayer(nextLayer);
                ScreenManager.TrySetFocus(nextLayer);
                host=nativeScreen;
                failedHost=null;
                layer=nextLayer;
                movie=nextMovie;
                vm=nextVM;
                abandonRequested=false;
                DiagnosticJournal.Event("cu_native_culture_composer_attached",
                    "native_culture_view_preserved=true" +
                    "|native_culture_vm_delegated=true" +
                    "|culture_choices="+controller.Options.Count+
                    "|game_stage_not_replaced=true");
            }
            catch (Exception error)
            {
                failedHost=nativeScreen;
                DiagnosticJournal.Exception("cu_native_culture_attach_failed",
                    error);
                try
                {
                    if (nextLayer!=null && !nativeScreen.IsFinalized)
                    {
                        if(nextMovie!=null)
                            nextLayer.ReleaseMovie(nextMovie);
                        if(nativeScreen.HasLayer(nextLayer))
                            nativeScreen.RemoveLayer(nextLayer);
                    }
                }
                catch(Exception cleanup)
                {
                    DiagnosticJournal.Exception(
                        "cu_native_culture_cleanup_failed",cleanup);
                }
                RestoreNativeCultureFocus(nativeScreen);
            }
        }

        private static void RequestAbandon()
        {
            // Never dispose Gauntlet's movie inside its own click callback.
            // Fall back to the native panel on the following application tick.
            abandonRequested=true;
        }

        private static void RestoreNativeCultureFocus(
            CharacterCreationScreen nativeScreen)
        {
            try
            {
                var manager=Game.Current==null||
                    Game.Current.GameStateManager==null ? null :
                    Game.Current.GameStateManager.ActiveState as
                        CharacterCreationState;
                if(manager==null || manager.CharacterCreationManager==null||
                    manager.CharacterCreationManager.CurrentStage==null ||
                    manager.CharacterCreationManager.CurrentStage.GetType().Name
                        !="CharacterCreationCultureStage") return;

                foreach(var candidate in nativeScreen.Layers)
                {
                    if(candidate is GauntletLayer && candidate!=layer &&
                        candidate.InputRestrictions.Order<60)
                    {
                        candidate.IsFocusLayer=true;
                        ScreenManager.TrySetFocus(candidate);
                        return;
                    }
                }
            }
            catch(Exception ex)
            {
                DiagnosticJournal.Exception(
                    "cu_native_culture_focus_fallback",ex);
            }
        }

        private static void Detach()
        {
            var oldHost=host;
            var oldLayer=layer;
            var oldMovie=movie;
            host=null;
            layer=null;
            movie=null;
            vm=null;
            abandonRequested=false;
            if(oldHost==null || oldLayer==null) return;
            try
            {
                if(!oldHost.IsFinalized && oldHost.HasLayer(oldLayer))
                {
                    oldLayer.IsFocusLayer=false;
                    ScreenManager.TryLoseFocus(oldLayer);
                    if(oldMovie!=null)
                        oldLayer.ReleaseMovie(oldMovie);
                    oldHost.RemoveLayer(oldLayer);
                }
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception("cu_native_culture_detach",error);
            }
        }

        public static void Clear()
        {
            Detach();
            failedHost=null;
        }
    }

    /// <summary>
    /// Each entry delegates into its actual native CultureVM. No generated
    /// option table and no offline culture stat guesses.
    /// </summary>
    public sealed class CUNativeCultureChoiceVM : ViewModel
    {
        private readonly Action<string> onSelect;
        private readonly string cultureId;
        private readonly string name;
        private bool selected;

        internal CUNativeCultureChoiceVM(
            CUNativeCultureController.Option option,Action<string> onSelect)
        {
            this.onSelect=onSelect;
            cultureId=option.Id;
            name=option.Name;
        }

        public void ExecuteSelect()
        {
            onSelect(cultureId);
        }

        [DataSourceProperty]
        public string Name {get {return name;}}

        [DataSourceProperty]
        public string CultureId {get {return cultureId;}}

        [DataSourceProperty]
        public bool IsSelected
        {
            get{return selected;}
            set
            {
                if(selected==value) return;
                selected=value;
                OnPropertyChangedWithValue(value,"IsSelected");
            }
        }
    }

    /// <summary>
    /// A real, responsive CU culture choice form. Calls the SAME native
    /// CultureStageVM selection and on-next delegates as the game's UI.
    /// </summary>
    public sealed class CUNativeCultureChooserVM : ViewModel
    {
        private readonly CUNativeCultureController controller;
        private readonly Action abandon;
        private string name="Choose a culture";
        private string description="Select a native culture to see its effects.";
        private string status="These are real game culture choices.";
        private string previousSelection="";

        [DataSourceProperty]
        public MBBindingList<CUNativeCultureChoiceVM> Cultures
            { get; private set; }

        internal CUNativeCultureChooserVM(
            CUNativeCultureController controller,Action abandon)
        {
            this.controller=controller;
            this.abandon=abandon;
            Cultures=new MBBindingList<CUNativeCultureChoiceVM>();
            foreach(var option in controller.Options)
                Cultures.Add(new CUNativeCultureChoiceVM(option,Choose));
            Sync();
        }

        public void Sync()
        {
            string id=controller.SelectedId;
            if(id==previousSelection) return;
            previousSelection=id;
            foreach(var choice in Cultures)
                choice.IsSelected=choice.CultureId==id;
            foreach(var item in controller.Options)
            {
                if(item.Id==id)
                {
                    SetCurrent(item.Name,item.Description);
                    return;
                }
            }
            SetCurrent("Choose a culture",
                "Select a native culture. Its body seed and hero culture " +
                "change through Bannerlord's own controller.");
        }

        private void SetCurrent(string selectedName,string selectedDescription)
        {
            if(name!=selectedName)
            {
                name=selectedName;
                OnPropertyChangedWithValue(name,"SelectedName");
            }
            if(description!=selectedDescription)
            {
                description=selectedDescription;
                OnPropertyChangedWithValue(description,"SelectedDescription");
            }
        }

        private void SetStatus(string value)
        {
            if(status==value) return;
            status=value;
            OnPropertyChangedWithValue(status,"Status");
        }

        private void Choose(string id)
        {
            try
            {
                if(!controller.SelectCulture(id))
                {
                    SetStatus("This culture is not in the native game list.");
                    return;
                }
                Sync();
                SetStatus("Selected in Bannerlord's real culture controller.");
                DiagnosticJournal.Event("cu_native_culture_selected",
                    "native_vm_choice=true|id="+id);
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception("cu_native_culture_select_failed",
                    error);
                abandon();
            }
        }

        public void ExecuteContinue()
        {
            if(!controller.CanContinue)
            {
                SetStatus("Choose a culture before continuing.");
                return;
            }
            try
            {
                DiagnosticJournal.Event("cu_native_culture_confirmed",
                    "via=native_stage_vm|id="+controller.SelectedId);
                controller.Continue();
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception(
                    "cu_native_culture_confirm_failed",error);
                abandon();
            }
        }

        public void ExecuteBack()
        {
            try
            {
                controller.Previous();
            }
            catch(Exception error)
            {
                DiagnosticJournal.Exception("cu_native_culture_back_failed",
                    error);
                abandon();
            }
        }

        public void ExecuteUseNativeControls()
        {
            // Explicit recovery for every session, without losing choices.
            DiagnosticJournal.Event("cu_native_culture_return_to_native",
                "preserve_native_state=true");
            abandon();
        }

        [DataSourceProperty]
        public string Title
            {get{return "CALRADIA UNIFIED | STARTING LIFE";}}

        [DataSourceProperty]
        public string Subtitle
            {get{return "CHOOSE YOUR REAL CULTURE";}}

        [DataSourceProperty]
        public string SelectedName {get{return name;}}

        [DataSourceProperty]
        public string SelectedDescription {get{return description;}}

        [DataSourceProperty]
        public string Status {get{return status;}}

        [DataSourceProperty]
        public string Notice
        {
            get
            {
                return "Choices use Bannerlord's native culture controller. " +
                    "Appearance editing follows on the real 3D face page.";
            }
        }
    }
}

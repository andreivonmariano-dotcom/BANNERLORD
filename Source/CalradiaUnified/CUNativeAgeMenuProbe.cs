using System;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterCreationContent;

namespace CalradiaUnified
{
    /// <summary>
    /// One-time, read-only native age menu observation in accepted Sandbox.
    /// The native narrative option callbacks own age-based skill and trait
    /// rewards. Do not replace native 20/30/40/50 options on guesses.
    /// Exact option IDs and ordered menu identity from a live run are
    /// required to convert this menu into a true one-year slider.
    /// </summary>
    internal static class CUNativeAgeMenuProbe
    {
        private static CharacterCreationManager tracked;
        private static NarrativeMenu observedFinalMenu;

        public static void Tick(CharacterCreationManager manager)
        {
            if(manager==null)
            {
                Clear();
                return;
            }
            if(!Object.ReferenceEquals(manager,tracked))
            {
                tracked=manager;
                observedFinalMenu=null;
            }
            if(manager.CurrentStage==null ||
                manager.CurrentStage.GetType().Name!=
                    "CharacterCreationNarrativeStage")
                return;
            try
            {
                var current=manager.CurrentMenu;
                var menus=manager.NarrativeMenus;
                if(current==null || menus==null || menus.Count==0 ||
                    !Object.ReferenceEquals(current,menus[menus.Count-1]) ||
                    Object.ReferenceEquals(current,observedFinalMenu))
                    return;
                observedFinalMenu=current;
                var choices=manager.GetSuitableNarrativeMenuOptions();
                if(choices==null)return;
                var options=choices.Where(x=>x!=null).ToArray();
                var ages=options.Select(x=>x.Text==null ?
                        "" : x.Text.ToString().Trim()).ToArray();
                var numeric=ages.All(x=>
                    x=="20" || x=="30" || x=="40" || x=="50");
                var ids=String.Join("/",options.Select(x=>
                    (x.StringId??"none").Replace("|","_")).ToArray());
                DiagnosticJournal.Event("cu_native_final_narrative_menu",
                    "options="+options.Length+
                    "|numeric_age_candidates="+numeric+
                    "|labels="+String.Join("/",ages).Replace("|","_")+
                    "|ids="+ids+
                    "|stage=CharacterCreationNarrativeStage"+
                    "|game_callbacks_preserved=true");
            }
            catch(Exception e)
            {
                DiagnosticJournal.Exception(
                    "cu_native_final_narrative_menu_probe_failed",e);
            }
        }

        public static void Clear()
        {
            tracked=null;
            observedFinalMenu=null;
        }
    }
}

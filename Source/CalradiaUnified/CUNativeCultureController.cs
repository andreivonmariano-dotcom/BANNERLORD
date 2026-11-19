using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SandBox.View.CharacterCreation;

namespace CalradiaUnified
{
    /// <summary>
    /// Reflection bridge to the EXISTING native CharacterCreationCultureStageVM
    /// owned by CharacterCreationCultureStageView. Pinned to game
    /// v1.5.7.125802. Never creates another native culture VM, never writes
    /// SelectedCulture directly, and never bypasses native stage effects.
    ///
    /// Every reflection lookup must succeed before CU obscures native
    /// controls. Unsupported DLLs fall back to the original culture page.
    /// </summary>
    internal sealed class CUNativeCultureController
    {
        internal sealed class Option
        {
            internal readonly string Id;
            internal readonly string Name;
            internal readonly string Description;
            internal readonly object NativeVM;
            internal readonly MethodInfo Select;

            internal Option(string id, string name, string description,
                object nativeVM, MethodInfo select)
            {
                Id=id; Name=name; Description=description;
                NativeVM=nativeVM; Select=select;
            }
        }

        private readonly object nativeVM;
        private readonly PropertyInfo currentSelection;
        private readonly MethodInfo advance;
        private readonly MethodInfo back;
        internal readonly List<Option> Options;

        private CUNativeCultureController(object nativeVM,
            PropertyInfo currentSelection, MethodInfo advance,
            MethodInfo back, List<Option> options)
        {
            this.nativeVM=nativeVM;
            this.currentSelection=currentSelection;
            this.advance=advance;
            this.back=back;
            Options=options;
        }

        private static PropertyInfo PublicProperty(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Instance |
                BindingFlags.Public);
        }

        public static bool TryBind(CharacterCreationScreen screen,
            out CUNativeCultureController controller, out string failure)
        {
            controller=null;
            failure="";
            try
            {
                if (screen==null)
                {
                    failure="native_screen_missing"; return false;
                }
                var stageField=typeof(CharacterCreationScreen).GetField(
                    "_currentStageView",BindingFlags.NonPublic |
                    BindingFlags.Instance);
                if (stageField==null)
                {
                    failure="native_stage_view_field_missing"; return false;
                }
                object stageView=stageField.GetValue(screen);
                if (stageView==null ||
                    stageView.GetType().Name!="CharacterCreationCultureStageView")
                {
                    failure="native_culture_stage_view_missing"; return false;
                }
                var sourceField=stageView.GetType().GetField("_dataSource",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                object source=sourceField==null ? null :
                    sourceField.GetValue(stageView);
                if (source==null ||
                    source.GetType().Name!="CharacterCreationCultureStageVM")
                {
                    failure="native_culture_vm_missing"; return false;
                }
                Type vmType=source.GetType();
                var choices=PublicProperty(vmType,"Cultures");
                var selected=PublicProperty(vmType,"CurrentSelectedCulture");
                var next=vmType.GetMethod("OnNextStage",
                    BindingFlags.Public|BindingFlags.Instance);
                var previous=vmType.GetMethod("OnPreviousStage",
                    BindingFlags.Public|BindingFlags.Instance);
                if (choices==null || selected==null || next==null ||
                    previous==null)
                {
                    failure="native_culture_methods_missing"; return false;
                }
                var list=choices.GetValue(source,null) as IEnumerable;
                if (list==null)
                {
                    failure="native_culture_list_missing"; return false;
                }
                var nativeOptions=new List<Option>();
                var ids=new HashSet<string>(StringComparer.Ordinal);
                foreach (object item in list)
                {
                    if(item==null) continue;
                    Type itemType=item.GetType();
                    var idProp=PublicProperty(itemType,"CultureID");
                    var nameProp=PublicProperty(itemType,"NameText");
                    var descriptionProp=PublicProperty(itemType,
                        "DescriptionText");
                    var selectMethod=itemType.GetMethod("ExecuteSelectCulture",
                        BindingFlags.Instance|BindingFlags.Public);
                    if (idProp==null || nameProp==null ||
                        descriptionProp==null || selectMethod==null)
                    {
                        failure="native_culture_item_incomplete";
                        return false;
                    }
                    string id=idProp.GetValue(item,null) as string;
                    string name=nameProp.GetValue(item,null) as string;
                    string desc=descriptionProp.GetValue(item,null) as string;
                    if (String.IsNullOrEmpty(id) ||
                        String.IsNullOrEmpty(name) || !ids.Add(id))
                    {
                        failure="native_culture_item_duplicate_or_blank";
                        return false;
                    }
                    nativeOptions.Add(new Option(id,name,desc??"",item,
                        selectMethod));
                }
                if (nativeOptions.Count<2)
                {
                    failure="native_culture_list_too_short"; return false;
                }
                controller=new CUNativeCultureController(source,selected,
                    next,previous,nativeOptions);
                return true;
            }
            catch(Exception ex)
            {
                failure="native_culture_reflection:"+ex.GetType().Name;
                return false;
            }
        }

        public string SelectedId
        {
            get
            {
                var selected=currentSelection.GetValue(nativeVM,null);
                if(selected==null) return "";
                var property=PublicProperty(selected.GetType(),"CultureID");
                return property==null?"":(property.GetValue(selected,null)
                    as string ?? "");
            }
        }

        public bool SelectCulture(string id)
        {
            foreach(var option in Options)
            {
                if(option.Id==id)
                {
                    // Calls native CharacterCreationCultureVM.ExecuteSelectCulture,
                    // which runs the native stage's OnCultureSelection path.
                    // This changes the real Hero culture and body-property seed.
                    option.Select.Invoke(option.NativeVM,null);
                    return true;
                }
            }
            return false;
        }

        public bool CanContinue {get {return SelectedId.Length>0;}}

        public void Continue()
        {
            if(!CanContinue) return;
            // Native CharacterCreationCultureStageVM.OnNextStage commits
            // SelectedCulture, resets narrative choices, then calls the
            // original CharacterCreationCultureStageView.NextStage delegate
            // to set the player name and advance the native manager.
            advance.Invoke(nativeVM,null);
        }

        public void Previous()
        {
            // Native CultureStageView.PreviousStage returns to the menu.
            back.Invoke(nativeVM,null);
        }
    }
}

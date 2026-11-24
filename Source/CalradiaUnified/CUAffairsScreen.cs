using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    // One campaign-wide read-only journal. No game-menu transition.
    public sealed class CUAffairsScreen : ScreenBase
    {
        private readonly List<SandboxAffair> records;
        private GauntletLayer layer;
        private CUAffairsVM data;
        public CUAffairsScreen(List<SandboxAffair> items){records=items;}
        protected override void OnInitialize()
        {
            base.OnInitialize();
            data=new CUAffairsVM(records,CloseSafely);
            layer=new GauntletLayer("CUAffairs",130,false){IsFocusLayer=true};
            AddLayer(layer);
            layer.InputRestrictions.SetInputRestrictions();
            layer.LoadMovie("CUAffairs",data);
            DiagnosticJournal.Event("cu_campaign_affairs_open","records="+records.Count+"|read_only=true");
        }
        protected override void OnActivate()
        {
            base.OnActivate();
            if(layer!=null){layer.IsFocusLayer=true;ScreenManager.TrySetFocus(layer);}
        }
        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            if(ScreenManager.TopScreen==this && Input.IsKeyReleased(InputKey.Escape))CloseSafely();
        }
        private void CloseSafely()
        {
            if(ScreenManager.TopScreen==this)ScreenManager.PopScreen();
        }
        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            if(layer!=null){layer.IsFocusLayer=false;ScreenManager.TryLoseFocus(layer);}
        }
        protected override void OnFinalize()
        {
            base.OnFinalize();
            if(layer!=null)RemoveLayer(layer);
            layer=null;data=null;
        }
    }
    public sealed class CUAffairRowVM : ViewModel
    {
        private readonly Action<SandboxAffair> open;
        private readonly SandboxAffair matter;
        public CUAffairRowVM(SandboxAffair item,Action<SandboxAffair> action)
        {matter=item;open=action;Title=item.Title;Status=item.Status;}
        [DataSourceProperty] public string Title{get;private set;}
        [DataSourceProperty] public string Status{get;private set;}
        public void ExecuteOpen(){if(open!=null)open(matter);}
    }
    public sealed class CUAffairsVM : ViewModel
    {
        private readonly List<SandboxAffair> records;
        private readonly Action close;
        private string tab="Pending";
        private int page;
        [DataSourceProperty] public MBBindingList<CUAffairRowVM> Rows{get;private set;}
        [DataSourceProperty] public string Heading{get;private set;}
        [DataSourceProperty] public string PageLabel{get;private set;}
        [DataSourceProperty] public string DetailTitle{get;private set;}
        [DataSourceProperty] public string DetailBody{get;private set;}
        public CUAffairsVM(List<SandboxAffair> items,Action closeAction)
        {
            records=items??new List<SandboxAffair>();close=closeAction;
            Rows=new MBBindingList<CUAffairRowVM>();RefreshPage();
        }
        private void RefreshPage()
        {
            var selected=SandboxAffairsIndexPolicy.Select(records,tab);
            int pages=SandboxAffairsIndexPolicy.PageCount(selected.Length);
            page=Math.Max(0,Math.Min(page,pages-1));
            Rows.Clear();
            foreach(var item in selected.Skip(page*SandboxAffairsIndexPolicy.PageSize)
                .Take(SandboxAffairsIndexPolicy.PageSize))
                Rows.Add(new CUAffairRowVM(item,Open));
            Heading=tab+" ("+selected.Length+")";
            PageLabel=(page+1)+" / "+pages;
            OnPropertyChangedWithValue(Heading,"Heading");
            OnPropertyChangedWithValue(PageLabel,"PageLabel");
            Open(null);
        }
        private void Open(SandboxAffair item)
        {
            DetailTitle=item==null?"Select a matter":item.Title;
            DetailBody=item==null?"Choose an entry to read its details. Speak with the named person to make a decision.":
                item.Status+"\n\n"+item.Detail;
            OnPropertyChangedWithValue(DetailTitle,"DetailTitle");
            OnPropertyChangedWithValue(DetailBody,"DetailBody");
        }
        private void Select(string name){tab=name;page=0;RefreshPage();}
        public void ExecutePending(){Select("Pending");}
        public void ExecuteLetters(){Select("Letters");}
        public void ExecuteService(){Select("Service");}
        public void ExecuteHousehold(){Select("Household");}
        public void ExecuteHistory(){Select("History");}
        public void ExecutePrevious(){if(page>0){page--;RefreshPage();}}
        public void ExecuteNext(){if(page+1<SandboxAffairsIndexPolicy.PageCount(
            SandboxAffairsIndexPolicy.Select(records,tab).Length)){page++;RefreshPage();}}
        public void ExecuteClose(){if(close!=null)close();}
    }
}

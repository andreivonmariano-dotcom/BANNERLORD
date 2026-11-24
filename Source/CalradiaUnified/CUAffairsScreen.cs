using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using SandBox.View.Map;
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
            data=new CUAffairsVM(records,CloseSafely,OpenEncyclopedia,ShowOnMap);
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
        private void OpenEncyclopedia(SandboxAffair matter)
        {
            if(matter==null || ScreenManager.TopScreen!=this || Campaign.Current==null)return;
            var hero=SandboxOriginBehavior.ResolveAffairHero(matter.HeroId);
            var place=SandboxOriginBehavior.ResolveAffairPlace(matter);
            if(hero==null && place==null)return;
            try
            {
                CloseSafely();
                Campaign.Current.EncyclopediaManager.GoToLink(hero!=null?"Hero":"Settlement",hero!=null?hero.StringId:place.StringId);
            }
            catch(Exception error){DiagnosticJournal.Exception("cu_affairs_encyclopedia_failed",error);}
        }
        private void ShowOnMap(SandboxAffair matter)
        {
            var place=SandboxOriginBehavior.ResolveAffairPlace(matter);
            if(place==null || ScreenManager.TopScreen!=this)return;
            try
            {
                CloseSafely();
                var map=ScreenManager.TopScreen as MapScreen;
                if(map!=null)map.FastMoveCameraToPosition(place.GatePosition);
                else if(Campaign.Current!=null)
                    Campaign.Current.EncyclopediaManager.GoToLink("Settlement",place.StringId);
            }
            catch(Exception error){DiagnosticJournal.Exception("cu_affairs_map_location_failed",error);}
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
        private readonly Action<SandboxAffair> encyclopedia,map;
        private SandboxAffair current;
        [DataSourceProperty] public CharacterImageIdentifierVM Portrait{get;private set;}
        [DataSourceProperty] public bool HasPortrait{get;private set;}
        [DataSourceProperty] public BannerImageIdentifierVM SenderBanner{get;private set;}
        [DataSourceProperty] public bool HasSenderBanner{get;private set;}
        [DataSourceProperty] public string LetterSender{get;private set;}
        [DataSourceProperty] public string LetterDate{get;private set;}
        [DataSourceProperty] public bool HasEncyclopedia{get;private set;}
        [DataSourceProperty] public bool HasLocation{get;private set;}
        private string tab="Pending";
        private int page;
        [DataSourceProperty] public MBBindingList<CUAffairRowVM> Rows{get;private set;}
        [DataSourceProperty] public string Heading{get;private set;}
        [DataSourceProperty] public string PageLabel{get;private set;}
        [DataSourceProperty] public string DetailTitle{get;private set;}
        [DataSourceProperty] public string DetailBody{get;private set;}
        public CUAffairsVM(List<SandboxAffair> items,Action closeAction,Action<SandboxAffair> openEncyclopedia,Action<SandboxAffair> showOnMap)
        {
            records=items??new List<SandboxAffair>();close=closeAction;encyclopedia=openEncyclopedia;map=showOnMap;
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
            Open(selected.Skip(page*SandboxAffairsIndexPolicy.PageSize).FirstOrDefault());
        }
        private void Open(SandboxAffair item)
        {
            current=item;
            var hero=item==null?null:SandboxOriginBehavior.ResolveAffairHero(item.HeroId);
            var place=SandboxOriginBehavior.ResolveAffairPlace(item);
            HasPortrait=hero!=null && hero.CharacterObject!=null;
            try{Portrait=new CharacterImageIdentifierVM(HasPortrait?CharacterCode.CreateFrom(hero.CharacterObject):null);}
            catch{HasPortrait=false;Portrait=new CharacterImageIdentifierVM(null);}
            var senderClan=hero==null?null:hero.Clan;
            HasSenderBanner=senderClan!=null && senderClan.Banner!=null;
            try{SenderBanner=new BannerImageIdentifierVM(HasSenderBanner?
                BannerCode.CreateFrom(senderClan.Banner):null);}
            catch(Exception error){HasSenderBanner=false;SenderBanner=null;
                DiagnosticJournal.Exception("cu_affairs_banner_unavailable",error);}
            LetterSender=hero==null?(item==null?"":"Campaign record"):
                hero.Name.ToString()+(senderClan==null?"":" | "+senderClan.Name);
            LetterDate=item==null || item.Day<0?"":"Recorded day "+item.Day;
            OnPropertyChangedWithValue(SenderBanner,"SenderBanner");
            OnPropertyChangedWithValue(HasSenderBanner,"HasSenderBanner");
            OnPropertyChangedWithValue(LetterSender,"LetterSender");
            OnPropertyChangedWithValue(LetterDate,"LetterDate");
            HasEncyclopedia=hero!=null || place!=null;
            HasLocation=place!=null;
            OnPropertyChangedWithValue(Portrait,"Portrait");
            OnPropertyChangedWithValue(HasPortrait,"HasPortrait");
            OnPropertyChangedWithValue(HasEncyclopedia,"HasEncyclopedia");
            OnPropertyChangedWithValue(HasLocation,"HasLocation");
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
        public void ExecuteEncyclopedia(){if(HasEncyclopedia && encyclopedia!=null)encyclopedia(current);}
        public void ExecuteLocation(){if(HasLocation && map!=null)map(current);}
        public void ExecuteClose(){if(close!=null)close();}
    }
}

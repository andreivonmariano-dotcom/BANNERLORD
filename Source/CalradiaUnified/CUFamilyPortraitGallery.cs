using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace CalradiaUnified
{
    /// <summary>
    /// Read-only native-Hero portrait gallery, intentionally usable only
    /// after real spouse and children exist. Never spawns a preview Hero,
    /// changes the infant's state, or mutates campaign relationships.
    /// </summary>
    public sealed class CUFamilyPortraitScreen : ScreenBase
    {
        private GauntletLayer layer;
        private CUFamilyPortraitsVM data;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            data = new CUFamilyPortraitsVM(() => CloseSafely());
            layer = new GauntletLayer("CUFamilyPortraits", 130, false)
            {
                IsFocusLayer = true
            };
            AddLayer(layer);
            layer.InputRestrictions.SetInputRestrictions();
            layer.LoadMovie("CUFamilyPortraits",data);
            DiagnosticJournal.Event("cu_family_portraits_open",
                "actual_hero_portraits=" + data.Members.Count +
                "|read_only=true|children_kept_underage=true");
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (layer != null) ScreenManager.TrySetFocus(layer);
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            if (ScreenManager.TopScreen == this &&
                Input.IsKeyReleased(InputKey.Escape))
                CloseSafely();
        }

        private void CloseSafely()
        {
            if (ScreenManager.TopScreen == this) ScreenManager.PopScreen();
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            if (layer != null)
            {
                layer.IsFocusLayer = false;
                ScreenManager.TryLoseFocus(layer);
            }
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();
            if (layer != null) RemoveLayer(layer);
            layer = null;
            data = null;
        }
    }

    public sealed class CUFamilyPortraitsVM : ViewModel
    {
        private readonly Action close;

        [DataSourceProperty]
        public MBBindingList<CUFamilyPortraitVM> Members { get; private set; }

        public CUFamilyPortraitsVM(Action closeAction)
        {
            close = closeAction;
            Members = new MBBindingList<CUFamilyPortraitVM>();
            var player = Hero.MainHero;
            if (player == null) return;
            Add(player,"YOUR CHARACTER");
            if (player.Spouse != null && player.Spouse.IsAlive &&
                player.Spouse.Spouse == player)
                Add(player.Spouse,"SPOUSE");
            if (player.Children != null)
            {
                foreach (var child in player.Children
                    .Where(h => h != null && h.IsAlive &&
                        (h.Mother == player || h.Father == player))
                    .OrderBy(h => h.Age)
                    .Take(SandboxOriginProfile.MaxChildren))
                    Add(child,"CHILD");
            }
        }

        private void Add(Hero hero,string relationship)
        {
            Members.Add(new CUFamilyPortraitVM(hero,relationship));
        }

        [DataSourceProperty]
        public string Heading { get { return "HOUSE OF CALRADIA"; } }

        [DataSourceProperty]
        public string Description
        {
            get
            {
                return "Every son and daughter is shown here. Young " +
                    "children have a recorded family home, but " +
                    "Bannerlord does not spawn or recall them as adult " +
                    "party heroes until they come of age.";
            }
        }

        [DataSourceProperty]
        public string MemberCount
        {
            get { return Members.Count + " FAMILY MEMBERS"; }
        }

        public void ExecuteClose()
        {
            if (close != null) close();
        }
    }

    public sealed class CUFamilyPortraitVM : ViewModel
    {
        private readonly CharacterImageIdentifierVM portrait;

        public CUFamilyPortraitVM(Hero hero,string relationship)
        {
            Relationship = relationship;
            Name = hero.Name.ToString();
            Age = hero.IsChild ? "Age " + (int)hero.Age +
                "  |  underage" : "Age " + (int)hero.Age;
            var residence = hero.StayingInSettlement ??
                hero.LastKnownClosestSettlement;
            Home = residence == null ? "Home: not recorded" :
                "Home: " + residence.Name.ToString();
            bool atHome=hero.StayingInSettlement!=null &&
                hero.StayingInSettlement==residence;
            bool scenePresent=residence!=null &&
                residence.HeroesWithoutParty.Contains(hero);
            string category=SandboxFamilyResidencePolicy.Classify(
                hero.IsChild,atHome,scenePresent);
            Presence=relationship=="CHILD" ?
                SandboxFamilyResidencePolicy.HumanStatus(category) :
                (hero.IsChild?"Underage; no party recall":
                    (scenePresent?"At settlement":"Location recorded"));
            if(relationship=="CHILD")
                DiagnosticJournal.Event("cu_family_gallery_child",
                    "id="+hero.StringId+
                    "|gender="+(hero.IsFemale?"Female":"Male")+
                    "|age="+(int)hero.Age+
                    "|state="+hero.HeroState+
                    "|residence="+(residence==null?"none":
                        residence.StringId)+
                    "|scene_present="+scenePresent+
                    "|status="+category);
            Parentage = relationship == "CHILD" ?
                "Mother: " + (hero.Mother == null ? "unknown" :
                    hero.Mother.Name.ToString()) + " | Father: " +
                (hero.Father == null ? "unknown" :
                    hero.Father.Name.ToString()) :
                (relationship == "SPOUSE" ?
                    "Partner: " + Hero.MainHero.Name.ToString() :
                    "Leader of your household");
            // Source from the registered native Hero's own CharacterObject.
            // An invalid infant image is a blank frame, not a new Hero.
            CharacterImageIdentifierVM image;
            try
            {
                image = new CharacterImageIdentifierVM(
                    CharacterCode.CreateFrom(hero.CharacterObject));
            }
            catch (Exception exception)
            {
                DiagnosticJournal.Exception("cu_family_portrait_unavailable",
                    exception);
                image = new CharacterImageIdentifierVM(null);
            }
            portrait = image;
        }

        [DataSourceProperty] public CharacterImageIdentifierVM Portrait
            { get { return portrait; } }
        [DataSourceProperty] public string Name { get; private set; }
        [DataSourceProperty] public string Relationship { get; private set; }
        [DataSourceProperty] public string Age { get; private set; }
        [DataSourceProperty] public string Home { get; private set; }
        [DataSourceProperty] public string Parentage { get; private set; }
        [DataSourceProperty] public string Presence { get; private set; }
    }
}

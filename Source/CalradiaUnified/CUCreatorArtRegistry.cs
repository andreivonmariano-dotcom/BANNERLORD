using System;
using TaleWorlds.Engine.GauntletUI;

namespace CalradiaUnified
{
    /// <summary>
    /// Optional custom sprite atlas. The pinned game needs TaleWorlds'
    /// sprite-sheet generator and Modding Kit import before raw PNGs are
    /// usable in Gauntlet XML. Never load a custom-art movie until the
    /// category is registered. Keep the installed-game fallback intact.
    /// </summary>
    internal static class CUCreatorArtRegistry
    {
        private const string Category="ui_calradiaunified";
        private static bool attempted;
        private static bool loaded;

        public static bool IsAvailable()
        {
            if(attempted)return loaded;
            attempted=true;
            try
            {
                var data=UIResourceManager.SpriteData;
                if(data==null || data.SpriteCategories==null)
                    return false;
                var categories=data.SpriteCategories;
                if(!categories.ContainsKey(Category))
                {
                    DiagnosticJournal.Event("cu_v059_art_unavailable",
                        "reason=category_not_imported|fallback=native");
                    return false;
                }
                var category=categories[Category];
                category.Load(UIResourceManager.ResourceContext,
                    UIResourceManager.UIResourceDepot);
                loaded=true;
                DiagnosticJournal.Event("cu_v059_art_ready",
                    "category="+Category+"|imported=true");
            }
            catch(Exception error)
            {
                loaded=false;
                DiagnosticJournal.Exception(
                    "cu_v059_art_category_load_failed",error);
            }
            return loaded;
        }
    }
}

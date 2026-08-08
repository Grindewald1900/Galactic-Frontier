using Assets.Resources.Scripts.Main;
using UnityEngine;
using static Assets.Resources.Scripts.Main.GameStatusManager;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Temporary bridge to MainScene prefab panels for screens not yet rewritten.
    /// Phase 2+ replaces each mapping with a first-class screen.
    /// </summary>
    internal static class LegacyPanelAdapter
    {
        public static bool TryShow(AppScreen screen, MainScrollController controller)
        {
            if (controller == null || !TryMap(screen, out CurrentScene scene))
                return false;

            controller.ShowPanel(scene);
            return true;
        }

        public static void Hide(MainScrollController controller)
        {
            controller?.HideAllPanels();
        }

        public static void FitToContentArea(MainScrollController controller, float navWidth)
        {
            if (controller?.panels == null)
                return;

            foreach (GameObject panel in controller.panels)
            {
                if (panel == null || panel.transform is not RectTransform rect)
                    continue;

                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(navWidth + 12f, NexusTheme.StatusBarHeight + 8f);
                rect.offsetMax = new Vector2(-12f, -(NexusTheme.TopBarHeight + NexusTheme.BreadcrumbHeight + 8f));
            }
        }

        private static bool TryMap(AppScreen screen, out CurrentScene scene)
        {
            switch (screen)
            {
                case AppScreen.Characters:
                    scene = CurrentScene.CHARACTER_MENU;
                    return true;
                case AppScreen.Cards:
                    scene = CurrentScene.CARDS_MENU;
                    return true;
                case AppScreen.Inventory:
                    scene = CurrentScene.INVENTORY_MENU;
                    return true;
                case AppScreen.Crafting:
                    scene = CurrentScene.BUILDING_MENU;
                    return true;
                case AppScreen.Market:
                    scene = CurrentScene.SHOP_MENU;
                    return true;
                default:
                    scene = CurrentScene.MAIN_SCENE;
                    return false;
            }
        }
    }
}

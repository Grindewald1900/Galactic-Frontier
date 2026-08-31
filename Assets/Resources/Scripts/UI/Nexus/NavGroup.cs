using System.Collections.Generic;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>P6 five-group navigation. Maps to existing <see cref="AppScreen"/> values.</summary>
    public enum NavGroup
    {
        Bridge,
        StarMap,
        Fleet,
        Industry,
        Starport
    }

    internal static class NavGroupRules
    {
        public static NavGroup GroupOf(AppScreen screen) => screen switch
        {
            AppScreen.Bridge => NavGroup.Bridge,
            AppScreen.Battle => NavGroup.StarMap,
            AppScreen.Formation or AppScreen.Ship or AppScreen.Characters or AppScreen.Cards => NavGroup.Fleet,
            AppScreen.Crafting or AppScreen.Inventory => NavGroup.Industry,
            AppScreen.Market or AppScreen.Recruit => NavGroup.Starport,
            _ => NavGroup.Bridge
        };

        public static AppScreen DefaultScreen(NavGroup group) => group switch
        {
            NavGroup.Bridge => AppScreen.Bridge,
            NavGroup.StarMap => AppScreen.Battle,
            NavGroup.Fleet => AppScreen.Formation,
            NavGroup.Industry => AppScreen.Crafting,
            NavGroup.Starport => AppScreen.Market,
            _ => AppScreen.Bridge
        };

        public static IReadOnlyList<AppScreen> SubScreens(NavGroup group) => group switch
        {
            NavGroup.Fleet => new[] { AppScreen.Formation, AppScreen.Ship, AppScreen.Characters, AppScreen.Cards },
            NavGroup.Industry => new[] { AppScreen.Crafting, AppScreen.Inventory },
            NavGroup.Starport => new[] { AppScreen.Market, AppScreen.Recruit },
            _ => System.Array.Empty<AppScreen>()
        };

        public static bool HasSubNav(NavGroup group) => SubScreens(group).Count > 0;
    }
}

using Assets.Resources.Scripts.Onboarding;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Soft onboarding CTA strip for target screens (systems/10 §4.4).</summary>
    internal static class OnboardingBanner
    {
        public static void TryDraw(Transform parent, AppScreen currentScreen, Vector2 position)
        {
            if (OnboardingService.IsChainComplete)
                return;

            var step = OnboardingService.ActiveStep;
            if (step == null)
                return;

            if (!Matches(step.targetScreen, currentScreen))
                return;

            var title = UiText.T(step.titleEn, step.titleZh);
            NexusUiFactory.CreateText(
                parent,
                "OnboardBanner",
                UiText.MissionsStepBanner(title) + " — " + UiText.T(step.hintEn, step.hintZh),
                position,
                new Vector2(1100f, 28f),
                12f,
                NexusTheme.Gold);
        }

        private static bool Matches(string targetScreen, AppScreen current) =>
            targetScreen switch
            {
                "Formation" => current == AppScreen.Formation,
                "Battle" => current == AppScreen.Battle,
                "Crafting" => current == AppScreen.Crafting,
                "Market" => current == AppScreen.Market,
                "Ship" => current == AppScreen.Ship,
                "Inventory" => current == AppScreen.Inventory,
                _ => false
            };
    }
}

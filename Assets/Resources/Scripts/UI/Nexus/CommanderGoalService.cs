using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Bridge main goal + bottleneck for P6 commander loop.</summary>
    internal static class CommanderGoalService
    {
        public struct GoalView
        {
            public string Title;
            public string Progress;
            public string Bottleneck;
            public string RecommendedAction;
            public AppScreen RecommendedScreen;
            public System.Action OnRecommended;
        }

        public static GoalView GetCurrent()
        {
            OnboardingService.EnsureLoaded(DataUtil.Instance);
            WorldService.EnsureLoaded(DataUtil.Instance);
            DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance?.cardEntities);

            if (!OnboardingService.IsChainComplete)
            {
                foreach (var view in OnboardingService.GetStepViews())
                {
                    if (view?.Def == null || view.Status != OnboardingStepStatus.Active)
                        continue;
                    return new GoalView
                    {
                        Title = UiText.T(view.Def.titleEn, view.Def.titleZh),
                        Progress = UiText.CommanderGoalOnboarding,
                        Bottleneck = UiText.T(view.Def.hintEn ?? "", view.Def.hintZh ?? ""),
                        RecommendedAction = UiText.QuestGo,
                        RecommendedScreen = MapTarget(view.Def.targetScreen),
                    };
                }
            }

            int gridPct = WorldService.State?.explorationProgress ?? 0;
            return new GoalView
            {
                Title = UiText.QuestNextGoalTitle,
                Progress = UiText.ExploreGridProgress(gridPct) + " · " + UiText.CommanderGoalPostOnboarding,
                Bottleneck = UiText.CommanderGoalScanHint,
                RecommendedAction = UiText.ExploreEnterSector,
                RecommendedScreen = AppScreen.Battle,
            };
        }

        private static AppScreen MapTarget(string targetScreen) => targetScreen switch
        {
            "Formation" => AppScreen.Formation,
            "Battle" => AppScreen.Battle,
            "Crafting" => AppScreen.Crafting,
            "Market" => AppScreen.Market,
            "Ship" => AppScreen.Ship,
            _ => AppScreen.Bridge
        };
    }
}

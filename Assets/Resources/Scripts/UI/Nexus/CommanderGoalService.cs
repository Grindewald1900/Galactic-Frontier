using System.Text;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

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
            public string RewardsLine;
            public string PowerLine;
            public int RecommendedPower;
        }

        public static GoalView GetCurrent()
        {
            OnboardingService.EnsureLoaded(DataUtil.Instance);
            ChapterQuestService.EnsureLoaded(DataUtil.Instance);
            WorldService.EnsureLoaded(DataUtil.Instance);
            DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance?.cardEntities);

            if (!ChapterQuestService.IsChapterComplete)
            {
                foreach (var view in ChapterQuestService.GetStepViews())
                {
                    if (view?.Def == null || view.Status != ChapterQuestStepStatus.Active)
                        continue;
                    int rec = RecommendedPowerForStep(view.Def.stepId);
                    return new GoalView
                    {
                        Title = UiText.T(view.Def.titleEn, view.Def.titleZh),
                        Progress = UiText.CommanderGoalChapter,
                        Bottleneck = UiText.T(view.Def.hintEn ?? "", view.Def.hintZh ?? ""),
                        RecommendedAction = UiText.QuestGo,
                        RecommendedScreen = MapTarget(view.Def.targetScreen),
                        RewardsLine = FormatRewards(view.Def.rewards),
                        RecommendedPower = rec,
                        PowerLine = FormatPowerLine(rec),
                    };
                }
            }

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
            var lastRegion = PlayerPrefs.GetString("nexus_last_region_id", "");
            int exploreRec = 0;
            if (!string.IsNullOrEmpty(lastRegion))
                exploreRec = WorldService.GetRegionView(lastRegion)?.Config?.recommendedPower ?? 0;
            return new GoalView
            {
                Title = UiText.QuestNextGoalTitle,
                Progress = UiText.ExploreGridProgress(gridPct) + " · " + UiText.CommanderGoalPostOnboarding,
                Bottleneck = UiText.CommanderGoalScanHint,
                RecommendedAction = UiText.ExploreEnterSector,
                RecommendedScreen = AppScreen.Battle,
                RecommendedPower = exploreRec,
                PowerLine = FormatPowerLine(exploreRec),
            };
        }

        private static string FormatPowerLine(int recommended)
        {
            if (recommended <= 0) return "";
            return UiText.BridgeQuestPower(NexusProgressUi.ActiveCombatPower(), recommended);
        }

        private static int RecommendedPowerForStep(string stepId)
        {
            string regionId = stepId switch
            {
                ChapterQuestCatalog.StepOuterCleanup => WorldConstants.OuterBeltId,
                ChapterQuestCatalog.StepMiningSpur => WorldConstants.MiningSpurId,
                _ => ""
            };
            if (string.IsNullOrEmpty(regionId)) return 0;
            return WorldService.GetRegionView(regionId)?.Config?.recommendedPower ?? 0;
        }

        private static string FormatRewards(ChapterQuestRewardGrant[] rewards)
        {
            if (rewards == null || rewards.Length == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < rewards.Length; i++)
            {
                var r = rewards[i];
                if (r == null) continue;
                if (sb.Length > 0) sb.Append(" · ");
                if (r.kind == "credits" || r.credits > 0)
                    sb.Append($"₵{r.credits}");
                else if (r.kind == "item" && !string.IsNullOrEmpty(r.itemDefId))
                {
                    var def = ItemCatalog.Get(r.itemDefId);
                    string name = def != null
                        ? UiText.T(def.displayNameEn, def.displayNameZh)
                        : r.itemDefId;
                    sb.Append($"×{Mathf.Max(1, r.quantity)} {name}");
                }
            }

            return sb.Length == 0 ? "" : UiText.BridgeQuestRewards(sb.ToString());
        }

        private static AppScreen MapTarget(string targetScreen) => targetScreen switch
        {
            "Formation" => AppScreen.Formation,
            "Battle" => AppScreen.Battle,
            "Recruit" => AppScreen.Recruit,
            "Crafting" => AppScreen.Crafting,
            "Market" => AppScreen.Market,
            "Ship" => AppScreen.Ship,
            _ => AppScreen.Bridge
        };
    }
}

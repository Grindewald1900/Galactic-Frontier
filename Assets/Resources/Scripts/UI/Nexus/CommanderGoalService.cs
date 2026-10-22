using System.Collections.Generic;
using System.Text;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Quest;
using Assets.Resources.Scripts.Quest.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Bridge main goal + bottleneck for P6 commander loop (doc 25 layers).</summary>
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
            public string NextStepLine;
            public string PendingClaimTitle;
            public System.Action OnPendingClaim;
        }

        public struct StoryHeadline
        {
            public string Title;
            public string QuestId;
        }

        public enum QuestBoardStatusFilter
        {
            All,
            InProgress,
            Ready,
            Claimed
        }

        private static readonly QuestType[] BoardTypes =
        {
            QuestType.Story,
            QuestType.Onboarding,
            QuestType.Expedition,
            QuestType.Ship,
            QuestType.Loop
        };

        public static StoryHeadline CurrentStoryHeadline()
        {
            QuestService.EnsureLoaded(DataUtil.Instance);
            QuestView ready = null;
            foreach (var view in QuestService.GetViews(QuestType.Story))
            {
                if (view?.Def == null) continue;
                if (view.Status == QuestStatus.Active)
                    return Headline(view);
                if (view.Status == QuestStatus.Ready && ready == null)
                    ready = view;
            }

            if (ready != null)
                return Headline(ready);
            return new StoryHeadline { Title = UiText.QuestNextGoalTitle, QuestId = "" };
        }

        /// <summary>
        /// Quests the bridge board may show. Locked steps, including linear successors
        /// whose prerequisite is not satisfied, are omitted.
        /// </summary>
        public static List<QuestView> ListBoard(QuestType? type, QuestBoardStatusFilter status)
        {
            QuestService.EnsureLoaded(DataUtil.Instance);
            var list = new List<QuestView>();
            if (type.HasValue)
                AppendBoard(list, type.Value, status);
            else
            {
                foreach (var boardType in BoardTypes)
                    AppendBoard(list, boardType, status);
            }

            return list;
        }

        private static void AppendBoard(List<QuestView> list, QuestType type, QuestBoardStatusFilter status)
        {
            foreach (var view in QuestService.GetViews(type))
            {
                if (view?.Def == null || view.Status == QuestStatus.Locked) continue;
                if (!MatchesBoard(view.Status, status)) continue;
                list.Add(view);
            }
        }

        private static bool MatchesBoard(QuestStatus status, QuestBoardStatusFilter filter) => filter switch
        {
            QuestBoardStatusFilter.InProgress => status == QuestStatus.Active,
            QuestBoardStatusFilter.Ready => status == QuestStatus.Ready,
            QuestBoardStatusFilter.Claimed => status == QuestStatus.Claimed,
            _ => true
        };

        private static StoryHeadline Headline(QuestView view) =>
            new StoryHeadline
            {
                Title = UiText.T(view.Def.titleEn, view.Def.titleZh),
                QuestId = view.Def.questId
            };

        public static GoalView GetCurrent()
        {
            QuestService.EnsureLoaded(DataUtil.Instance);
            WorldService.EnsureLoaded(DataUtil.Instance);
            DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance?.cardEntities);

            foreach (var view in QuestService.GetTrackerViews())
            {
                if (view?.Def == null) continue;
                if (view.Status != QuestStatus.Active && view.Status != QuestStatus.Ready)
                    continue;
                int rec = RecommendedPower(view.Def);
                string progress = view.Def.type switch
                {
                    QuestType.Story => UiText.CommanderGoalChapter,
                    QuestType.Onboarding => UiText.CommanderGoalOnboarding,
                    QuestType.Expedition => UiText.QuestExpeditionTitle,
                    QuestType.Ship => UiText.QuestShipTitle,
                    QuestType.Loop => view.LoopTarget > 0
                        ? UiText.QuestLoopProgress(view.LoopCount, view.LoopTarget)
                        : UiText.QuestLoopTitle,
                    _ => UiText.QuestStepActive
                };
                var pending = view.Status == QuestStatus.Active
                    ? QuestService.LatestClaimable(view.Def.questId)
                    : null;
                string pendingTitle = pending?.Def == null
                    ? ""
                    : UiText.T(pending.Def.titleEn, pending.Def.titleZh);
                string pendingId = pending?.Def?.questId;
                return new GoalView
                {
                    Title = UiText.T(view.Def.titleEn, view.Def.titleZh),
                    Progress = progress,
                    Bottleneck = QuestService.LocalizedHint(view.Def),
                    NextStepLine = view.Status == QuestStatus.Active
                        ? QuestService.LocalizedHint(view.Def)
                        : "",
                    RecommendedAction = view.CanClaim ? UiText.QuestClaim : UiText.QuestGo,
                    RecommendedScreen = MapTarget(QuestService.TargetScreen(view.Def)),
                    OnRecommended = view.CanClaim
                        ? () => QuestService.TryClaim(view.Def.questId)
                        : view.Def.questId == QuestIds.ChPrologue
                            ? (System.Action)ChapterQuestService.StartPrologueBattle
                            : null,
                    PendingClaimTitle = pendingTitle,
                    OnPendingClaim = string.IsNullOrEmpty(pendingId)
                        ? null
                        : () => QuestService.TryClaim(pendingId),
                    RewardsLine = FormatRewards(view.Def.rewards),
                    RecommendedPower = rec,
                    PowerLine = FormatPowerLine(rec),
                };
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

        private static int RecommendedPower(QuestDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.regionId)) return 0;
            return WorldService.GetRegionView(def.regionId)?.Config?.recommendedPower ?? 0;
        }

        private static string FormatRewards(QuestRewardGrant[] rewards)
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

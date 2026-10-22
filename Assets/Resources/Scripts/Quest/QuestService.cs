using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.Dialogue;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.Quest.Domain;
using Assets.Resources.Scripts.Unlock;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.Quest
{
    /// <summary>Unified MVP quest façade (doc 25). Owns quests.json and mirrors onboarding/chapter saves.</summary>
    public static class QuestService
    {
        public const int MaxActiveLoops = QuestPacingRules.MaxActiveLoops;
        public static QuestState State { get; private set; }
        public static bool IsLoaded => State != null;
        private static string boundPlayerId;
        private static bool loading;
        private static bool saving;

        public static void Clear()
        {
            State = null;
            boundPlayerId = null;
        }

        public static void EnsureLoaded(DataUtil dataUtil = null)
        {
            if (loading) return;
            var util = dataUtil ?? DataUtil.Instance;
            var playerId = util?.currentPlayer?.playerID;
            if (State != null
                && !string.IsNullOrEmpty(boundPlayerId)
                && !string.IsNullOrEmpty(playerId)
                && boundPlayerId != playerId)
                Clear();

            if (State != null)
            {
                PushLegacyViews();
                return;
            }

            loading = true;
            try
            {
                QuestState loaded = util?.LoadQuestState();
                if (loaded != null)
                {
                    State = Normalize(loaded);
                }
                else
                {
                    State = MigrateFromLegacy(util) ?? CreateDefault();
                    util?.SaveQuestState(State, touchMeta: false);
                }

                boundPlayerId = playerId;
                if (util != null && State.onboardingComplete && !HasAnyStoryProgress() && !State.chapterSkipped)
                {
                    SkipChapterForLegacySave();
                    util.SaveQuestState(State, touchMeta: false);
                }
            }
            finally
            {
                loading = false;
            }

            Evaluate(persist: true);
        }

        public static void Save(DataUtil dataUtil = null)
        {
            if (State == null || saving) return;
            saving = true;
            try
            {
                PullLegacyGuides();
                State.count = State.claimedIds?.Count ?? 0;
                var util = dataUtil ?? DataUtil.Instance;
                util?.SaveQuestState(State, touchMeta: true);
                PushLegacyViews();
                if (util != null)
                {
                    util.SaveOnboardingState(ToOnboarding(), touchMeta: false);
                    util.SaveChapterQuestState(ToChapter(), touchMeta: false);
                }
            }
            finally
            {
                saving = false;
            }
        }

        public static void Evaluate(bool persist = true)
        {
            EnsureLoaded();
            if (State == null) return;
            State.flags ??= new QuestFlags();
            bool changed = false;
            if (!State.flags.formationReady && QuestRules.HasFormationMember())
            {
                State.flags.formationReady = true;
                changed = true;
            }

            if (!State.flags.chapterFormationReady && State.flags.coleFrontAdjusted)
            {
                State.flags.chapterFormationReady = true;
                changed = true;
            }

            if (!State.flags.heldRepairKit && QuestRules.HasItem(EconomyConstants.RepairKitDefId))
            {
                State.flags.heldRepairKit = true;
                changed = true;
            }

            if (!State.flags.miningSignalScanned && QuestRules.BodyAtLeast(QuestIds.BodyMining, GridNodeState.Fogged))
            {
                State.flags.miningSignalScanned = true;
                changed = true;
            }

            string prevOb = State.activeOnboardingId ?? "";
            string prevStory = State.activeStoryId ?? "";
            string prevLoops = JoinIds(State.activeLoopIds);
            RefreshActivePointers();
            RefreshActiveLoops();
            bool pointerChanged = prevOb != (State.activeOnboardingId ?? "")
                || prevStory != (State.activeStoryId ?? "")
                || prevLoops != JoinIds(State.activeLoopIds);
            if (persist && (changed || pointerChanged))
                Save();
            else
                PushLegacyViews();
        }

        public static List<QuestView> GetViews(QuestType type)
        {
            EnsureLoaded();
            Evaluate(persist: false);
            var list = new List<QuestView>();
            foreach (var def in QuestCatalog.OfType(type))
                list.Add(BuildView(def));
            return list;
        }

        public static QuestView GetActive(QuestType type)
        {
            EnsureLoaded();
            Evaluate(persist: false);
            return FirstActionable(type);
        }

        public static List<QuestView> GetTrackerViews()
        {
            EnsureLoaded();
            Evaluate(persist: false);
            var list = new List<QuestView>();
            AddIfCurrent(list, QuestType.Story);
            var ship = GetActiveWithoutEvaluate(QuestType.Ship);
            if (ship != null)
                list.Add(ship);
            else
                AddIfCurrent(list, QuestType.Expedition);
            if (!State.onboardingComplete)
                AddIfCurrent(list, QuestType.Onboarding);
            foreach (var id in State.activeLoopIds ?? new List<string>())
            {
                var def = QuestCatalog.Get(id);
                if (def != null)
                    list.Add(BuildView(def));
            }

            return list;
        }

        public static bool HasPlayedDialogue(string dialogueId) =>
            State?.playedDialogueIds != null
            && !string.IsNullOrEmpty(dialogueId)
            && State.playedDialogueIds.Contains(dialogueId);

        public static void MarkDialoguePlayed(string dialogueId)
        {
            EnsureLoaded();
            if (State == null || string.IsNullOrEmpty(dialogueId)) return;
            if (State.playedDialogueIds == null)
                State.playedDialogueIds = new List<string>();
            if (State.playedDialogueIds.Contains(dialogueId)) return;
            State.playedDialogueIds.Add(dialogueId);
            Save();
        }

        public static QuestCommandResult TryClaim(string questId, bool playDialogues = true)
        {
            EnsureLoaded();
            Evaluate(persist: false);
            var def = QuestCatalog.Get(questId);
            if (def == null)
                return QuestCommandResult.Fail("Unknown quest.");
            var view = BuildView(def);
            if (!view.CanClaim)
                return QuestCommandResult.Fail("Conditions not met yet.");

            // Mark before granting: reward events (credits/warehouse changed) can re-enter TryClaim.
            if (def.type == QuestType.Loop)
            {
                QuestRules.ResetLoopCount(State, questId);
            }
            else
            {
                AddUnique(State.claimedIds, questId);
                AddUnique(State.completedIds, questId);
            }

            int overflow = GrantRewards(def);

            MirrorLinkedOnboarding(def);

            QuestDef nextStory = null;
            if (def.type == QuestType.Onboarding)
            {
                var next = NextUnclaimed(QuestType.Onboarding);
                if (next == null)
                {
                    State.onboardingComplete = true;
                    State.activeOnboardingId = "";
                }
                else
                    State.activeOnboardingId = next.questId;
            }
            else if (def.type == QuestType.Story)
            {
                nextStory = NextEligibleStory();
                if (nextStory == null && AllStoryClaimed())
                {
                    State.chapterComplete = true;
                    State.activeStoryId = "";
                }
                else if (nextStory != null)
                    State.activeStoryId = nextStory.questId;
                else
                    State.activeStoryId = "";
            }

            RefreshActivePointers();
            RefreshActiveLoops();
            if (playDialogues)
            {
                PlayDialogue(def.dialogueIdOnClaim);
                if (nextStory != null
                    && !Assets.Resources.Scripts.UI.Nexus.Tutorial.TutorialGuideService
                        .ShouldSuppressFullscreenDialogue(nextStory.questId))
                    PlayDialogue(nextStory.dialogueIdOnStart);
            }

            Save();
            if (def.type == QuestType.Onboarding)
            {
                FeatureUnlockService.Evaluate();
                Assets.Resources.Scripts.Cosmetics.AvatarFrameService.Evaluate();
            }

            return QuestCommandResult.Ok(overflow > 0
                ? Assets.Scripts.Utils.LocalizationUtil.T("Warehouse full — rewards moved to Pending Loot.", "仓库已满，奖励已转入待领取收益。")
                : "Claimed " + questId);
        }

        public static void NotifyFormationReady()
        {
            EnsureLoaded();
            if (State.flags.formationReady) return;
            State.flags.formationReady = true;
            Save();
        }

        public static void NotifyFirstBattleWon()
        {
            EnsureLoaded();
            if (State.flags.firstBattleWon) return;
            State.flags.firstBattleWon = true;
            Save();
        }

        public static void NotifyGatherProgress()
        {
            EnsureLoaded();
            if (State.flags.gatherStartedOrLooted) return;
            State.flags.gatherStartedOrLooted = true;
            Save();
        }

        public static void NotifyGatherCycle(string nodeId, int cycles = 1)
        {
            EnsureLoaded();
            if (cycles <= 0) cycles = 1;
            State.flags.gatherCycleSettled = true;
            State.flags.gatherStartedOrLooted = true;
            foreach (var def in QuestCatalog.OfType(QuestType.Loop))
            {
                if (def.loopKind != QuestLoopKind.Gather) continue;
                if (!string.IsNullOrEmpty(def.gatherNodeId) && def.gatherNodeId != nodeId) continue;
                if (string.IsNullOrEmpty(def.gatherNodeId) && !GatherNodeMatchesRegion(nodeId, def.regionId))
                    continue;
                if (QuestRules.IsTriggered(State, def))
                    QuestRules.AddLoopCount(State, def.questId, cycles);
            }

            Save();
        }

        public static void NotifyFarmVictory(string regionId, int cycles = 1)
        {
            EnsureLoaded();
            if (cycles <= 0) cycles = 1;
            foreach (var def in QuestCatalog.OfType(QuestType.Loop))
            {
                if (def.loopKind != QuestLoopKind.Farm) continue;
                if (def.regionId != regionId) continue;
                if (QuestRules.IsTriggered(State, def))
                    QuestRules.AddLoopCount(State, def.questId, cycles);
            }

            Save();
        }

        public static void NotifyCraftedOnce(string outputDefId = "")
        {
            EnsureLoaded();
            State.flags.craftedOnce = true;
            if (outputDefId == EconomyConstants.RepairKitDefId || QuestRules.HasItem(EconomyConstants.RepairKitDefId))
                State.flags.heldRepairKit = true;
            var craft = QuestCatalog.Get(QuestIds.LpCraftRepair);
            if (craft != null && outputDefId == EconomyConstants.RepairKitDefId && QuestRules.IsTriggered(State, craft))
                QuestRules.AddLoopCount(State, craft.questId, 1);
            Save();
        }

        public static void NotifyShopTraded()
        {
            EnsureLoaded();
            State.flags.shopTradedOnce = true;
            var shop = QuestCatalog.Get(QuestIds.LpShop);
            if (shop != null && QuestRules.IsTriggered(State, shop))
                QuestRules.AddLoopCount(State, shop.questId, 1);
            Save();
        }

        public static void NotifyPrologueWon()
        {
            EnsureLoaded();
            if (State.flags.prologueWon) return;
            State.flags.prologueWon = true;
            // Progress accepts the next quest here. The prologue reward stays on TryClaim.
            Evaluate(persist: true);
        }

        /// <summary>
        /// Battle-log hint for Patchwork Fleet. Marks the lesson seen so a later player edit can finish the quest.
        /// Returns empty when this battle is not that lesson.
        /// </summary>
        public static string FormationBattleHint()
        {
            EnsureLoaded();
            if (State?.flags == null) return "";
            if (State.flags.coleFrontAdjusted || State.flags.chapterFormationReady) return "";
            if (QuestRules.IsClaimed(State, QuestIds.ChFormation)) return "";
            if (State.activeStoryId != QuestIds.ChFormation) return "";
            if (!State.flags.formationLessonSeen)
            {
                State.flags.formationLessonSeen = true;
                Save();
            }

            return Assets.Resources.Scripts.UI.Nexus.UiText.BattleLogFormationBad;
        }

        /// <summary>Counts only a player assign of Cole into the front row after the formation battle hint.</summary>
        public static void NotifyColePlacedInFront(string cardId, System.Collections.Generic.IList<Assets.Resources.Scripts.Entity.CardEntity> cards)
        {
            EnsureLoaded();
            if (State?.flags == null || string.IsNullOrEmpty(cardId)) return;
            if (State.flags.coleFrontAdjusted || QuestRules.IsClaimed(State, QuestIds.ChFormation)) return;
            if (!State.flags.formationLessonSeen) return;
            if (State.activeStoryId != QuestIds.ChFormation) return;

            Assets.Resources.Scripts.Entity.CardEntity entity = null;
            if (cards != null)
            {
                foreach (var card in cards)
                {
                    if (card != null && card.id == cardId)
                    {
                        entity = card;
                        break;
                    }
                }
            }

            if (entity == null || entity.characterName != Assets.Resources.Scripts.CharacterPanel.CharacterName.Cole)
                return;

            State.flags.coleFrontAdjusted = true;
            State.flags.chapterFormationReady = true;
            Evaluate(persist: true);
        }

        public static string LocalizedHint(QuestDef def)
        {
            if (def == null) return "";
            if (def.questId == QuestIds.ChFormation && State?.flags != null && !State.flags.coleFrontAdjusted && !State.flags.chapterFormationReady)
            {
                if (State.flags.formationLessonSeen)
                    return Assets.Resources.Scripts.UI.Nexus.UiText.T(
                        "The fight showed this lineup cannot hold. Move Cole to the front row.",
                        "这场战斗说明当前阵容站不住。把科尔调到前排。");
                return Assets.Resources.Scripts.UI.Nexus.UiText.T(
                    "Asra is in the back row. Fight once and see why this lineup fails.",
                    "阿斯拉在后排。先打一场，看清这套阵容为什么不行。");
            }

            return Assets.Resources.Scripts.UI.Nexus.UiText.T(def.hintEn ?? "", def.hintZh ?? "");
        }

        public static string TargetScreen(QuestDef def)
        {
            if (def == null) return "";
            if (def.questId == QuestIds.ChFormation
                && State?.flags != null
                && !State.flags.formationLessonSeen
                && !State.flags.coleFrontAdjusted
                && !State.flags.chapterFormationReady)
                return "Battle";
            return def.targetScreen ?? "";
        }

        public static QuestView LatestClaimable(string exceptQuestId = null)
        {
            EnsureLoaded();
            Evaluate(persist: false);
            foreach (var type in new[] { QuestType.Story, QuestType.Onboarding, QuestType.Expedition, QuestType.Ship, QuestType.Loop })
            {
                QuestView last = null;
                foreach (var def in QuestCatalog.OfType(type))
                {
                    var view = BuildView(def);
                    if (view?.Def == null || !view.CanClaim) continue;
                    if (!string.IsNullOrEmpty(exceptQuestId) && view.Def.questId == exceptQuestId) continue;
                    last = view;
                }

                if (last != null)
                    return last;
            }

            return null;
        }

        public static void NotifyEmergencyRecruit()
        {
            EnsureLoaded();
            State.flags.emergencyRecruitDone = true;
            Save();
        }

        public static void NotifyOuterCleanupWon()
        {
            EnsureLoaded();
            State.flags.outerCleanupWon = true;
            State.flags.firstBattleWon = true;
            Save();
        }

        public static void NotifyMiningSpurWon()
        {
            EnsureLoaded();
            State.flags.miningSpurWon = true;
            Save();
        }

        public static void NotifyMiningSignalScanned()
        {
            EnsureLoaded();
            State.flags.miningSignalScanned = true;
            Save();
        }

        public static void NotifyRegionVictory(string regionId)
        {
            EnsureLoaded();
            State.flags.firstBattleWon = true;
            if (regionId == WorldConstants.OuterBeltId)
                State.flags.outerCleanupWon = true;
            if (regionId == WorldConstants.MiningSpurId)
                State.flags.miningSpurWon = true;
            if (regionId == WorldConstants.FrontierBossId)
                State.flags.frontierBossWon = true;
            Evaluate(persist: true);
        }

        public static void NotifyPendingLootClaimed()
        {
            EnsureLoaded();
            State.flags.pendingLootClaimed = true;
            Save();
        }

        public static bool TryChooseRoute(string route)
        {
            EnsureLoaded();
            if (route != "convoy" && route != "rift") return false;
            if (!QuestRules.BothRoutesFogged()) return false;
            State.routeChoice = route;
            State.flags.routeChoiceMade = true;
            Save();
            return true;
        }

        public static void MarkGuideComplete(string guideId, bool chapter)
        {
            EnsureLoaded();
            AddUnique(State.completedGuideIds, guideId);
            Save();
        }

        public static void MarkStrategyCompareShown()
        {
            EnsureLoaded();
            if (State.flags.strategyCompareShown) return;
            State.flags.strategyCompareShown = true;
            Save();
        }

        public static bool IsGuideComplete(string guideId) =>
            State?.completedGuideIds != null && State.completedGuideIds.Contains(guideId);

        private static QuestView BuildView(QuestDef def)
        {
            var status = ResolveStatus(def);
            int count = def.type == QuestType.Loop ? QuestRules.LoopCount(State, def.questId) : 0;
            bool ready = status == QuestStatus.Ready
                || (status == QuestStatus.Active && QuestRules.IsCompleteWhen(State, def, Cards()));
            if (ready && status != QuestStatus.Claimed)
                status = QuestStatus.Ready;
            return new QuestView
            {
                Def = def,
                Status = status,
                CanClaim = status == QuestStatus.Ready && (def.type == QuestType.Loop || !QuestRules.IsClaimed(State, def.questId)),
                LoopCount = count,
                LoopTarget = def.loopTarget
            };
        }

        private static QuestStatus ResolveStatus(QuestDef def)
        {
            if (def.type != QuestType.Loop && QuestRules.IsClaimed(State, def.questId))
                return QuestStatus.Claimed;
            if (def.type == QuestType.Onboarding && State.onboardingComplete)
                return QuestStatus.Claimed;
            if (def.type == QuestType.Story && (State.chapterComplete || State.chapterSkipped))
                return QuestStatus.Claimed;
            if (!QuestRules.IsTriggered(State, def))
                return QuestStatus.Locked;

            bool isCurrent = def.type switch
            {
                QuestType.Onboarding => def.questId == State.activeOnboardingId,
                QuestType.Story => def.questId == State.activeStoryId,
                QuestType.Expedition => IsCurrentExpedition(def),
                QuestType.Ship => IsCurrentShip(def),
                QuestType.Loop => State.activeLoopIds != null && State.activeLoopIds.Contains(def.questId),
                _ => false
            };
            bool complete = QuestRules.IsCompleteWhen(State, def, Cards());
            // A finished quest stays claimable after the next one is accepted.
            if (QuestPacingRules.StaysOffBoard(def.type, isCurrent, complete))
                return QuestStatus.Locked;
            return complete ? QuestStatus.Ready : QuestStatus.Active;
        }

        private static bool IsCurrentExpedition(QuestDef def)
        {
            if (def == null) return false;
            var offers = new List<ExpeditionOffer>();
            foreach (var other in QuestCatalog.OfType(QuestType.Expedition))
            {
                offers.Add(new ExpeditionOffer
                {
                    QuestId = other.questId,
                    RegionId = other.regionId,
                    Claimed = QuestRules.IsClaimed(State, other.questId),
                    Triggered = QuestRules.IsTriggered(State, other),
                    Complete = QuestRules.IsCompleteWhen(State, other, Cards())
                });
            }

            var current = QuestPacingRules.SelectCurrentExpedition(
                offers,
                State.routeChoice,
                WorldConstants.ConvoyLaneId,
                WorldConstants.QuantumRiftId);
            return !string.IsNullOrEmpty(current) && current == def.questId;
        }

        private static bool IsCurrentShip(QuestDef def)
        {
            // Doc 25 §7: ship gates step up one at a time (QuestPacingRules.MaxActiveShip).
            QuestDef ready = null;
            foreach (var other in QuestCatalog.OfType(QuestType.Ship))
            {
                if (QuestRules.IsClaimed(State, other.questId)) continue;
                if (!QuestRules.IsTriggered(State, other)) continue;
                if (!QuestRules.IsCompleteWhen(State, other, Cards()))
                    return other.questId == def.questId;
                ready ??= other;
            }

            return ready != null && ready.questId == def.questId;
        }

        private static void RefreshActivePointers()
        {
            if (!State.onboardingComplete)
            {
                var ob = NextUnclaimed(QuestType.Onboarding);
                State.activeOnboardingId = ob?.questId ?? "";
                if (ob == null)
                    State.onboardingComplete = true;
            }

            if (!State.chapterComplete && !State.chapterSkipped)
            {
                var story = NextEligibleStory();
                State.activeStoryId = story?.questId ?? "";
                if (story == null && AllStoryClaimed())
                    State.chapterComplete = true;
            }
        }

        private static void RefreshActiveLoops()
        {
            State.activeLoopIds ??= new List<string>();
            var keep = new List<string>();
            foreach (var id in State.activeLoopIds)
            {
                var def = QuestCatalog.Get(id);
                if (def == null || def.type != QuestType.Loop) continue;
                if (QuestRules.IsTriggered(State, def))
                    keep.Add(id);
            }

            string preferred = PreferredLoopRegion();
            foreach (var def in QuestCatalog.OfType(QuestType.Loop))
            {
                if (keep.Count >= MaxActiveLoops) break;
                if (keep.Contains(def.questId)) continue;
                if (!QuestRules.IsTriggered(State, def)) continue;
                if (!string.IsNullOrEmpty(preferred) && def.regionId != preferred && def.loopKind != QuestLoopKind.Shop && def.loopKind != QuestLoopKind.Craft)
                    continue;
                keep.Add(def.questId);
            }

            if (keep.Count < MaxActiveLoops)
            {
                foreach (var def in QuestCatalog.OfType(QuestType.Loop))
                {
                    if (keep.Count >= MaxActiveLoops) break;
                    if (keep.Contains(def.questId)) continue;
                    if (!QuestRules.IsTriggered(State, def)) continue;
                    keep.Add(def.questId);
                }
            }

            State.activeLoopIds = keep;
        }

        private static string PreferredLoopRegion()
        {
            foreach (var def in QuestCatalog.OfType(QuestType.Expedition))
            {
                if (QuestRules.IsClaimed(State, def.questId)) continue;
                if (QuestRules.IsTriggered(State, def))
                    return def.regionId;
            }

            if (State.routeChoice == "convoy") return WorldConstants.ConvoyLaneId;
            if (State.routeChoice == "rift") return WorldConstants.QuantumRiftId;
            return "";
        }

        private static QuestDef NextUnclaimed(QuestType type)
        {
            QuestDef ready = null;
            foreach (var def in QuestCatalog.OfType(type))
            {
                if (QuestRules.IsClaimed(State, def.questId) || !QuestRules.DependsMet(State, def))
                    continue;
                if (!QuestRules.IsCompleteWhen(State, def, Cards()))
                    return def;
                ready ??= def;
            }

            return ready;
        }

        private static QuestDef NextEligibleStory()
        {
            QuestDef ready = null;
            foreach (var def in QuestCatalog.OfType(QuestType.Story))
            {
                if (QuestRules.IsClaimed(State, def.questId)) continue;
                if (!QuestRules.IsTriggered(State, def)) continue;
                if (!QuestRules.IsCompleteWhen(State, def, Cards()))
                    return def;
                ready ??= def;
            }

            return ready;
        }

        private static QuestView FirstActionable(QuestType type)
        {
            QuestView ready = null;
            foreach (var def in QuestCatalog.OfType(type))
            {
                var view = BuildView(def);
                if (view.Status == QuestStatus.Active)
                    return view;
                if (view.Status == QuestStatus.Ready && ready == null)
                    ready = view;
            }

            return ready;
        }

        private static bool AllStoryClaimed()
        {
            foreach (var def in QuestCatalog.OfType(QuestType.Story))
            {
                if (!QuestRules.IsClaimed(State, def.questId))
                    return false;
            }

            return true;
        }

        private static void AddIfCurrent(List<QuestView> list, QuestType type)
        {
            var view = GetActiveWithoutEvaluate(type);
            if (view?.Def != null)
                list.Add(view);
        }

        private static QuestView GetActiveWithoutEvaluate(QuestType type) =>
            FirstActionable(type);

        private static void MirrorLinkedOnboarding(QuestDef def)
        {
            if (string.IsNullOrEmpty(def?.linkedOnboardingStepId)) return;
            switch (def.linkedOnboardingStepId)
            {
                case QuestIds.ObFormation:
                    State.flags.formationReady = true;
                    break;
                case QuestIds.ObFirstBattle:
                    State.flags.firstBattleWon = true;
                    break;
                case QuestIds.ObGather:
                    State.flags.gatherStartedOrLooted = true;
                    break;
                case QuestIds.ObCraft:
                    State.flags.craftedOnce = true;
                    break;
            }
        }

        private static void PlayDialogue(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId)) return;
            if (DialogueService.IsPlaying) return;
            DialogueService.Play(dialogueId);
        }

        /// <summary>Grants every reward; items that do not fit go to Pending Loot. Returns the overflow count.</summary>
        private static int GrantRewards(QuestDef def)
        {
            if (def?.rewards == null) return 0;
            int overflow = 0;
            int credits = 0;
            foreach (var reward in def.rewards)
            {
                if (reward == null) continue;
                if (reward.kind == "credits" || reward.credits > 0)
                {
                    credits += Mathf.Max(0, reward.credits);
                    continue;
                }

                if (reward.kind == "item" && !string.IsNullOrEmpty(reward.itemDefId))
                {
                    int qty = Mathf.Max(1, reward.quantity);
                    int quality = reward.quality > 0 ? reward.quality : EconomyConstants.DefaultQuality;
                    if (!ProductionService.TryAddLocal(ItemFactory.FromDef(reward.itemDefId, qty, quality)))
                    {
                        IdleSettlementService.EnqueuePending(reward.itemDefId, qty, quality, "quest:" + def.questId);
                        overflow++;
                    }
                }
            }

            if (overflow > 0)
                IdleSettlementService.Save();
            if (credits > 0)
                CurrencyService.AddCredits(credits);
            return overflow;
        }

        private static bool GatherNodeMatchesRegion(string nodeId, string regionId)
        {
            var node = GatherNodeCatalog.Get(nodeId);
            return node != null && node.regionId == regionId;
        }

        private static IList<Assets.Resources.Scripts.Entity.CardEntity> Cards() =>
            CardListManager.Instance?.cardEntities;

        private static void AddUnique(List<string> list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id) || list.Contains(id)) return;
            list.Add(id);
        }

        private static string JoinIds(List<string> ids)
        {
            if (ids == null || ids.Count == 0) return "";
            return string.Join(",", ids);
        }

        private static QuestState CreateDefault() =>
            new QuestState
            {
                activeOnboardingId = QuestIds.ObFormation,
                activeStoryId = QuestIds.ChPrologue,
                flags = new QuestFlags()
            };

        private static QuestState Normalize(QuestState state)
        {
            state ??= CreateDefault();
            state.claimedIds ??= new List<string>();
            state.completedIds ??= new List<string>();
            state.completedGuideIds ??= new List<string>();
            state.playedDialogueIds ??= new List<string>();
            state.loops ??= new List<QuestLoopProgress>();
            state.activeLoopIds ??= new List<string>();
            state.flags ??= new QuestFlags();
            return state;
        }

        private static QuestState MigrateFromLegacy(DataUtil util)
        {
            if (util == null) return null;
            var ob = util.LoadOnboardingState();
            var ch = util.LoadChapterQuestState();
            if (ob == null && ch == null) return null;
            var state = CreateDefault();
            if (ob != null)
            {
                state.onboardingComplete = ob.chainCompleted;
                state.activeOnboardingId = ob.activeStepId ?? "";
                MergeIds(state.claimedIds, ob.claimedStepIds);
                MergeIds(state.completedIds, ob.completedStepIds);
                MergeIds(state.completedGuideIds, ob.completedGuideIds);
                if (ob.flags != null)
                {
                    state.flags.formationReady = ob.flags.formationReady;
                    state.flags.firstBattleWon = ob.flags.firstBattleWon;
                    state.flags.gatherStartedOrLooted = ob.flags.gatherStartedOrLooted;
                    state.flags.craftedOnce = ob.flags.craftedOnce;
                    state.flags.shopTradedOnce = ob.flags.shopTradedOnce;
                }
            }

            if (ch != null)
            {
                state.chapterComplete = ch.chapterCompleted;
                state.chapterSkipped = ch.chapterSkipped;
                state.activeStoryId = ch.activeStepId ?? "";
                MergeIds(state.claimedIds, ch.claimedStepIds);
                MergeIds(state.completedIds, ch.completedStepIds);
                MergeIds(state.completedGuideIds, ch.completedGuideIds);
                if (ch.flags != null)
                {
                    state.flags.prologueWon = ch.flags.prologueWon;
                    state.flags.emergencyRecruitDone = ch.flags.emergencyRecruitDone;
                    state.flags.chapterFormationReady = ch.flags.formationReady;
                    state.flags.outerCleanupWon = ch.flags.outerCleanupWon;
                    state.flags.miningSignalScanned = ch.flags.miningSignalScanned;
                    state.flags.miningSpurWon = ch.flags.miningSpurWon;
                    state.flags.strategyCompareShown = ch.flags.strategyCompareShown;
                    state.flags.gatherCycleSettled = ch.flags.gatherCycleSettled;
                    state.flags.craftedOnce = state.flags.craftedOnce || ch.flags.craftedOnce;
                    state.flags.heldRepairKit = ch.flags.heldRepairKit;
                    state.flags.routeChoiceMade = ch.flags.routeChoiceMade;
                    state.routeChoice = ch.flags.routeChoice ?? "";
                    state.flags.pendingLootClaimed = ch.flags.pendingLootClaimed;
                    state.flags.frontierBossWon = ch.flags.frontierBossWon;
                }
            }

            return Normalize(state);
        }

        private static void MergeIds(List<string> dest, List<string> src)
        {
            if (src == null) return;
            foreach (var id in src)
                AddUnique(dest, id);
        }

        private static bool HasAnyStoryProgress() =>
            State.claimedIds != null && State.claimedIds.Exists(id => id != null && id.StartsWith("ch1_"));

        private static void SkipChapterForLegacySave()
        {
            State.chapterSkipped = true;
            State.chapterComplete = true;
            State.activeStoryId = "";
            foreach (var def in QuestCatalog.OfType(QuestType.Story))
            {
                AddUnique(State.claimedIds, def.questId);
                AddUnique(State.completedIds, def.questId);
            }
        }

        private static void PullLegacyGuides()
        {
            if (Onboarding.OnboardingService.IsLoaded)
                MergeIds(State.completedGuideIds, Onboarding.OnboardingService.State?.completedGuideIds);
            if (ChapterQuestService.IsLoaded)
                MergeIds(State.completedGuideIds, ChapterQuestService.State?.completedGuideIds);
        }

        private static void PushLegacyViews()
        {
            PullLegacyGuides();
            OnboardingService.Bind(ToOnboarding());
            ChapterQuestService.Bind(ToChapter());
        }

        private static OnboardingState ToOnboarding()
        {
            var f = State.flags ?? new QuestFlags();
            return new OnboardingState
            {
                chainId = OnboardingCatalog.ChainId,
                activeStepId = State.activeOnboardingId ?? "",
                completedStepIds = FilterPrefix("ob_"),
                claimedStepIds = FilterPrefix("ob_"),
                completedGuideIds = new List<string>(State.completedGuideIds ?? new List<string>()),
                chainCompleted = State.onboardingComplete,
                flags = new OnboardingFlags
                {
                    formationReady = f.formationReady,
                    firstBattleWon = f.firstBattleWon,
                    gatherStartedOrLooted = f.gatherStartedOrLooted,
                    craftedOnce = f.craftedOnce,
                    shopTradedOnce = f.shopTradedOnce
                }
            };
        }

        private static ChapterQuestState ToChapter()
        {
            var f = State.flags ?? new QuestFlags();
            return new ChapterQuestState
            {
                chapterId = ChapterQuestCatalog.ChapterId,
                activeStepId = State.activeStoryId ?? "",
                completedStepIds = FilterPrefix("ch1_"),
                claimedStepIds = FilterPrefix("ch1_"),
                completedGuideIds = new List<string>(State.completedGuideIds ?? new List<string>()),
                chapterCompleted = State.chapterComplete,
                chapterSkipped = State.chapterSkipped,
                flags = new ChapterQuestFlags
                {
                    prologueWon = f.prologueWon,
                    emergencyRecruitDone = f.emergencyRecruitDone,
                    formationReady = f.chapterFormationReady,
                    outerCleanupWon = f.outerCleanupWon,
                    miningSignalScanned = f.miningSignalScanned,
                    miningSpurWon = f.miningSpurWon,
                    strategyCompareShown = f.strategyCompareShown,
                    gatherCycleSettled = f.gatherCycleSettled,
                    craftedOnce = f.craftedOnce,
                    heldRepairKit = f.heldRepairKit,
                    routeChoiceMade = f.routeChoiceMade,
                    routeChoice = State.routeChoice ?? "",
                    pendingLootClaimed = f.pendingLootClaimed,
                    frontierBossWon = f.frontierBossWon
                }
            };
        }

        private static List<string> FilterPrefix(string prefix)
        {
            var list = new List<string>();
            if (State.claimedIds == null) return list;
            foreach (var id in State.claimedIds)
            {
                if (id != null && id.StartsWith(prefix))
                    list.Add(id);
            }

            return list;
        }
    }
}

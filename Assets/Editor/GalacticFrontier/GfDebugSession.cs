using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Quest;
using Assets.Resources.Scripts.Quest.Domain;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Unlock;
using Assets.Resources.Scripts.Unlock.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GalacticFrontier.DebugTools
{
    /// <summary>
    /// Editor-only debug operations. Lives under Assets/Editor so it is not in player builds.
    /// Mutating calls redirect <see cref="DataUtil"/> saves into Temp/gf-debug-saves first.
    /// </summary>
    public static partial class GfDebugSession
    {
        public const string SandboxFolderName = "gf-debug-saves";

        public static GfDebugResult QueryQuest()
        {
            var error = RequirePlay(out var data);
            if (error != null)
                return GfDebugResult.Fail(error);

            var bound = BoundPlayerDirectory(data);
            if (data.currentPlayer == null || string.IsNullOrWhiteSpace(data.currentPlayer.playerID))
            {
                return GfDebugResult.Ok("No player is loaded.", new Dictionary<string, object>
                {
                    ["playing"] = true,
                    ["scene"] = SceneManager.GetActiveScene().name,
                    ["savePath"] = SaveRoot(data),
                    ["sandbox"] = IsSandbox(SaveRoot(data)),
                    ["playerId"] = null,
                    ["mainQuestId"] = null,
                    ["story"] = null,
                    ["onboarding"] = null,
                    ["features"] = new List<object>(),
                    ["deck"] = null,
                    ["cards"] = new List<object>()
                });
            }

            if (!IsSandbox(bound))
            {
                var message = string.IsNullOrEmpty(bound)
                    ? "A player id is selected, but no save directory is bound. Services were not loaded."
                    : "Official save is loaded. Quest services were not evaluated or saved.";
                return GfDebugResult.Ok(message, new Dictionary<string, object>
                {
                    ["playing"] = true,
                    ["scene"] = SceneManager.GetActiveScene().name,
                    ["savePath"] = string.IsNullOrEmpty(bound) ? SaveRoot(data) : bound,
                    ["sandbox"] = false,
                    ["playerId"] = data.currentPlayer.playerID,
                    ["mainQuestId"] = QuestService.IsLoaded ? QuestService.State.activeStoryId : null,
                    ["activeStoryId"] = QuestService.IsLoaded ? QuestService.State.activeStoryId : null,
                    ["activeOnboardingId"] = QuestService.IsLoaded ? QuestService.State.activeOnboardingId : null,
                    ["story"] = null,
                    ["onboarding"] = null,
                    ["features"] = new List<object>(),
                    ["deck"] = null,
                    ["cards"] = new List<object>(),
                    ["officialSaveUntouched"] = true
                });
            }

            QuestService.EnsureLoaded(data);
            FeatureUnlockService.EnsureLoaded(data);
            var story = QuestService.GetActive(QuestType.Story);
            var onboarding = QuestService.GetActive(QuestType.Onboarding);
            var storyId = QuestService.State?.activeStoryId ?? "";
            var onboardingId = QuestService.State?.activeOnboardingId ?? "";
            var mainId = !string.IsNullOrEmpty(storyId) ? storyId : onboardingId;
            return GfDebugResult.Ok("Quest state.", new Dictionary<string, object>
            {
                ["playing"] = true,
                ["scene"] = SceneManager.GetActiveScene().name,
                ["savePath"] = SaveRoot(data),
                ["sandbox"] = IsSandbox(SaveRoot(data)),
                ["playerId"] = data.currentPlayer.playerID,
                ["mainQuestId"] = mainId,
                ["activeStoryId"] = storyId,
                ["activeOnboardingId"] = onboardingId,
                ["story"] = Describe(story),
                ["onboarding"] = Describe(onboarding),
                ["features"] = FeatureRows(),
                ["deck"] = DeckSnapshot(data),
                ["cards"] = CardRows()
            });
        }

        public static GfDebugResult NewPlayer()
        {
            var error = RequirePlay(out var data);
            if (error != null)
                return GfDebugResult.Fail(error);
            error = RedirectSandbox(data);
            if (error != null)
                return GfDebugResult.Fail(error);
            var root = SaveRoot(data);
            if (IsOfficialSave(root))
                return GfDebugResult.Fail("Refusing to create a player while the save path is the official saves directory.");

            // Drop the previous in-memory player without flushing it. CreatePlayerData
            // would otherwise write that player's services back to their bound directory.
            data.currentPlayer = null;
            bool created;
            try
            {
                created = data.CreatePlayerData();
            }
            catch (Exception ex)
            {
                return GfDebugResult.Fail("CreatePlayerData threw: " + ex.Message);
            }

            if (!created)
                return GfDebugResult.Fail(string.IsNullOrEmpty(data.LastSaveError)
                    ? "CreatePlayerData returned false."
                    : data.LastSaveError);

            LoadingOverlay.LoadScene(nameof(SceneLoader.SceneName.MainScene));
            return GfDebugResult.Ok("Created a sandbox player and started loading MainScene.", new Dictionary<string, object>
            {
                ["playerId"] = data.currentPlayer != null ? data.currentPlayer.playerID : "",
                ["savePath"] = root,
                ["sandbox"] = true,
                ["scene"] = nameof(SceneLoader.SceneName.MainScene)
            });
        }

        public static GfDebugResult JumpToQuest(string questId)
        {
            var error = RequirePlayer(out var data);
            if (error != null)
                return GfDebugResult.Fail(error);
            if (string.IsNullOrWhiteSpace(questId))
                return GfDebugResult.Fail("quest_id is required.");

            error = EnsureSandboxWrites(data);
            if (error != null)
                return GfDebugResult.Fail(error);

            QuestService.EnsureLoaded(data);
            var target = QuestCatalog.Get(questId.Trim());
            if (target == null)
                return GfDebugResult.Fail("Unknown quest id: " + questId);
            if (target.type != QuestType.Loop && Claimed(target.questId))
                return GfDebugResult.Fail("Target quest is already claimed: " + target.questId);

            var ancestors = Ancestors(target);
            foreach (var prereq in ancestors)
            {
                error = ClaimPrerequisite(prereq);
                if (error != null)
                    return GfDebugResult.Fail(error);
            }

            if (target.type != QuestType.Loop && Claimed(target.questId))
                return GfDebugResult.Fail("Target quest was claimed while preparing prerequisites.");

            QuestService.Evaluate(true);
            FeatureUnlockService.Evaluate();
            var view = FindView(target);
            bool current = IsCurrent(target, view);
            var payload = new Dictionary<string, object>
            {
                ["questId"] = target.questId,
                ["titleEn"] = target.titleEn,
                ["titleZh"] = target.titleZh,
                ["status"] = view != null ? view.Status.ToString() : "Missing",
                ["conditionsMet"] = QuestRules.IsCompleteWhen(QuestService.State, target, Cards()),
                ["claimed"] = Claimed(target.questId),
                ["isCurrentTarget"] = current,
                ["activeStoryId"] = QuestService.State?.activeStoryId ?? "",
                ["activeOnboardingId"] = QuestService.State?.activeOnboardingId ?? "",
                ["claimedPrerequisites"] = ClaimedIds(ancestors)
            };
            if (!current)
                return GfDebugResult.Fail("Quest did not become the current target.", payload);
            return GfDebugResult.Ok("Quest is the current target.", payload);
        }

        public static GfDebugResult JumpToBattle(string encounterId, string regionId)
        {
            var error = RequirePlayer(out var data);
            if (error != null)
                return GfDebugResult.Fail(error);
            if (string.IsNullOrWhiteSpace(encounterId))
                return GfDebugResult.Fail("encounter_id is required.");
            error = EnsureSandboxWrites(data);
            if (error != null)
                return GfDebugResult.Fail(error);
            encounterId = encounterId.Trim();
            if (EncounterCatalog.Get(encounterId) == null)
                return GfDebugResult.Fail("Unknown encounter id: " + encounterId);

            if (encounterId == ChapterQuestService.PrologueEncounterId)
            {
                ChapterQuestService.StartPrologueBattle();
                return GfDebugResult.Ok("Started the prologue battle.", new Dictionary<string, object>
                {
                    ["encounterId"] = encounterId,
                    ["regionId"] = ChapterQuestService.PrologueRegionId,
                    ["scene"] = nameof(SceneLoader.SceneName.BattleScene)
                });
            }

            if (string.IsNullOrWhiteSpace(regionId))
                regionId = FindRegionForEncounter(encounterId);
            if (string.IsNullOrEmpty(regionId))
                return GfDebugResult.Fail("Could not resolve a region for encounter " + encounterId + ". Pass region_id.");

            var cards = Cards();
            if (DataUtil.Instance != null)
                DeckService.EnsureLoaded(DataUtil.Instance, cards);
            var deck = DeckService.GetActiveCombatDeck();
            if (deck == null || deck.MemberCount < 1)
                return GfDebugResult.Fail("Combat deck has no members.");

            bool stopped = false;
            var start = DeckService.TryStart(deck.deckId, DeckActionType.MainCombat, regionId, cards);
            if (!start.Success && start.Error == DeckCommandError.DeckBusy)
            {
                var stop = DeckService.TryStop(deck.deckId);
                if (!stop.Success)
                    return GfDebugResult.Fail(stop.Message);
                stopped = true;
                start = DeckService.TryStart(deck.deckId, DeckActionType.MainCombat, regionId, cards);
            }

            if (!start.Success)
                return GfDebugResult.Fail(string.IsNullOrEmpty(start.Message) ? "Could not start combat." : start.Message);

            BattleController.LaunchEncounter(regionId, encounterId);
            return GfDebugResult.Ok("Launching battle.", new Dictionary<string, object>
            {
                ["encounterId"] = encounterId,
                ["regionId"] = regionId,
                ["stoppedPreviousAction"] = stopped,
                ["scene"] = nameof(SceneLoader.SceneName.BattleScene)
            });
        }

        public static GfDebugResult SetDeckSlot(int slotIndex, string cardId)
        {
            var error = RequirePlayer(out var data);
            if (error != null)
                return GfDebugResult.Fail(error);
            error = RedirectSandbox(data);
            if (error != null)
                return GfDebugResult.Fail(error);
            error = EnsureSandboxWrites(data);
            if (error != null)
                return GfDebugResult.Fail(error);

            cardId ??= "";
            var cards = Cards();
            if (cards == null)
                return GfDebugResult.Fail("Card list is not loaded.");
            if (!string.IsNullOrEmpty(cardId) && FindCard(cards, cardId) == null)
                return GfDebugResult.Fail("Card id is not in the owned collection: " + cardId);

            DeckService.EnsureLoaded(data, cards);
            bool stopped = false;
            var result = DeckService.TryAssignToActiveCombat(slotIndex, cardId, cards);
            if (!result.Success && result.Error == DeckCommandError.DeckBusy)
            {
                var deck = DeckService.GetActiveCombatDeck();
                var stop = deck != null ? DeckService.TryStop(deck.deckId) : null;
                if (stop == null || !stop.Success)
                    return GfDebugResult.Fail(stop != null ? stop.Message : result.Message);
                stopped = true;
                result = DeckService.TryAssignToActiveCombat(slotIndex, cardId, cards);
            }

            if (!result.Success)
                return GfDebugResult.Fail(string.IsNullOrEmpty(result.Message) ? "Slot assign failed." : result.Message);

            var slots = CurrentSlots();
            if (slotIndex < 0 || slotIndex >= slots.Length || (slots[slotIndex] ?? "") != cardId)
                return GfDebugResult.Fail("Slot write did not stick.", new Dictionary<string, object>
                {
                    ["slotIndex"] = slotIndex,
                    ["cardId"] = cardId,
                    ["slots"] = slots
                });

            return GfDebugResult.Ok("Deck slot updated.", new Dictionary<string, object>
            {
                ["slotIndex"] = slotIndex,
                ["cardId"] = cardId,
                ["stoppedPreviousAction"] = stopped,
                ["slots"] = slots
            });
        }

        public static GfDebugResult GrantResource(string itemId, int quantity, int credits)
        {
            var error = RequirePlayer(out var data);
            if (error != null)
                return GfDebugResult.Fail(error);
            if (string.IsNullOrWhiteSpace(itemId) && credits <= 0)
                return GfDebugResult.Fail("Pass item_id and quantity, or credits greater than zero.");
            error = EnsureSandboxWrites(data);
            if (error != null)
                return GfDebugResult.Fail(error);

            int creditsBefore = CurrencyService.Credits;
            if (credits > 0)
            {
                CurrencyService.AddCredits(credits);
                if (CurrencyService.Credits != creditsBefore + credits)
                    return GfDebugResult.Fail("Credits were not added.");
            }
            else if (credits < 0)
            {
                return GfDebugResult.Fail("credits cannot be negative.");
            }

            int owned = 0;
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                if (quantity <= 0)
                    return GfDebugResult.Fail("quantity must be greater than zero.");
                if (ItemCatalog.Get(itemId) == null)
                    return GfDebugResult.Fail("Unknown item id: " + itemId);
                if (!ProductionService.TryAddLocal(ItemFactory.FromDef(itemId, quantity)))
                    return GfDebugResult.Fail("Warehouse rejected " + itemId + ".");
                owned = ProductionService.CountLocal(itemId);
            }

            return GfDebugResult.Ok("Resources updated.", new Dictionary<string, object>
            {
                ["itemId"] = itemId ?? "",
                ["quantity"] = string.IsNullOrWhiteSpace(itemId) ? 0 : quantity,
                ["owned"] = owned,
                ["creditsAdded"] = Math.Max(0, credits),
                ["credits"] = CurrencyService.Credits
            });
        }

        public static GfDebugResult SetStage(string regionId, int stageIndex)
        {
            var error = RequirePlayer(out var data);
            if (error != null)
                return GfDebugResult.Fail(error);
            if (string.IsNullOrWhiteSpace(regionId))
                return GfDebugResult.Fail("region_id is required.");
            error = EnsureSandboxWrites(data);
            if (error != null)
                return GfDebugResult.Fail(error);

            WorldService.EnsureLoaded(data);
            error = AdvanceRegion(regionId.Trim(), stageIndex);
            if (error != null)
                return GfDebugResult.Fail(error);
            FeatureUnlockService.Evaluate();
            return GfDebugResult.Ok("Stage progress updated.", new Dictionary<string, object>
            {
                ["regionId"] = regionId.Trim(),
                ["stageIndex"] = stageIndex,
                ["highestClearedStage"] = HighestCleared(regionId.Trim())
            });
        }

        private static string ClaimPrerequisite(QuestDef def)
        {
            if (def == null)
                return "Missing prerequisite.";
            if (def.type != QuestType.Loop && Claimed(def.questId))
                return null;

            var signalError = ApplyProgress(def);
            if (signalError != null)
                return def.questId + ": " + signalError;
            if (def.type != QuestType.Loop && Claimed(def.questId))
                return null;

            QuestService.Evaluate(false);
            if (!QuestRules.IsCompleteWhen(QuestService.State, def, Cards()))
                return def.questId + ": completion conditions are still unmet.";

            var claim = QuestService.TryClaim(def.questId, playDialogues: false);
            if (!claim.Success && (def.type == QuestType.Loop || !Claimed(def.questId)))
                return def.questId + ": " + (string.IsNullOrEmpty(claim.Message) ? "Claim failed." : claim.Message);
            return null;
        }

        private static string ApplyProgress(QuestDef def)
        {
            if (QuestRules.IsCompleteWhen(QuestService.State, def, Cards()))
                return null;

            switch (def.questId)
            {
                case QuestIds.ObFormation:
                    QuestService.NotifyFormationReady();
                    return null;
                case QuestIds.ObFirstBattle:
                    QuestService.NotifyFirstBattleWon();
                    return null;
                case QuestIds.ObGather:
                    QuestService.NotifyGatherProgress();
                    return null;
                case QuestIds.ObCraft:
                    QuestService.NotifyCraftedOnce();
                    return null;
                case QuestIds.ObNpcShop:
                    QuestService.NotifyShopTraded();
                    return null;
                case QuestIds.ChPrologue:
                    QuestService.NotifyPrologueWon();
                    return null;
                case QuestIds.ChRecruitCole:
                    QuestService.NotifyEmergencyRecruit();
                    return null;
                case QuestIds.ChFormation:
                    SatisfyFormationLayout();
                    return QuestRules.IsCompleteWhen(QuestService.State, def, Cards())
                        ? null
                        : "Formation lesson is not met (Cole must be moved to the front row after the battle hint).";
                case QuestIds.ChOuterCleanup:
                    QuestService.NotifyOuterCleanupWon();
                    return null;
                case QuestIds.ChScanSignal:
                    QuestService.NotifyMiningSignalScanned();
                    return null;
                case QuestIds.ChMiningSpur:
                    QuestService.NotifyMiningSpurWon();
                    return null;
                case QuestIds.ChIdleGather:
                    var stageError = AdvanceRegion(WorldConstants.OuterBeltId, 2);
                    if (stageError != null)
                        return stageError;
                    QuestService.NotifyGatherCycle("node_outer_iron");
                    return null;
                case QuestIds.ChHullLeak:
                    if (!QuestRules.HasItem(EconomyConstants.RepairKitDefId)
                        && !ProductionService.TryAddLocal(ItemFactory.FromDef(EconomyConstants.RepairKitDefId, 1)))
                        return "Could not add a repair kit through the warehouse.";
                    QuestService.NotifyCraftedOnce(EconomyConstants.RepairKitDefId);
                    return null;
                case QuestIds.ChTwoRoutes:
                    return QuestService.TryChooseRoute("convoy")
                        ? null
                        : "Route choice is not available until both routes are fogged.";
                case QuestIds.ChExpeditionPrep:
                    QuestService.NotifyPendingLootClaimed();
                    return null;
                case QuestIds.ChOpenTheLock:
                    QuestService.NotifyRegionVictory(WorldConstants.FrontierBossId);
                    return null;
            }

            if (def.type == QuestType.Expedition)
                return AdvanceRegion(def.regionId, ExpeditionStageTarget(def));
            if (def.type == QuestType.Loop)
            {
                int need = Math.Max(1, def.loopTarget);
                int have = QuestRules.LoopCount(QuestService.State, def.questId);
                if (have < need)
                    QuestRules.AddLoopCount(QuestService.State, def.questId, need - have);
                QuestService.Save();
                return null;
            }

            if (def.type == QuestType.Ship && !QuestRules.ShipMeetsRegion(def.regionId))
                return "Ship gate is not met. This tool will not bypass ship upgrades.";
            if (!QuestRules.IsCompleteWhen(QuestService.State, def, Cards()))
                return "No official progress signal for " + def.questId + ".";
            return null;
        }

        private static void SatisfyFormationLayout()
        {
            var cards = Cards();
            if (cards == null || QuestRules.IsCompleteWhen(QuestService.State, QuestCatalog.Get(QuestIds.ChFormation), cards))
                return;
            if (QuestService.State?.flags != null && !QuestService.State.flags.formationLessonSeen)
            {
                QuestService.State.flags.formationLessonSeen = true;
                QuestService.Save();
            }
            string cole = null;
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (cole == null && card.characterName == CharacterName.Cole)
                    cole = card.id;
            }

            if (string.IsNullOrEmpty(cole))
                return;
            Place(cards, cole, 0);
        }

        private static void Place(IList<CardEntity> cards, string cardId, int slot)
        {
            var deck = DeckService.GetActiveCombatDeck();
            if (deck?.slotCardIds == null)
                return;
            for (int i = 0; i < deck.slotCardIds.Length; i++)
            {
                if (i != slot && deck.slotCardIds[i] == cardId)
                    DeckService.TryAssignToActiveCombat(i, "", cards);
            }

            if (deck.slotCardIds[slot] != cardId)
                DeckService.TryAssignToActiveCombat(slot, cardId, cards);
        }

        private static string AdvanceRegion(string regionId, int stageIndex)
        {
            if (string.IsNullOrEmpty(regionId))
                return "Region id is required.";
            WorldService.EnsureReady();
            var campaign = NodeCampaignCatalog.Get(regionId);
            if (campaign == null || campaign.StageCount <= 0 || RegionCatalog.Get(regionId) == null)
                return "Unknown region: " + regionId;
            if (stageIndex < 0 || stageIndex > campaign.StageCount)
                return "stage_index " + stageIndex + " is outside 0.." + campaign.StageCount + ".";

            int guard = 0;
            while (HighestCleared(regionId) < stageIndex && guard++ < 16)
            {
                int cleared = HighestCleared(regionId);
                var stage = campaign.stages[cleared];
                var encounterId = stage != null ? stage.encounterId : "";
                if (string.IsNullOrEmpty(encounterId))
                    return "Stage " + (cleared + 1) + " has no encounter.";
                var result = WorldService.RegisterBattleVictory(regionId, encounterId);
                if (!result.Success)
                    return string.IsNullOrEmpty(result.Message) ? "Victory was rejected." : result.Message;
                QuestService.NotifyRegionVictory(regionId);
                RewardService.GrantForRegionVictory(regionId, result);
                if (regionId == WorldConstants.OuterBeltId)
                    ChapterQuestService.NotifyOuterCleanupWon();
                else if (regionId == WorldConstants.MiningSpurId)
                    ChapterQuestService.NotifyMiningSpurWon();
                if (HighestCleared(regionId) <= cleared)
                    return "Stage did not advance for encounter " + encounterId + ".";
            }

            return HighestCleared(regionId) >= stageIndex
                ? null
                : "Stopped at stage " + HighestCleared(regionId) + ".";
        }

        private static int ExpeditionStageTarget(QuestDef def)
        {
            var campaign = NodeCampaignCatalog.Get(def.regionId);
            if (def.ring <= 1)
                return NodeCampaignCatalog.GatherIdleMinStage(campaign);
            if (def.ring == 2)
                return NodeCampaignCatalog.CombatIdleMinStage(campaign);
            return campaign?.StageCount ?? 1;
        }

        private static int HighestCleared(string regionId)
        {
            var runtime = WorldRules.FindRuntime(WorldService.State, regionId);
            return runtime != null ? runtime.highestClearedStage : 0;
        }

        private static string FindRegionForEncounter(string encounterId)
        {
            foreach (var region in RegionCatalog.All)
            {
                if (region == null) continue;
                if (region.mainEncounterId == encounterId || region.farmEncounterId == encounterId)
                    return region.regionId;
                var campaign = NodeCampaignCatalog.Get(region.regionId);
                if (campaign?.stages == null) continue;
                foreach (var stage in campaign.stages)
                {
                    if (stage != null && stage.encounterId == encounterId)
                        return region.regionId;
                }
            }

            return null;
        }

        private static List<QuestDef> Ancestors(QuestDef target)
        {
            var seen = new HashSet<string>();
            var list = new List<QuestDef>();
            Walk(target);
            return list;

            void Walk(QuestDef def)
            {
                if (def?.dependsOn == null) return;
                foreach (var id in def.dependsOn)
                {
                    if (string.IsNullOrEmpty(id) || !seen.Add(id)) continue;
                    var dep = QuestCatalog.Get(id);
                    if (dep == null) continue;
                    Walk(dep);
                    list.Add(dep);
                }
            }
        }

        private static bool IsCurrent(QuestDef def, QuestView view)
        {
            if (def == null || view == null)
                return false;
            if (view.Status != QuestStatus.Active && view.Status != QuestStatus.Ready)
                return false;
            var state = QuestService.State;
            if (state == null) return false;
            switch (def.type)
            {
                case QuestType.Story:
                    return state.activeStoryId == def.questId;
                case QuestType.Onboarding:
                    return state.activeOnboardingId == def.questId;
                case QuestType.Loop:
                    return state.activeLoopIds != null && state.activeLoopIds.Contains(def.questId);
                default:
                    return true;
            }
        }

        private static QuestView FindView(QuestDef def)
        {
            if (def == null) return null;
            foreach (var view in QuestService.GetViews(def.type))
            {
                if (view?.Def != null && view.Def.questId == def.questId)
                    return view;
            }

            return null;
        }

        private static bool Claimed(string questId) =>
            QuestService.State?.claimedIds != null && QuestService.State.claimedIds.Contains(questId);

        private static List<string> ClaimedIds(List<QuestDef> defs)
        {
            var ids = new List<string>();
            if (defs == null) return ids;
            foreach (var def in defs)
            {
                if (def != null && Claimed(def.questId))
                    ids.Add(def.questId);
            }

            return ids;
        }

        private static object Describe(QuestView view)
        {
            if (view?.Def == null) return null;
            return new Dictionary<string, object>
            {
                ["questId"] = view.Def.questId,
                ["titleEn"] = view.Def.titleEn ?? "",
                ["titleZh"] = view.Def.titleZh ?? "",
                ["status"] = view.Status.ToString(),
                ["conditionsMet"] = QuestRules.IsCompleteWhen(QuestService.State, view.Def, Cards()),
                ["canClaim"] = view.CanClaim,
                ["hintEn"] = view.Def.hintEn ?? "",
                ["hintZh"] = view.Def.hintZh ?? ""
            };
        }

        private static List<object> FeatureRows()
        {
            var rows = new List<object>();
            foreach (var def in FeatureUnlockCatalog.All)
            {
                if (def == null || string.IsNullOrEmpty(def.featureId)) continue;
                rows.Add(new Dictionary<string, object>
                {
                    ["featureId"] = def.featureId,
                    ["navScreen"] = def.navScreen ?? "",
                    ["unlocked"] = FeatureUnlockService.IsFeatureUnlocked(def.featureId),
                    ["titleEn"] = def.titleEn ?? ""
                });
            }

            return rows;
        }

        private static Dictionary<string, object> DeckSnapshot(DataUtil data)
        {
            DeckService.EnsureLoaded(data, Cards());
            return new Dictionary<string, object>
            {
                ["deckId"] = DeckService.GetActiveCombatDeck()?.deckId ?? "",
                ["slots"] = CurrentSlots()
            };
        }

        private static string[] CurrentSlots()
        {
            var slots = DeckRules.BattleStations(DeckService.GetActiveCombatDeck());
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    slots[i] = "";
            }

            return slots;
        }

        private static List<object> CardRows()
        {
            var rows = new List<object>();
            var cards = Cards();
            if (cards == null) return rows;
            foreach (var card in cards)
            {
                if (card == null) continue;
                rows.Add(new Dictionary<string, object>
                {
                    ["id"] = card.id ?? "",
                    ["name"] = card.characterName.ToString()
                });
            }

            return rows;
        }

        private static IList<CardEntity> Cards() => CardListManager.Instance?.cardEntities;

        private static CardEntity FindCard(IList<CardEntity> cards, string cardId)
        {
            if (cards == null) return null;
            foreach (var card in cards)
            {
                if (card != null && card.id == cardId)
                    return card;
            }

            return null;
        }

        private static string RequirePlay(out DataUtil data)
        {
            data = null;
            if (!Application.isPlaying)
                return "Not in Play Mode.";
            Application.runInBackground = true;
            data = DataUtil.Instance;
            if (data == null)
                return "DataUtil is not loaded.";
            return null;
        }

        private static string RequirePlayer(out DataUtil data)
        {
            var error = RequirePlay(out data);
            if (error != null) return error;
            if (data.currentPlayer == null || string.IsNullOrWhiteSpace(data.currentPlayer.playerID))
                return "No player loaded.";
            return null;
        }

        private static string RedirectSandbox(DataUtil data)
        {
            var sandbox = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", SandboxFolderName));
            Directory.CreateDirectory(sandbox);
            var field = typeof(DataUtil).GetField("savePath", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                return "DataUtil.savePath was not found.";
            field.SetValue(data, sandbox);
            var root = SaveRoot(data);
            if (!string.Equals(Path.GetFullPath(root ?? ""), sandbox, StringComparison.OrdinalIgnoreCase))
                return "Failed to redirect DataUtil.savePath.";
            if (IsOfficialSave(root))
                return "Sandbox path resolved inside the official saves directory.";
            return null;
        }

        private static string EnsureSandboxWrites(DataUtil data)
        {
            var error = RedirectSandbox(data);
            if (error != null)
                return error;
            var bound = BoundPlayerDirectory(data);
            if (string.IsNullOrEmpty(bound))
                return "No player save directory is bound.";
            if (IsOfficialSave(bound))
                return "Current player is bound to the official saves directory. Use gf_new_player first.";
            if (!IsSandbox(bound))
                return "Refusing to write outside the sandbox save folder.";
            return null;
        }

        private static string BoundPlayerDirectory(DataUtil data)
        {
            if (data == null) return "";
            var field = typeof(DataUtil).GetField("playerSavePath", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(data) as string ?? "";
        }

        private static string SaveRoot(DataUtil data)
        {
            if (data == null) return "";
            var field = typeof(DataUtil).GetField("savePath", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(data) as string ?? "";
        }

        public static bool IsOfficialSave(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var official = Path.GetFullPath(Path.Combine(Application.persistentDataPath, "saves"));
            var full = Path.GetFullPath(path);
            return full.StartsWith(official, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSandbox(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            return path.IndexOf(SandboxFolderName, StringComparison.OrdinalIgnoreCase) >= 0
                && !IsOfficialSave(path);
        }
    }

    public sealed class GfDebugResult
    {
        public bool Success;
        public string Message;
        public object Data;

        public static GfDebugResult Ok(string message, object data = null) =>
            new GfDebugResult { Success = true, Message = message ?? "", Data = data };

        public static GfDebugResult Fail(string message, object data = null) =>
            new GfDebugResult { Success = false, Message = message ?? "", Data = data };
    }
}

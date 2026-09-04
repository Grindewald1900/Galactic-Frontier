using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Dialogue;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using Assets.Resources.Scripts.UI.Nexus;
using UnityEngine;
namespace Assets.Resources.Scripts.ChapterQuest
{
    public static class ChapterQuestService
    {
        public const string PrologueEncounterId = "enc_prologue_sweep";
        public const string PrologueRegionId = "ch1_prologue";

        public static ChapterQuestState State { get; private set; }
        public static bool IsLoaded => State != null;
        private static string boundPlayerId;

        public static void Clear()
        {
            State = null;
            boundPlayerId = null;
        }

        public static void EnsureLoaded(DataUtil dataUtil = null)
        {
            var util = dataUtil ?? DataUtil.Instance;
            if (util == null)
            {
                State ??= CreateDefault();
                return;
            }

            var playerId = util.currentPlayer?.playerID;
            if (State != null
                && !string.IsNullOrEmpty(boundPlayerId)
                && !string.IsNullOrEmpty(playerId)
                && boundPlayerId != playerId)
            {
                Clear();
            }

            if (State != null) return;

            OnboardingService.EnsureLoaded(util);
            var loaded = util.LoadChapterQuestState();
            if (loaded == null)
            {
                State = CreateDefault();
                if (OnboardingService.IsChainComplete)
                    SkipChapterForLegacySave();
                util.SaveChapterQuestState(State, touchMeta: false);
            }
            else
            {
                State = loaded;
            }

            boundPlayerId = playerId;
            Evaluate(persist: true);
        }

        public static void Save(DataUtil dataUtil = null)
        {
            if (State == null) return;
            var util = dataUtil ?? DataUtil.Instance;
            util?.SaveChapterQuestState(State, touchMeta: true);
        }

        public static bool IsChapterComplete
        {
            get
            {
                EnsureLoaded();
                return State.chapterCompleted || State.chapterSkipped;
            }
        }

        public static ChapterQuestStepDef ActiveStep
        {
            get
            {
                EnsureLoaded();
                if (State.chapterCompleted || State.chapterSkipped) return null;
                return ChapterQuestCatalog.Get(State.activeStepId);
            }
        }

        public static List<ChapterQuestStepView> GetStepViews()
        {
            EnsureLoaded();
            Evaluate(persist: false);
            var list = new List<ChapterQuestStepView>();
            var cards = CardListManager.Instance?.cardEntities;
            foreach (var def in ChapterQuestCatalog.All)
            {
                if (def == null) continue;
                var status = ResolveStatus(def.stepId);
                list.Add(new ChapterQuestStepView
                {
                    Def = def,
                    Status = status,
                    CanClaim = status == ChapterQuestStepStatus.Active
                        && ChapterQuestRules.IsConditionMet(State, def.stepId, cards)
                        && !IsClaimed(def.stepId)
                });
            }

            return list;
        }

        public static ChapterQuestCommandResult TryClaim(string stepId)
        {
            EnsureLoaded();
            Evaluate(persist: false);

            var def = ChapterQuestCatalog.Get(stepId);
            if (def == null)
                return ChapterQuestCommandResult.Fail("Unknown chapter step.");
            if (State.chapterCompleted || State.chapterSkipped)
                return ChapterQuestCommandResult.Fail("Chapter already complete.");
            if (State.activeStepId != stepId)
                return ChapterQuestCommandResult.Fail("Step is not active.");
            if (IsClaimed(stepId))
                return ChapterQuestCommandResult.Fail("Already claimed.");
            if (!ChapterQuestRules.IsConditionMet(State, stepId, CardListManager.Instance?.cardEntities))
                return ChapterQuestCommandResult.Fail("Conditions not met yet.");

            if (!GrantRewards(def))
                return ChapterQuestCommandResult.Fail("Could not grant rewards.");

            MirrorLinkedOnboarding(def);

            if (!State.claimedStepIds.Contains(stepId))
                State.claimedStepIds.Add(stepId);
            if (!State.completedStepIds.Contains(stepId))
                State.completedStepIds.Add(stepId);

            var next = ChapterQuestCatalog.NextAfter(stepId);
            if (next == null)
            {
                State.chapterCompleted = true;
                State.activeStepId = "";
            }
            else
            {
                State.activeStepId = next.stepId;
                TryPlayDialogue(next.dialogueIdOnStart);
            }

            if (!string.IsNullOrEmpty(def.dialogueIdOnClaim))
                TryPlayDialogue(def.dialogueIdOnClaim);

            Save();
            return ChapterQuestCommandResult.Ok(next == null ? "Chapter slice complete." : $"Claimed {stepId}");
        }

        public static void Evaluate(bool persist = true)
        {
            EnsureLoaded();
            if (State.flags == null)
                State.flags = new ChapterQuestFlags();

            var cards = CardListManager.Instance?.cardEntities;
            bool changed = false;

            if (!State.flags.formationReady && ChapterQuestRules.MeetsFormationLayout(cards))
            {
                State.flags.formationReady = true;
                changed = true;
            }

            if (!State.flags.miningSignalScanned)
            {
                GridService.EnsureReady();
                if (GridService.GetState("body_mining_spur") >= GridNodeState.Located)
                {
                    State.flags.miningSignalScanned = true;
                    changed = true;
                }
            }

            if (changed && persist)
                Save();
        }

        public static void NotifyPrologueWon()
        {
            EnsureLoaded();
            State.flags ??= new ChapterQuestFlags();
            if (State.flags.prologueWon) return;
            State.flags.prologueWon = true;
            Save();
            TryAutoClaim(ChapterQuestCatalog.StepPrologue);
        }

        public static void NotifyEmergencyRecruit()
        {
            EnsureLoaded();
            State.flags ??= new ChapterQuestFlags();
            if (State.flags.emergencyRecruitDone) return;
            State.flags.emergencyRecruitDone = true;
            Save();
        }

        public static void NotifyOuterCleanupWon()
        {
            EnsureLoaded();
            State.flags ??= new ChapterQuestFlags();
            if (State.flags.outerCleanupWon) return;
            State.flags.outerCleanupWon = true;
            Save();
            if (State.activeStepId == ChapterQuestCatalog.StepOuterCleanup)
            {
                TryPlayDialogue("ch1_mita_strategy");
                ShowStrategyCompareIfNeeded();
            }
        }

        public static void NotifyMiningSpurWon()
        {
            EnsureLoaded();
            State.flags ??= new ChapterQuestFlags();
            if (State.flags.miningSpurWon) return;
            State.flags.miningSpurWon = true;
            Save();
        }

        public static void NotifyMiningSignalScanned()
        {
            EnsureLoaded();
            State.flags ??= new ChapterQuestFlags();
            if (State.flags.miningSignalScanned) return;
            State.flags.miningSignalScanned = true;
            Save();
        }

        public static void TryBeginEntryFlow(System.Action onReady)
        {
            EnsureLoaded();
            if (IsChapterComplete) { onReady?.Invoke(); return; }

            if (State.activeStepId != ChapterQuestCatalog.StepPrologue || State.flags.prologueWon)
            {
                TryPlayActiveStartDialogue();
                onReady?.Invoke();
                return;
            }

            EnsureStarterCards();
            var def = ChapterQuestCatalog.Get(ChapterQuestCatalog.StepPrologue);
            if (def != null && !string.IsNullOrEmpty(def.dialogueIdOnStart))
            {
                DialogueService.Play(def.dialogueIdOnStart, () =>
                {
                    StartPrologueBattle();
                    onReady?.Invoke();
                });
            }
            else
            {
                StartPrologueBattle();
                onReady?.Invoke();
            }
        }

        public static void StartPrologueBattle()
        {
            EnsureStarterCards();
            DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance?.cardEntities);
            AssignStarterLineup();

            Battle.BattleController.PendingBattleTargetId = PrologueRegionId;
            Battle.BattleController.PendingEncounterId = PrologueEncounterId;
            Battle.BattleController.PendingIsChapterPrologue = true;
            Battle.BattleController.PendingReturnScreen = AppScreen.Bridge;
            Battle.BattleController.PendingBattleSeed =
                unchecked(PrologueRegionId.GetHashCode() * 1_000_003L);

            LoadingOverlay.LoadScene(nameof(Scene.SceneLoader.SceneName.BattleScene));
        }

        public static void EnsureStarterCards()
        {
            if (CardListManager.Instance == null || CardDataManager.Instance == null)
                return;

            GrantStarterIfMissing(CharacterName.Asra);
            GrantStarterIfMissing(CharacterName.Magki);
            GrantStarterIfMissing(CharacterName.Sernia);
            DataUtil.Instance?.SaveCardData(CardListManager.Instance.cardEntities);
        }

        public static bool ShouldShowStrategyCompare()
        {
            EnsureLoaded();
            return State.activeStepId == ChapterQuestCatalog.StepOuterCleanup
                && State.flags.outerCleanupWon
                && !State.flags.strategyCompareShown;
        }

        public static void MarkStrategyCompareShown()
        {
            EnsureLoaded();
            if (State.flags.strategyCompareShown) return;
            State.flags.strategyCompareShown = true;
            Save();
        }

        private static void ShowStrategyCompareIfNeeded()
        {
            if (!ShouldShowStrategyCompare()) return;
            MarkStrategyCompareShown();
            StrategyComparePopup.Show();
        }

        private static void TryAutoClaim(string stepId)
        {
            if (State.activeStepId != stepId) return;
            if (!ChapterQuestRules.IsConditionMet(State, stepId, CardListManager.Instance?.cardEntities))
                return;
            if (IsClaimed(stepId)) return;
            TryClaim(stepId);
        }

        private static void TryPlayActiveStartDialogue()
        {
            var def = ActiveStep;
            if (def == null || string.IsNullOrEmpty(def.dialogueIdOnStart)) return;
            if (IsClaimed(def.stepId)) return;
            if (Assets.Resources.Scripts.UI.Nexus.Tutorial.TutorialGuideService
                .ShouldSuppressFullscreenDialogue(def.stepId))
                return;
            TryPlayDialogue(def.dialogueIdOnStart);
        }

        private static void TryPlayDialogue(string dialogueId)
        {
            if (string.IsNullOrEmpty(dialogueId)) return;
            if (Dialogue.DialogueService.IsPlaying) return;
            DialogueService.Play(dialogueId);
        }

        private static void AssignStarterLineup()
        {
            var deck = DeckService.GetActiveCombatDeck();
            if (deck == null) return;
            var cards = CardListManager.Instance?.cardEntities;
            if (cards == null) return;

            int slot = 0;
            foreach (var name in new[] { CharacterName.Asra, CharacterName.Magki, CharacterName.Sernia })
            {
                var entity = FindOwned(cards, name);
                if (entity == null) continue;
                DeckService.TryAssignToActiveCombat(slot, entity.id, cards);
                slot++;
            }
        }

        private static void GrantStarterIfMissing(CharacterName name)
        {
            var cards = CardListManager.Instance.cardEntities;
            if (ChapterQuestRules.OwnsCharacter(cards, name)) return;
            var character = CharacterSkillController.GetCharacter(name);
            if (character == null) return;
            var entity = CardDataManager.Instance.GetGachaCardEntity(character);
            CardListManager.Instance.AddCardEntity(entity);
        }

        private static CardEntity FindOwned(IList<CardEntity> cards, CharacterName name)
        {
            if (cards == null) return null;
            foreach (var c in cards)
            {
                if (c != null && c.characterName == name)
                    return c;
            }

            return null;
        }

        private static void SkipChapterForLegacySave()
        {
            State.chapterSkipped = true;
            State.chapterCompleted = true;
            State.activeStepId = "";
            foreach (var def in ChapterQuestCatalog.All)
            {
                if (def == null) continue;
                if (!State.completedStepIds.Contains(def.stepId))
                    State.completedStepIds.Add(def.stepId);
                if (!State.claimedStepIds.Contains(def.stepId))
                    State.claimedStepIds.Add(def.stepId);
            }
        }

        private static void MirrorLinkedOnboarding(ChapterQuestStepDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.linkedOnboardingStepId)) return;
            switch (def.linkedOnboardingStepId)
            {
                case Onboarding.Domain.OnboardingCatalog.StepFormation:
                    OnboardingService.NotifyFormationReady();
                    break;
                case Onboarding.Domain.OnboardingCatalog.StepFirstBattle:
                    OnboardingService.NotifyFirstBattleWon();
                    break;
            }
        }

        private static ChapterQuestState CreateDefault() =>
            new ChapterQuestState
            {
                chapterId = ChapterQuestCatalog.ChapterId,
                activeStepId = ChapterQuestCatalog.StepPrologue,
                completedStepIds = new List<string>(),
                claimedStepIds = new List<string>(),
                flags = new ChapterQuestFlags()
            };

        private static bool IsClaimed(string stepId) =>
            State?.claimedStepIds != null && State.claimedStepIds.Contains(stepId);

        private static ChapterQuestStepStatus ResolveStatus(string stepId)
        {
            if (State.completedStepIds != null && State.completedStepIds.Contains(stepId))
                return ChapterQuestStepStatus.Completed;
            if (State.chapterCompleted || State.chapterSkipped)
                return ChapterQuestStepStatus.Completed;
            if (State.activeStepId == stepId)
                return ChapterQuestStepStatus.Active;
            return ChapterQuestStepStatus.Locked;
        }

        private static bool GrantRewards(ChapterQuestStepDef def)
        {
            if (def?.rewards == null) return true;
            foreach (var reward in def.rewards)
            {
                if (reward == null) continue;
                if (reward.kind == "credits" || reward.credits > 0)
                {
                    if (reward.credits > 0)
                        CurrencyService.AddCredits(reward.credits);
                    continue;
                }

                if (reward.kind == "item" && !string.IsNullOrEmpty(reward.itemDefId))
                {
                    var item = ItemFactory.FromDef(
                        reward.itemDefId,
                        Mathf.Max(1, reward.quantity),
                        reward.quality > 0 ? reward.quality : EconomyConstants.DefaultQuality);
                    if (!ProductionService.TryAddLocal(item))
                        return false;
                }
            }

            return true;
        }
    }
}

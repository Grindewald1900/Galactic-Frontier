using System.Collections.Generic;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Dialogue;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Quest;
using Assets.Resources.Scripts.Quest.Domain;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.Save;
using Assets.Resources.Scripts.UI.Nexus;
using Assets.Resources.Scripts.UI.Nexus.Tutorial;
using UnityEngine;

namespace Assets.Resources.Scripts.ChapterQuest
{
    public static class ChapterQuestService
    {
        public const string PrologueEncounterId = "enc_prologue_sweep";
        public const string PrologueRegionId = "ch1_prologue";

        public static ChapterQuestState State { get; private set; }
        public static bool IsLoaded => State != null;

        public static void Bind(ChapterQuestState state) => State = state;

        public static void Clear() => State = null;

        public static void EnsureLoaded(DataUtil dataUtil = null) =>
            QuestService.EnsureLoaded(dataUtil);

        public static void Save(DataUtil dataUtil = null) =>
            QuestService.Save(dataUtil);

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
            var list = new List<ChapterQuestStepView>();
            foreach (var view in QuestService.GetViews(QuestType.Story))
            {
                if (view?.Def == null) continue;
                var def = ChapterQuestCatalog.Get(view.Def.questId);
                if (def == null) continue;
                list.Add(new ChapterQuestStepView
                {
                    Def = def,
                    Status = MapStatus(view.Status),
                    CanClaim = view.CanClaim
                });
            }

            return list;
        }

        public static ChapterQuestCommandResult TryClaim(string stepId)
        {
            var r = QuestService.TryClaim(stepId);
            return r.Success
                ? ChapterQuestCommandResult.Ok(r.Message)
                : ChapterQuestCommandResult.Fail(r.Message);
        }

        public static void Evaluate(bool persist = true) =>
            QuestService.Evaluate(persist);

        public static void NotifyPrologueWon() =>
            QuestService.NotifyPrologueWon();

        public static void NotifyEmergencyRecruit() =>
            QuestService.NotifyEmergencyRecruit();

        public static void NotifyOuterCleanupWon()
        {
            QuestService.NotifyOuterCleanupWon();
            if (State != null && State.activeStepId == ChapterQuestCatalog.StepOuterCleanup)
            {
                TryPlayDialogue("ch1_mita_strategy");
                ShowStrategyCompareIfNeeded();
            }
        }

        public static void NotifyMiningSpurWon() =>
            QuestService.NotifyMiningSpurWon();

        public static void NotifyMiningSignalScanned() =>
            QuestService.NotifyMiningSignalScanned();

        /// <summary>
        /// First-load onboarding. Plays <c>ch1_wake</c> once, then starts the prologue fight.
        /// If wake already played (save load / return from battle), stays on the Nexus so
        /// Return to Bridge cannot immediately re-enter the settlement report.
        /// </summary>
        /// <returns>True when BattleScene is being loaded and MainScene should keep the loading cover.</returns>
        public static bool TryBeginEntryFlow(System.Action onReady)
        {
            EnsureLoaded();
            if (IsChapterComplete)
            {
                onReady?.Invoke();
                return false;
            }

            if (State.flags != null && State.flags.prologueWon)
            {
                // Next quest is already accepted. Reward stays unclaimed until the player claims it.
                PresentPendingChapterDialogue();
                onReady?.Invoke();
                return false;
            }

            if (State.activeStepId != ChapterQuestCatalog.StepPrologue)
            {
                PresentPendingChapterDialogue();
                onReady?.Invoke();
                return false;
            }

            if (!PrepareStarterFleet())
            {
                Debug.LogError("[CHAPTER] Starter fleet was empty; prologue battle aborted.");
                onReady?.Invoke();
                return false;
            }

            var wakeId = ChapterQuestCatalog.Get(ChapterQuestCatalog.StepPrologue)?.dialogueIdOnStart;
            if (!string.IsNullOrEmpty(wakeId) && !DialogueService.HasPlayed(wakeId))
            {
                DialogueService.Play(wakeId, () =>
                {
                    StartPrologueBattle();
                    onReady?.Invoke();
                });
                return false;
            }

            onReady?.Invoke();
            return false;
        }

        public static bool PrepareStarterFleet()
        {
            GameSaveService.PushImmediate();
            try
            {
                EnsureStarterCards();
                if (DataUtil.Instance == null || CardListManager.Instance == null)
                    return false;

                DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance.cardEntities);
                AssignStarterLineup();
                DataUtil.Instance.SaveCardData(CardListManager.Instance.cardEntities);
                DeckService.Save(DataUtil.Instance);
                var deck = DeckService.GetActiveCombatDeck();
                int members = deck?.MemberCount ?? 0;
                if (members < 1)
                    Debug.LogError("[CHAPTER] PrepareStarterFleet: combat deck has no members.");
                else
                    Debug.Log($"[CHAPTER] Starter fleet ready ({members} members).");
                return members >= 1;
            }
            finally
            {
                GameSaveService.PopImmediate();
            }
        }

        public static void StartPrologueBattle()
        {
            if (!PrepareStarterFleet())
            {
                Debug.LogError("[CHAPTER] Cannot start prologue battle without a starter fleet.");
                return;
            }

            BattleController.PendingBattleTargetId = PrologueRegionId;
            BattleController.PendingEncounterId = PrologueEncounterId;
            BattleController.PendingIsChapterPrologue = true;
            BattleController.PendingSpectateAutoCombat = false;
            BattleController.PendingReturnScreen = AppScreen.Bridge;
            BattleController.PendingBattleSeed =
                unchecked(PrologueRegionId.GetHashCode() * 1_000_003L);
            BattleController.PendingPlayerParty = CopyParty(
                DeckService.GetActiveCombatMembers(CardListManager.Instance?.cardEntities));

            LiveBattleSession.Clear();
            BackgroundBattleHost.Ensure().ReleasePresentation();

            LoadingOverlay.LoadScene(nameof(SceneLoader.SceneName.BattleScene));
        }

        public static void EnsureStarterCards()
        {
            if (CardListManager.Instance == null || CardDataManager.Instance == null)
            {
                Debug.LogWarning("[CHAPTER] EnsureStarterCards skipped — card managers not ready.");
                return;
            }

            GrantStarterIfMissing(CharacterName.Asra);
            GrantStarterIfMissing(CharacterName.Magki);
            GrantStarterIfMissing(CharacterName.Sernia);
            DataUtil.Instance?.SaveCardData(CardListManager.Instance.cardEntities);
        }

        public static void PresentPendingChapterDialogue()
        {
            if (DialogueService.IsPlaying) return;

            foreach (var def in ChapterQuestCatalog.All)
            {
                if (def == null || string.IsNullOrEmpty(def.dialogueIdOnClaim)) continue;
                if (State?.claimedStepIds == null || !State.claimedStepIds.Contains(def.stepId))
                    continue;
                if (!DialogueService.HasPlayed(def.dialogueIdOnClaim))
                {
                    DialogueService.Play(def.dialogueIdOnClaim);
                    return;
                }
            }

            TryPlayActiveStartDialogue();
        }

        public static bool ShouldShowStrategyCompare()
        {
            EnsureLoaded();
            return State.activeStepId == ChapterQuestCatalog.StepOuterCleanup
                && State.flags.outerCleanupWon
                && !State.flags.strategyCompareShown;
        }

        public static void MarkStrategyCompareShown() =>
            QuestService.MarkStrategyCompareShown();

        private static void ShowStrategyCompareIfNeeded()
        {
            if (!ShouldShowStrategyCompare()) return;
            MarkStrategyCompareShown();
            StrategyComparePopup.Show();
        }

        private static void TryPlayActiveStartDialogue()
        {
            var def = ActiveStep;
            if (def == null || string.IsNullOrEmpty(def.dialogueIdOnStart)) return;
            if (State?.claimedStepIds != null && State.claimedStepIds.Contains(def.stepId)) return;
            if (DialogueService.HasPlayed(def.dialogueIdOnStart)) return;
            if (TutorialGuideService.ShouldSuppressFullscreenDialogue(def.stepId))
                return;
            DialogueService.Play(def.dialogueIdOnStart);
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

            if (deck.IsActionBusy)
            {
                var stop = DeckService.TryStop(deck.deckId);
                if (!stop.Success)
                {
                    Debug.LogWarning("[CHAPTER] Could not free combat deck for starter lineup: " + stop.Message);
                    return;
                }
            }

            // Asra starts in the back row so recruiting Cole does not finish Patchwork Fleet.
            // Front row (0–1) stays empty until the player moves Cole forward after the lesson battle.
            PlaceStarter(cards, CharacterName.Asra, 2);
            PlaceStarter(cards, CharacterName.Magki, 3);
            PlaceStarter(cards, CharacterName.Sernia, 4);
        }

        private static void PlaceStarter(IList<CardEntity> cards, CharacterName name, int slot)
        {
            var entity = FindOwned(cards, name);
            if (entity == null) return;
            var result = DeckService.TryAssignToActiveCombat(slot, entity.id, cards);
            if (!result.Success)
                Debug.LogWarning($"[CHAPTER] Starter slot {slot} ({name}) failed: {result.Message}");
        }

        private static void GrantStarterIfMissing(CharacterName name)
        {
            var cards = CardListManager.Instance.cardEntities;
            if (ChapterQuestRules.OwnsCharacter(cards, name)) return;
            var character = CharacterSkillController.GetCharacter(name);
            if (character == null)
            {
                Debug.LogError("[CHAPTER] Missing character template: " + name);
                return;
            }
            var entity = CardDataManager.Instance.GetGachaCardEntity(character);
            if (entity == null)
            {
                Debug.LogError("[CHAPTER] Failed to create starter card: " + name);
                return;
            }
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

        private static List<CardEntity> CopyParty(IList<CardEntity> members)
        {
            if (members == null || members.Count == 0)
                return new List<CardEntity>();
            var list = members as List<CardEntity> ?? new List<CardEntity>(members);
            var copies = ListDeepCopyUtil.DeepCopyViaJson(list);
            return copies != null && copies.Count > 0 ? copies : list;
        }

        private static ChapterQuestStepStatus MapStatus(QuestStatus status) =>
            status == QuestStatus.Claimed
                ? ChapterQuestStepStatus.Completed
                : status == QuestStatus.Locked
                    ? ChapterQuestStepStatus.Locked
                    : ChapterQuestStepStatus.Active;
    }
}

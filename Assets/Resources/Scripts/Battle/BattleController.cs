using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using System.Collections;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.Save;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.UI.Nexus;
using Assets.Resources.Scripts.Battle.Domain;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.World.Domain;
using TMPro;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.Battle
{
    /// <summary>
    /// Orchestrates battle initialization, initiative order, attacks, victory checks, and reporting.
    /// Player formation entities are deep-copied before combat so transient health and modifiers do
    /// not mutate the persistent collection owned by CardListManager.
    /// </summary>
    /// <remarks>
    /// Character implementations perform individual attacks; this controller owns round ordering
    /// and selects whether a normal or energy-gated special attack runs.
    /// </remarks>
    public class BattleController : MonoBehaviour
    {
        #region Variables
        private const int MaxRound = 15;
        private int currentRound = 0;

        /// <summary>Current auto-battle round; exposed for BattleChrome HUD (Option A).</summary>
        public int CurrentRound => currentRound;
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static BattleController Instance { get; private set; }
        [SerializeField] private List<Card> playerCards;
        [SerializeField] private List<Card> enemyCards;
        private List<CardEntity> inlineEntities = new();
        [SerializeField] private BattleController enemyController;

        public GameObject reportPanel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI reportTitle;

        /// <summary>Outcome of the last finished battle; set before the report opens.</summary>
        public BattleOutcome LastOutcome { get; private set; } = BattleOutcome.None;

        /// <summary>Seed used for this battle's hit/crit rolls.</summary>
        public long BattleSeed { get; private set; }

        /// <summary>Optional seed set before loading BattleScene (e.g. Explore / AFK).</summary>
        public static long? PendingBattleSeed { get; set; }

        /// <summary>Optional combat target id (region) for MainCombat occupation / progress.</summary>
        public static string PendingBattleTargetId { get; set; }

        /// <summary>Encounter id for enemy party load (P2.3).</summary>
        public static string PendingEncounterId { get; set; }

        /// <summary>Chapter 1 prologue battle — skips world region registration.</summary>
        public static bool PendingIsChapterPrologue { get; set; }

        /// <summary>Screen to open when leaving BattleScene (defaults to Explore).</summary>
        public static AppScreen? PendingReturnScreen { get; set; }

        [Tooltip("Non-zero overrides PendingBattleSeed / auto seed (Editor testing).")]
        [SerializeField] private long battleSeedOverride;

        private BattleRng battleRng;
        private bool reportConfirmWired;
        private bool progressApplied;
        private CombatStrategyId activeStrategy = CombatStrategyId.Balanced;
        #endregion

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }
        private void Start()
        {
            Init();
            WireReportConfirm();
            StartBattle();
        }

        private void OnEnable()
        {
            if (GameStatusManager.Instance != null)
                GameStatusManager.Instance.CurrentScene = CurrentScene.BATTLE_SCENE;
            if (reportPanel != null)
                reportPanel.SetActive(false);
        }

        #region Initialization 🚀 
        private void Init()
        {
            // Prefer the cross-scene collection, but support direct BattleScene entry from a save.
            CharacterSkillController.InitSkillSet();
            var cardListMgr = CardListManager.Instance;
            Debug.Log("inLine counts cardListMgr " + (cardListMgr != null));
            if (cardListMgr != null)
            {
                if (DataUtil.Instance != null && !DeckService.IsLoaded)
                    DeckService.EnsureLoaded(DataUtil.Instance, cardListMgr.cardEntities);
                inlineEntities = ListDeepCopyUtil.DeepCopyViaJson(cardListMgr.GetInLineCardEntities());
            }
            else if (DataUtil.Instance != null)
            {
                var loaded = DataUtil.Instance.LoadCardData();
                DeckService.EnsureLoaded(DataUtil.Instance, loaded);
                inlineEntities = ListDeepCopyUtil.DeepCopyViaJson(DeckService.GetActiveCombatMembers(loaded));
            }
            else
            {
                inlineEntities = new List<CardEntity>();
            }
            HideCards(playerCards);
            HideCards(enemyCards);

            if (DeckService.IsLoaded)
            {
                var deck = DeckService.GetActiveCombatDeck();
                activeStrategy = CombatStrategyRules.Parse(deck?.combatStrategyId);
            }

            var encounter = EncounterCatalog.Get(PendingEncounterId);
            if (encounter == null && !string.IsNullOrEmpty(PendingBattleTargetId))
            {
                var region = RegionCatalog.Get(PendingBattleTargetId);
                if (region != null)
                    encounter = EncounterCatalog.Get(region.mainEncounterId);
            }

            if (encounter != null)
            {
                var party = EncounterPartyBuilder.Build(encounter);
                for (var i = 0; i < party.Count && i < enemyCards.Count; i++)
                    SetCard(enemyCards, i, party[i], false);
                Debug.Log($"[BATTLE] Loaded encounter {encounter.encounterId} enemies={party.Count}");
            }
            else if (DevData.IsActive)
            {
                var enemies = DevData.Current.CreateSampleEnemyParty(Mathf.Min(enemyCards.Count, 5));
                for (var i = 0; i < enemies.Count && i < enemyCards.Count; i++)
                    SetCard(enemyCards, i, enemies[i], false);
            }
            else
            {
                DevData.LogSkipped(nameof(BattleController) + ".CreateSampleEnemyParty");
                Debug.LogWarning("[BATTLE] No encounter configured and Dev Data off — enemy slots empty.");
            }

            if (inlineEntities != null)
            {
                Debug.Log("inLine counts" + inlineEntities.Count);
                foreach (var entity in inlineEntities)
                    SetCard(playerCards, (int)entity.GetLineupPosition(), entity, true);
            }
        }
        #endregion

        /// <summary>
        /// Calculates damage entity between attacker and defender using the battle seed RNG.
        /// </summary>
        public DamageEntity CalculateDamage(Card playerCard, Card enemyCard, float attackMultiplier = 1)
        {
            if (playerCard?.cardEntity == null || enemyCard?.cardEntity == null)
                return new DamageEntity(0, DamageType.DAMAGE, 1);

            EnsureBattleRng();
            var pEntity = playerCard.cardEntity;
            var eEntity = enemyCard.cardEntity;
            var roll = CombatMath.ResolveAttack(
                attackerAccuracy: pEntity.Accuracy,
                defenderDodge: eEntity.Dodge,
                criticalChance: pEntity.Critical,
                criticalDamageMultiplier: pEntity.CriticalDamage,
                battleAttack: pEntity.GetBattleAttack(),
                battleDefense: eEntity.GetBattleDefense(),
                fixedDamageReduction: eEntity.DamageReduction,
                attackMultiplier: attackMultiplier,
                rng: battleRng);

            return new DamageEntity(roll.Damage, DamageType.DAMAGE, roll.CriticalMultiplier);
        }

        /// <summary>
        /// Calculates defender's damage reduction rate.
        /// </summary>
        public float CalculateDmgReductionRate(CardEntity eEntity)
        {
            if (eEntity == null) return 0;
            return CombatMath.DamageReductionRate(eEntity.GetBattleDefense(), eEntity.DamageReduction);
        }

        /// <summary>
        /// Initializes a card in the specified list at the given index.
        /// </summary>
        public void SetCard(List<Card> target, int index, CardEntity cardEntity, bool isPlayer)
        {
            Debug.Log("Setting Card: " + index + ", CardEntity: " + cardEntity?.cardName + " isPlayer: " + isPlayer);

            if (target == null || index < 0 || index >= target.Count || target[index] == null || cardEntity == null) return;
            var card = target[index];
            card.gameObject.SetActive(true);
            card.isPlayerCard = isPlayer;
            card.position = index + 1;
            card.InitCard(cardEntity);
        }

        private void HideCards(List<Card> cards)
        {
            if (cards == null) return;
            foreach (var card in cards)
            {
                if (card != null && card.gameObject != null)
                    card.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Deactivates and removes a card from a target list.
        /// </summary>
        /// <param name="target">List the card belongs to.</param>
        /// <param name="card">The card to remove.</param>
        public void RemoveCard(List<Card> target, Card card)
        {
            if (target == null || card == null) return;
            if (target.Remove(card))
                card.gameObject?.SetActive(false);
        }

        /// <summary>
        /// Starts the battle if not already active.
        /// </summary>
        public void StartBattle()
        {
            if (GameStatusManager.Instance.IsBattle) return;
            GameStatusManager.Instance.IsBattle = true;
            currentRound = 0;
            LastOutcome = BattleOutcome.None;
            progressApplied = false;
            BeginSeededBattle();
            EnsureMainCombatOccupation();
            StartCoroutine(BattleRoutine());
        }

        private void EnsureMainCombatOccupation()
        {
            if (!DeckService.IsLoaded)
                return;

            var deck = DeckService.GetActiveCombatDeck();
            if (deck == null)
                return;

            if (deck.IsActionBusy && deck.action.actionType == DeckActionType.MainCombat)
                return;

            var cards = CardListManager.Instance != null
                ? CardListManager.Instance.cardEntities
                : null;
            var result = DeckService.TryStart(
                deck.deckId,
                DeckActionType.MainCombat,
                PendingBattleTargetId ?? "",
                cards);
            if (!result.Success)
                Debug.LogWarning("[DECK] MainCombat start: " + result.Message + " (" + result.Error + ")");
        }

        private void BeginSeededBattle()
        {
            if (battleSeedOverride != 0)
                BattleSeed = battleSeedOverride;
            else if (PendingBattleSeed.HasValue)
            {
                BattleSeed = PendingBattleSeed.Value;
                PendingBattleSeed = null;
            }
            else
                BattleSeed = DateTime.UtcNow.Ticks;

            battleRng = new BattleRng(BattleSeed);
            Debug.Log($"[BATTLE] Seed={BattleSeed}");
        }

        private void EnsureBattleRng()
        {
            if (battleRng != null)
                return;
            BeginSeededBattle();
        }

        /// <summary>
        /// Runs the battle state machine until one side is defeated or MaxRound is reached.
        /// Initiative is rebuilt every round from living cards and ordered by speed.
        /// </summary>
        private IEnumerator BattleRoutine()
        {
            // No opposing party (e.g. encounter table not ready): resolve immediately so the report/return path works.
            if (!HasAliveCards(enemyCards) || !HasAliveCards(playerCards))
            {
                LastOutcome = BattleOutcomeRules.Resolve(
                    HasAliveCards(playerCards),
                    HasAliveCards(enemyCards),
                    reachedRoundCap: false);
                BattleInfo.Instance?.PlayBattleInfoAnimation(
                    LastOutcome == BattleOutcome.Victory ? "Victory" : "Defeated");
                GameStatusManager.Instance.IsBattle = false;
                if (BattleInfo.Instance != null)
                {
                    BattleInfo.Instance.PlayBattleInfoAnimation("Battle End");
                    yield return new WaitUntil(() => !BattleInfo.Instance.isBattleInfoActive);
                }
                yield return new WaitForSeconds(0.5f);
                ShowBattleReport();
                yield break;
            }

            while (currentRound < MaxRound && GameStatusManager.Instance.IsBattle)
            {
                currentRound++;

                // Start of battle
                if (currentRound == 1)
                {
                    if (BattleInfo.Instance != null)
                    {
                        BattleInfo.Instance.PlayBattleInfoAnimation("Battle Start");
                        yield return new WaitUntil(() => !BattleInfo.Instance.isBattleInfoActive);
                    }
                }

                if (BattleInfo.Instance != null)
                {
                    BattleInfo.Instance.PlayBattleInfoAnimation($"Round {currentRound}");
                    yield return new WaitUntil(() => !BattleInfo.Instance.isBattleInfoActive);
                }

                var allCards = playerCards.Concat(enemyCards)
                    .Where(card => card != null && card.IsAlive())
                    .OrderByDescending(card => card.cardEntity.Speed)
                    .ToList();

                foreach (var card in allCards)
                {
                    Debug.Log($"Card: {card.name} isAlive: {card.IsAlive()}");
                    if (!card.IsAlive()) continue;

                    var targets = GetAliveTargets(card);
                    Debug.Log("target count: " + targets.Count);
                    if (targets.Count == 0)
                    {
                        LastOutcome = BattleOutcomeRules.Resolve(
                            HasAliveCards(playerCards),
                            HasAliveCards(enemyCards),
                            reachedRoundCap: false);
                        GameStatusManager.Instance.IsBattle = false;
                        break;
                    }

                    var character = CharacterSkillController.GetCharacter(card.cardEntity.characterName);
                    if (character == null)
                    {
                        Debug.LogError($"character not found for character: {card.cardEntity.characterName}");
                        continue;
                    }

                    card.Highlight(DefaultProperty.highlightCardScale);

                    if (card.IsMaxEnergy())
                    {
                        Debug.Log("Max energy, using special attack.");
                        card.ResetEnergyBar();
                        yield return character.SpecialAttack(card, targets);
                    }
                    else
                    {
                        Debug.Log("Using normal attack.");
                        card.UpdateEnergyBar(30f);
                        yield return character.NormalAttack(card, targets);
                    }

                    card.Unhighlight(DefaultProperty.defaultCardScale);

                    if (!HasAliveCards(playerCards) || !HasAliveCards(enemyCards))
                    {
                        LastOutcome = BattleOutcomeRules.Resolve(
                            HasAliveCards(playerCards),
                            HasAliveCards(enemyCards),
                            reachedRoundCap: false);
                        BattleInfo.Instance?.PlayBattleInfoAnimation(
                            LastOutcome == BattleOutcome.Victory ? "Victory" : "Defeated");
                        GameStatusManager.Instance.IsBattle = false;
                        break;
                    }
                }

                yield return new WaitForSeconds(0.5f);

                if (!GameStatusManager.Instance.IsBattle) break;
            }

            if (GameStatusManager.Instance.IsBattle)
            {
                LastOutcome = BattleOutcomeRules.Resolve(
                    HasAliveCards(playerCards),
                    HasAliveCards(enemyCards),
                    reachedRoundCap: true);
                BattleInfo.Instance?.PlayBattleInfoAnimation("Max Rounds Reached");
            }

            if (BattleInfo.Instance != null)
            {
                BattleInfo.Instance.PlayBattleInfoAnimation("Battle End");
                yield return new WaitUntil(() => !BattleInfo.Instance.isBattleInfoActive);
            }
            GameStatusManager.Instance.IsBattle = false;
            yield return new WaitForSeconds(0.5f);

            ShowBattleReport();
        }

        /// <summary>
        /// Checks if any card in list is alive.
        /// </summary>
        private bool HasAliveCards(List<Card> cards)
        {
            return cards != null && cards.Any(card => card != null && card.IsAlive());
        }

        /// <summary>
        /// Gets all alive enemy targets for a given card.
        /// </summary>
        public List<Card> GetAliveTargets(Card card)
        {
            if (card == null || !card.IsAlive())
                return new List<Card>();

            var targets = card.isPlayerCard ? enemyCards : playerCards;
            if (targets == null)
                return new List<Card>();

            var alive = targets.Where(t => t != null && t.IsAlive()).ToList();
            if (!card.isPlayerCard || alive.Count <= 1)
                return alive;

            // P2.5: reorder so preferred target is first (attackers typically hit targets[0]).
            var snaps = new List<CombatantSnapshot>();
            foreach (var t in alive)
            {
                snaps.Add(new CombatantSnapshot
                {
                    Id = t.cardEntity?.id,
                    CurrentHp = t.cardEntity?.Health ?? 0f,
                    MaxHp = t.cardEntity?.Health ?? 1f,
                    Power = t.cardEntity?.power ?? 0f,
                    IsPlayer = false
                });
            }

            var pick = CombatStrategyRules.PickEnemyTargetIndex(activeStrategy, snaps);
            if (pick > 0 && pick < alive.Count)
            {
                var preferred = alive[pick];
                alive.RemoveAt(pick);
                alive.Insert(0, preferred);
            }

            return alive;
        }

        /// <summary>
        /// Shows the battle report. Confirm button returns to MainScene Explore.
        /// </summary>
        private void ShowBattleReport()
        {
            Debug.Log($"{GetType().Name} ShowBattleReport outcome={LastOutcome}");
            ApplyWorldProgressOnVictory();
            WireReportConfirm();

            if (reportPanel != null)
                reportPanel.SetActive(true);

            ApplyReportTitle();

            if (BattleReportManager.Instance != null)
            {
                BattleReportManager.Instance.RefreshChart(ChartType.pDamageChart, playerCards);
                BattleReportManager.Instance.RefreshChart(ChartType.pInjuryChart, playerCards);
                BattleReportManager.Instance.RefreshChart(ChartType.pHealChart, playerCards);
            }
        }

        private void ApplyWorldProgressOnVictory()
        {
            if (progressApplied || LastOutcome != BattleOutcome.Victory)
                return;
            progressApplied = true;

            if (PendingIsChapterPrologue)
            {
                ChapterQuestService.NotifyPrologueWon();
                PendingIsChapterPrologue = false;
                PendingBattleTargetId = null;
                PendingEncounterId = null;
                return;
            }

            var regionId = PendingBattleTargetId;
            var encounterId = PendingEncounterId;
            if (string.IsNullOrEmpty(regionId))
                return;

            if (DataUtil.Instance != null)
            {
                WorldService.EnsureLoaded(DataUtil.Instance);
                ShipService.EnsureLoaded(DataUtil.Instance);
            }

            if (string.IsNullOrEmpty(encounterId))
            {
                var cfg = RegionCatalog.Get(regionId);
                encounterId = cfg?.mainEncounterId;
            }

            var result = WorldService.RegisterBattleVictory(regionId, encounterId);
            Debug.Log($"[WORLD] Victory registered region={regionId} enc={encounterId} ok={result.Success} first={result.WasFirstClear} {result.Message}");
            if (result.Success)
            {
                Assets.Resources.Scripts.Onboarding.OnboardingService.NotifyFirstBattleWon();
                RewardService.GrantForRegionVictory(regionId, result.WasFirstClear);
                if (regionId == WorldConstants.OuterBeltId)
                    ChapterQuestService.NotifyOuterCleanupWon();
                else if (regionId == WorldConstants.MiningSpurId)
                    ChapterQuestService.NotifyMiningSpurWon();
            }
            Assets.Resources.Scripts.Economy.DurabilityService.ApplyCombatWearToEquipped(
                CardListManager.Instance?.cardEntities);
        }

        private void WireReportConfirm()
        {
            if (reportConfirmWired)
                return;

            if (confirmButton == null && reportPanel != null)
            {
                var confirmTransform = reportPanel.transform.Find("ConfirmButton");
                if (confirmTransform != null)
                    confirmButton = confirmTransform.GetComponent<Button>();
            }

            if (confirmButton == null)
            {
                Debug.LogWarning($"{GetType().Name}: ConfirmButton missing on report panel.");
                return;
            }

            confirmButton.onClick.RemoveListener(OnReportConfirm);
            confirmButton.onClick.AddListener(OnReportConfirm);
            reportConfirmWired = true;
        }

        private void ApplyReportTitle()
        {
            if (reportTitle == null && reportPanel != null)
            {
                var titleTransform = reportPanel.transform.Find("ReportTitle");
                if (titleTransform != null)
                    reportTitle = titleTransform.GetComponent<TextMeshProUGUI>();
            }

            if (reportTitle == null)
                return;

            reportTitle.text = LastOutcome switch
            {
                BattleOutcome.Victory => UiText.BattleVictory,
                BattleOutcome.Defeat => UiText.BattleDefeat,
                _ => UiText.BattleEnd
            };
        }

        private void OnReportConfirm()
        {
            Debug.Log($"{GetType().Name}: Report confirmed → Main.");
            if (reportPanel != null)
                reportPanel.SetActive(false);
            var screen = PendingReturnScreen ?? AppScreen.Battle;
            PendingReturnScreen = null;
            PendingIsChapterPrologue = false;
            if (screen == AppScreen.Bridge)
                BattleSceneExit.ReturnToBridge();
            else
                BattleSceneExit.ReturnToExplore();
        }

    }
}

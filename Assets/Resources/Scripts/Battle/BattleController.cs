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
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.Scene;
using UnityEngine.SceneManagement;

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
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static BattleController Instance { get; private set; }
        [SerializeField] private List<Card> playerCards;
        [SerializeField] private List<Card> enemyCards;
        private List<CardEntity> inlineEntities = new();
        [SerializeField] private BattleController enemyController;

        //private bool isBattleActive = false;
        // public bool isSpecialAttackInProgress = false;

        public GameObject reportPanel;
        #endregion

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }
        private void Start()
        {
            Init();
            StartBattle();
        }

        private void OnEnable()
        {
            if (GameStatusManager.Instance != null)
                GameStatusManager.Instance.CurrentScene = CurrentScene.BATTLE_SCENE;
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
                inlineEntities = ListDeepCopyUtil.DeepCopyViaJson(cardListMgr.GetInLineCardEntities());
            }
            else if (DataUtil.Instance != null)
            {
                var loaded = DataUtil.Instance.LoadCardData();
                inlineEntities = ListDeepCopyUtil.DeepCopyViaJson(
                    loaded.FindAll(e => e.GetLineupPosition() != LineupPosition.None));
            }
            else
            {
                inlineEntities = new List<CardEntity>();
            }
            HideCards(playerCards);
            HideCards(enemyCards);

            for (var i = 0; i < enemyCards.Count && i < 5; i++)
                SetCard(enemyCards, i, FakeData(), false);

            if (inlineEntities != null)
            {
                Debug.Log("inLine counts" + inlineEntities.Count);
                foreach (var entity in inlineEntities)
                    SetCard(playerCards, (int)entity.GetLineupPosition(), entity, true);
            }
        }
        #endregion

        /// <summary>
        /// Calculates damage entity between attacker and defender.
        /// </summary>
        public DamageEntity CalculateDamage(Card playerCard, Card enemyCard, float attackMultiplier = 1)
        {
            if (playerCard?.cardEntity == null || enemyCard?.cardEntity == null)
                return new DamageEntity(0, DamageType.DAMAGE, 1);
            var pEntity = playerCard.cardEntity;
            var eEntity = enemyCard.cardEntity;
            float hitRate = Mathf.Clamp(pEntity.Accuracy - eEntity.Dodge, 0, 1);
            if (UnityEngine.Random.value >= hitRate)
                return new DamageEntity(0, DamageType.DAMAGE, 1);
            float criticalMultiplier = UnityEngine.Random.value < pEntity.Critical ? pEntity.CriticalDamage : 1;
            float reductionRate = CalculateDmgReductionRate(eEntity);
            float damage = pEntity.GetBattleAttack() * criticalMultiplier * (1 - reductionRate) * attackMultiplier;
            return new DamageEntity(damage, DamageType.DAMAGE, criticalMultiplier);
        }

        /// <summary>
        /// Calculates defender's damage reduction rate.
        /// </summary>
        public float CalculateDmgReductionRate(CardEntity eEntity)
        {
            if (eEntity == null) return 0;
            double logBase1Point4 = Math.Log(Mathf.Max(eEntity.GetBattleDefense(), 1)) / Math.Log(1.4);
            float reductionRate = (float)(logBase1Point4 * 0.01f + eEntity.DamageReduction);
            return Mathf.Clamp(reductionRate, 0, 1);
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
            StartCoroutine(BattleRoutine());
        }

        /// <summary>
        /// Runs the battle state machine until one side is defeated or MaxRound is reached.
        /// Initiative is rebuilt every round from living cards and ordered by speed.
        /// </summary>
        private IEnumerator BattleRoutine()
        {
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
                    if (targets.Count == 0) break;

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
                        BattleInfo.Instance?.PlayBattleInfoAnimation(!HasAliveCards(playerCards) ? "Defeated" : "Victory");
                        GameStatusManager.Instance.IsBattle = false;
                        break;
                    }
                }

                yield return new WaitForSeconds(0.5f);

                if (!GameStatusManager.Instance.IsBattle) break;
            }

            if (GameStatusManager.Instance.IsBattle && BattleInfo.Instance != null)
                BattleInfo.Instance.PlayBattleInfoAnimation("Max Rounds Reached");

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
            Debug.Log("Getting alive targets for card: " + card?.name ?? "null");
            Debug.Log("player cards count: " + playerCards?.Count);
            Debug.Log("enemy cards count: " + enemyCards?.Count);

            if (card == null || !card.IsAlive())
            {
                Debug.LogWarning($"{GetType().Name} GetAliveTargets: Invalid or dead card");
                return new List<Card>();
            }

            var targets = card.isPlayerCard ? enemyCards : playerCards;
            if (targets == null)
            {
                Debug.LogWarning($"{GetType().Name} GetAliveTargets: Invalid targets");
                return new List<Card>();
            }
            Debug.Log("Returning alive targets: " + targets.Count() + " cards: " + targets.Select(t => t?.name ?? "null").ToArray() + "\n");
            return targets.Where(t => t != null && t.IsAlive()).ToList();
        }

        /// <summary>
        /// Shows result and returns to main scene.
        /// </summary>
        private void ShowBattleReport()
        {
            Debug.Log($"{GetType().Name} ShowBattleReport");
            reportPanel.SetActive(true);
            BattleReportManager.Instance.RefreshChart(ChartType.pDamageChart, playerCards);
            BattleReportManager.Instance.RefreshChart(ChartType.pInjuryChart, playerCards);
            BattleReportManager.Instance.RefreshChart(ChartType.pHealChart, playerCards);
            //SceneLoader.Instance.LoadScene(nameof(SceneLoader.SceneName.MainScene));
        }

        /// <summary>
        /// Generates fake card entity data.
        /// </summary>
        private CardEntity FakeData()
        {
            var cardDataMgr = CardDataManager.Instance;
            Debug.Log("CardDataMgr is null: " + (cardDataMgr == null));
            if (cardDataMgr == null) return null;
            var character = cardDataMgr.GetCharacter();
            return character == null ? null : cardDataMgr.GetCardEntity(character);
        }
    }
}

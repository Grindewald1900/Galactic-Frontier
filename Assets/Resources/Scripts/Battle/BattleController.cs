using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using System.Collections;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Main;
using UnityEngine.SceneManagement;

namespace Assets.Resources.Scripts.Battle
{
    /// <summary>
    /// Manages battle flow, damage calculations, and card entities.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
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

        private bool isBattleActive = false;
        public bool isSpecialAttackInProgress = false;

        public GameObject reportPanel;

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
                GameStatusManager.Instance.currentScene = GameStatusManager.CurrentScene.BATTLE_SCENE;
            reportPanel.SetActive(false);
        }

        private void Init()
        {
            CharacterSkillController.InitSkillSet();
            var cardListMgr = CardListManager.Instance;
            Debug.Log("inLine counts cardListMgr " + cardListMgr != null);
            inlineEntities = cardListMgr != null
                ? ListDeepCopyUtil.DeepCopyViaJson(cardListMgr.GetInLineCardEntities())
                : new List<CardEntity>();
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
            if (isBattleActive) return;
            isBattleActive = true;
            currentRound = 0;
            StartCoroutine(BattleRoutine());
        }

        /// <summary>
        /// Main battle sequence coroutine.
        /// </summary>
        private IEnumerator BattleRoutine()
        {
            while (currentRound < MaxRound && isBattleActive)
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
                    if (!card.IsAlive()) continue;
                    yield return new WaitUntil(() => !isSpecialAttackInProgress);

                    var targets = GetAliveTargets(card);
                    if (targets.Count == 0) continue;

                    var character = CharacterSkillController.GetCharacter(card.cardEntity.characterName);
                    if (character == null)
                    {
                        Debug.LogError($"character not found for character: {card.cardEntity.characterName}");
                        continue;
                    }

                    card.Highlight(DefaultProperty.highlightCardScale);

                    if (card.progress >= 1f)
                    {
                        card.ResetEnergyBar();
                        isSpecialAttackInProgress = true;
                        yield return character.SpecialAttack(card, targets);
                        isSpecialAttackInProgress = false;
                    }
                    else
                    {
                        yield return character.NormalAttack(card, targets);
                    }

                    card.Unhighlight(DefaultProperty.defaultCardScale);

                    if (!HasAliveCards(playerCards) || !HasAliveCards(enemyCards))
                    {
                        if (BattleInfo.Instance != null)
                        {
                            BattleInfo.Instance.PlayBattleInfoAnimation(!HasAliveCards(playerCards) ? "Defeated" : "Victory");
                        }
                        isBattleActive = false;
                        break;
                    }
                }

                yield return new WaitForSeconds(0.5f);

                if (!isBattleActive) break;
            }

            if (isBattleActive && BattleInfo.Instance != null)
                BattleInfo.Instance.PlayBattleInfoAnimation("Max Rounds Reached");

            if (BattleInfo.Instance != null)
            {
                BattleInfo.Instance.PlayBattleInfoAnimation("Battle End");
                yield return new WaitUntil(() => !BattleInfo.Instance.isBattleInfoActive);
            }
            isBattleActive = false;
            yield return new WaitForSeconds(0.5f);

            // ShowBattleReport();
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
            if (card == null) return new List<Card>();
            var targets = card.isPlayerCard ? enemyCards : playerCards;
            if (targets == null) return new List<Card>();
            var result = new List<Card>();
            foreach (var target in targets) if (target != null && target.IsAlive()) result.Add(target);
            return result;
        }

        /// <summary>
        /// Shows result and returns to main scene.
        /// </summary>
        private void ShowBattleReport()
        {
            Debug.Log($"{GetType().Name} ShowBattleReport");
            SceneManager.LoadScene("MainScene");
        }

        /// <summary>
        /// Generates fake card entity data.
        /// </summary>
        private CardEntity FakeData()
        {
            var cardDataMgr = CardDataManager.Instance;
            if (cardDataMgr == null) return null;
            var character = cardDataMgr.GetCharacter();
            return character == null ? null : cardDataMgr.GetCardEntity(character);
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using System.Collections;

public class BattleController : MonoBehaviour
{
    private static readonly int maxRound = 15;
    private int currentRound = 0;
    public static BattleController Instance { get; private set; }
    public List<Card> playerCards;
    public List<Card> enermyCards;
    public BattleController enermyController;

    private bool isBattleActive = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {
        Init();
        StartBattle();
        // CardDataManager.Instance.AddCard();
        // CardDataManager.Instance.SaveCards();
    }

    void Update()
    {

    }

    private void Init()
    {
        for (int i = 0; i < 5; i++)
        {
            CardEntity demoCardEntity = new CardEntity().SetSpeed(UnityEngine.Random.Range(25f, 35f));
            SetCard(playerCards, i, demoCardEntity, true);
            SetCard(enermyCards, i, demoCardEntity, false);
        }
    }

    public static int FindMinPositionIndex(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            return -1;
        }

        int minPosition = int.MaxValue;
        int minIndex = -1;

        for (int i = 0; i < cards.Count; i++)
        {
            Card currentCard = cards[i];
            if (!currentCard.IsAlive())
            {
                continue;
            }

            if (currentCard.position < minPosition)
            {
                minPosition = currentCard.position;
                minIndex = i;
            }
        }

        return minIndex;
    }

    public void AttackMinPosEnermy(Card card)
    {
        List<Card> enermyList = card.isPlayerCard ? enermyCards : playerCards;
        if (enermyList == null) return;
        if (enermyList.Count == 0) return;
        int enermyIndex = FindMinPositionIndex(enermyList);
        if (enermyIndex == -1) return;
        float damage = CalculateDamage(card, enermyList[enermyIndex]);
        enermyList[enermyIndex].TakeDamage(damage);
    }

    public void AttackRandomEnermy(Card card, int count)
    {
        List<Card> enermyList = card.isPlayerCard ? enermyCards : playerCards;
        if (enermyList == null) return;
        if (enermyList.Count == 0) return;

        // Get list of alive enemies
        var aliveEnemies = enermyList.Where(e => e.IsAlive()).ToList();
        if (aliveEnemies.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            int enermyIndex = UnityEngine.Random.Range(0, aliveEnemies.Count);
            float damage = CalculateDamage(card, aliveEnemies[enermyIndex]);
            aliveEnemies[enermyIndex].TakeDamage(damage);
        }
    }

    public float CalculateDamage(Card playerCard, Card enermyCard)
    {
        float damage = 0;
        CardEntity pEntity = playerCard.cardEntity;
        CardEntity eEntity = enermyCard.cardEntity;
        float hitRate = pEntity.accuracy - eEntity.dodge;
        hitRate = Mathf.Clamp(hitRate, 0, 1);
        bool isHit = UnityEngine.Random.Range(0f, 1f) < hitRate;
        if (!isHit) return damage;
        float criticalMultiplier = UnityEngine.Random.Range(0f, 1f) < pEntity.critical ? pEntity.criticalDamage : 1;
        float reductionRate = CalculateDmgReductionRate(eEntity);
        return pEntity.attack * pEntity.attackCoefficient * criticalMultiplier * (1 - reductionRate);
    }

    public float CalculateDmgReductionRate(CardEntity eEntity)
    {
        double logBase1Point4 = Math.Log(eEntity.defense * eEntity.defenseCoefficient) / Math.Log(1.4);
        float reductionRate = (float)(logBase1Point4 * 0.01f + eEntity.dagameReduction);
        return Mathf.Clamp(reductionRate, 0, 1);
    }

    // Setup initial state of card
    public void SetCard(List<Card> target, int index, CardEntity cardEntity, bool isPlayer)
    {
        if (target[index] == null) return;
        {
            target[index].gameObject.SetActive(true);
            target[index].isPlayerCard = isPlayer;
            target[index].InitCard(cardEntity);
        }
    }

    public void RemoveCard(List<Card> target, Card card)
    {
        if (target.Any(c => c.position == card.position))
        {
            card.gameObject.SetActive(false);
        }
    }

    public void StartBattle()
    {
        if (isBattleActive) return;
        isBattleActive = true;
        currentRound = 0;
        StartCoroutine(BattleRoutine());
    }

    private IEnumerator BattleRoutine()
    {
        BattleInfo.Instance.PlayBattleStartAnimation("Battle Start");
        yield return new WaitForSeconds(2f);
        while (currentRound < maxRound && isBattleActive)
        {
            currentRound++;
            Debug.Log($"Round {currentRound} started");
            // Combine and sort all alive cards by speed
            var allCards = playerCards.Concat(enermyCards)
                .Where(card => card.IsAlive())
                .OrderByDescending(card => card.cardEntity.speed)
                .ToList();
            Debug.Log("allCards: " + allCards.Count);
            // Each card takes their turn
            foreach (var card in allCards)
            {
                if (!card.IsAlive()) continue;
                // Perform attack
                card.Attack();
                // Add delay between actions for visualization
                yield return new WaitForSeconds(1f);
                // Check if battle should end
                if (!HasAliveCards(playerCards) || !HasAliveCards(enermyCards))
                {
                    isBattleActive = false;
                    Debug.Log("Battle ended - one side defeated");
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
        isBattleActive = false;
        Debug.Log("Battle ended - max rounds reached");
    }

    private bool HasAliveCards(List<Card> cards)
    {
        return cards.Any(card => card.IsAlive());
    }
}
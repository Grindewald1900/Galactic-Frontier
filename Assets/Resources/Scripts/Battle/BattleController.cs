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
    public bool isSpecialAttackInProgress = false;

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
    }

    private void Init()
    {
        CharacterSkillController.InitSkillSet();
        List<CardEntity> cardEntities = new List<CardEntity>();
        cardEntities.Add(new CardEntity().SetSpeed(UnityEngine.Random.Range(25f, 35f)).SetCardName("Asra").SetCharacter(Character.Asra).SetArchetype(Archetype.Mechanician).SetCharacterTier(CharacterTier.TierF));
        cardEntities.Add(new CardEntity().SetSpeed(UnityEngine.Random.Range(25f, 35f)).SetCardName("Sernia").SetCharacter(Character.Sernia).SetArchetype(Archetype.Magician).SetCharacterTier(CharacterTier.TierE));
        cardEntities.Add(new CardEntity().SetSpeed(UnityEngine.Random.Range(25f, 35f)).SetCardName("Magki").SetCharacter(Character.Magki).SetArchetype(Archetype.Monster).SetCharacterTier(CharacterTier.TierD));
        for (int i = 0; i < 5; i++)
        {
            SetCard(playerCards, i, cardEntities[UnityEngine.Random.Range(0, cardEntities.Count)], true);
            SetCard(enermyCards, i, cardEntities[UnityEngine.Random.Range(0, cardEntities.Count)], false);
        }
    }

    public DamageEntity CalculateDamage(Card playerCard, Card enermyCard, float attackMultiplier = 1)
    {
        Debug.Log("CalculateDamage: " + playerCard.isPlayerCard + " " + playerCard.position + " -> " + enermyCard.isPlayerCard + " " + enermyCard.position);

        CardEntity pEntity = playerCard.cardEntity;
        CardEntity eEntity = enermyCard.cardEntity;
        float hitRate = Mathf.Clamp(pEntity.accuracy - eEntity.dodge, 0, 1);
        float damage = 0;
        float criticalMultiplier = UnityEngine.Random.Range(0f, 1f) < pEntity.critical ? pEntity.criticalDamage : 1;
        float reductionRate = CalculateDmgReductionRate(eEntity);
        bool isHit = UnityEngine.Random.Range(0f, 1f) < hitRate;
        damage = pEntity.attack * pEntity.attackCoefficient * criticalMultiplier * (1 - reductionRate) * attackMultiplier;
        Debug.Log("CalculateDamage damage: " + damage);
        return new DamageEntity(damage, DamageType.DAMAGE, criticalMultiplier);
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
            target[index].position = index + 1;
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
        while (currentRound < maxRound && isBattleActive)
        {
            currentRound++;
            if (currentRound == 1)
            {
                BattleInfo.Instance.PlayBattleInfoAnimation("Battle Start");
                yield return new WaitUntil(() => !BattleInfo.Instance.isBattleInfoActive);
            }
            BattleInfo.Instance.PlayBattleInfoAnimation("Round " + currentRound);
            yield return new WaitUntil(() => !BattleInfo.Instance.isBattleInfoActive);
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

                yield return new WaitUntil(() => !isSpecialAttackInProgress);

                List<Card> targets = GetAliveTargets(card);
                if (targets == null || targets.Count == 0)
                    yield break;
                SkillSet skillSet = CharacterSkillController.GetSkillSet(card.cardEntity.character);
                if (skillSet == null)
                {
                    Debug.LogError("SkillSet not found for character: " + card.cardEntity.character);
                    yield break;
                }
                skillSet.NormalAttack(card, targets);
                // Add delay between actions for visualization
                yield return new WaitForSeconds(1.5f);
                // Check if battle should end
                if (!HasAliveCards(playerCards))
                {
                    BattleInfo.Instance.PlayBattleInfoAnimation("Defeated");
                    yield break;
                }
                if (!HasAliveCards(enermyCards))
                {
                    BattleInfo.Instance.PlayBattleInfoAnimation("Victory");
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
        BattleInfo.Instance.PlayBattleInfoAnimation("Battle End");
        isBattleActive = false;
        Debug.Log("Battle ended - max rounds reached");
    }

    private bool HasAliveCards(List<Card> cards)
    {
        return cards.Any(card => card.IsAlive());
    }

    public List<Card> GetAliveTargets(Card card)
    {
        List<Card> targets = card.isPlayerCard ? enermyCards : playerCards;
        return targets.Where(target => target.IsAlive()).ToList();
    }
}
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

    // 单体攻击方法
    // attacker 发动攻击的一方
    // selection: 目标选择策略（随机、最快、最低血量、最低防御）
    // status: 附加的异常状态（如果不需要，则传 StatusType.None）
    public void AttackSingleTarget(Card attacker, TargetSelection selection, Status.DebuffType status = Status.DebuffType.None)
    {
        List<Card> targets = attacker.isPlayerCard ? enermyCards : playerCards;
        if (targets == null || targets.Count == 0)
            return;

        // 仅考虑存活的目标
        var aliveTargets = targets.Where(t => t.IsAlive()).ToList();
        if (aliveTargets.Count == 0) return;

        Card target = null;
        switch (selection)
        {
            case TargetSelection.Random:
                target = aliveTargets[UnityEngine.Random.Range(0, aliveTargets.Count)];
                break;
            case TargetSelection.Fastest:

                target = aliveTargets.OrderByDescending(t => t.cardEntity.speed).First();
                break;
            case TargetSelection.LowestHP:
                target = aliveTargets.OrderBy(t => t.currentHealth).First();
                break;
            case TargetSelection.LowestDefense:
                target = aliveTargets.OrderBy(t => t.cardEntity.defense).First();
                break;
        }
        if (target == null) return;

        CalculateDamage(attacker, target);
        attacker.PlayAttackAnimation();

        /**       
         * if (status != StatusType.None)
               {
                   target.ApplyStatus(status);
                   Debug.Log($"[{target.cardName}] is inflicted with {status}.");
               }
       **/
    }

    // 群体攻击方法
    // attacker 发动攻击的一方
    // selection: 群体目标选择策略（随机多人、攻击前排、攻击后排）
    // status: 附加的异常状态（如果不需要，则传 StatusType.None）
    public void AttackGroupTarget(Card attacker, TargetSelection selection, Status.DebuffType status = Status.DebuffType.None)
    {
        List<Card> targets = attacker.isPlayerCard ? enermyCards : playerCards;

        if (targets == null || targets.Count == 0)
            return;

        // 取出存活的目标
        var aliveTargets = targets.Where(t => t.IsAlive()).ToList();
        if (aliveTargets.Count == 0) return;

        List<Card> selectedTargets = new List<Card>();

        switch (selection)
        {
            case TargetSelection.RandomGroup:
                // 攻击随机 2~3 个目标（可根据需求调整数量）
                int count = Mathf.Min(UnityEngine.Random.Range(2, 4), aliveTargets.Count);
                selectedTargets = aliveTargets.OrderBy(x => Guid.NewGuid()).Take(count).ToList();
                break;

            case TargetSelection.FrontRow:
                // 前排卡牌：假设 position==1或2 为前排
                selectedTargets = aliveTargets.Where(t => t.position == 1 || t.position == 2).ToList();
                break;
            case TargetSelection.BackRow:
                // 后排卡牌：假设 position==3,4,5 为后排
                selectedTargets = aliveTargets.Where(t => t.position >= 3 && t.position <= 5).ToList();
                break;
        }

        if (selectedTargets.Count == 0)
        {
            // 如果没有满足条件的目标，则降级为随机攻击所有存活目标
            selectedTargets = aliveTargets;
        }

        foreach (var target in selectedTargets)
        {
            CalculateDamage(attacker, target);
            attacker.PlayAttackAnimation();
            // if (status != StatusType.None)
            // {
            //     target.ApplyStatus(status);
            //     Debug.Log($"[{target.cardName}] is inflicted with {status}.");
            // }
        }
    }

    public void CalculateDamage(Card playerCard, Card enermyCard)
    {
        Debug.Log("CalculateDamage: " + playerCard.isPlayerCard + " " + playerCard.position + " -> " + enermyCard.isPlayerCard + " " + enermyCard.position);
        float damage = 0;
        CardEntity pEntity = playerCard.cardEntity;
        CardEntity eEntity = enermyCard.cardEntity;
        float hitRate = pEntity.accuracy - eEntity.dodge;

        hitRate = Mathf.Clamp(hitRate, 0, 1);

        bool isHit = UnityEngine.Random.Range(0f, 1f) < hitRate;

        float criticalMultiplier = UnityEngine.Random.Range(0f, 1f) < pEntity.critical ? pEntity.criticalDamage : 1;
        float reductionRate = CalculateDmgReductionRate(eEntity);
        damage = pEntity.attack * pEntity.attackCoefficient * criticalMultiplier * (1 - reductionRate);
        enermyCard.StartCoroutine(enermyCard.TakeDamage(new DamageEntity[] {
                new DamageEntity(damage, DamageType.DAMAGE, criticalMultiplier),
                new DamageEntity(damage * 2.5f, DamageType.DAMAGE, 2.5f),
                new DamageEntity(0, DamageType.MISS, 1f),
                }));
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
        BattleInfo.Instance.PlayBattleInfoAnimation("Battle Start");
        yield return new WaitForSeconds(2f);
        while (currentRound < maxRound && isBattleActive)
        {
            currentRound++;
            BattleInfo.Instance.PlayBattleInfoAnimation("Round " + currentRound);
            yield return new WaitForSeconds(2f);
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
                bool useGroupAttack = UnityEngine.Random.Range(0f, 1f) < 0.5f;
                if (useGroupAttack)
                {
                    AttackGroupTarget(card, TargetSelection.RandomGroup, Status.DebuffType.Burning);
                }
                else
                {
                    AttackSingleTarget(card, TargetSelection.LowestHP, Status.DebuffType.Poisoned);
                }
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
}
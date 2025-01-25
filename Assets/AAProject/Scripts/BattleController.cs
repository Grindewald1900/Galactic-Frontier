using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    public static BattleController Instance { get; private set; }
    public List<Card> playerCards;
    public List<Card> enermyCards;
    public BattleController enermyController;

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
            CardEntity demoCardEntity = new CardEntity();
            demoCardEntity.SetCardName("Yee" + i.ToString()).SetDamage(Random.Range(10f, 25f)).SetSpeed(Random.Range(8f, 20f)).SetEnergyIncreaseRate(Random.Range(15f, 35f));
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
        enermyList[enermyIndex].TakeDamage(card.damage);
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
}

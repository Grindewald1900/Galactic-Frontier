using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    public List<Card> playerCards;
    public List<Card> enermyCards;
    public BattleController enermyController;

    void Start()
    {
        enermyCards = enermyController.playerCards;
        CardEntity card1 = new CardEntity();
        card1.SetCardName("Card1").SetDamage(20f).SetSpeed(10f);

        // CardDataManager.Instance.AddCard();
        // CardDataManager.Instance.SaveCards();
    }

    void Update()
    {

    }

    public void AddCard(Card card)
    {
        if (!playerCards.Any(c => c.cardName == card.name))
        {
            playerCards.Add(card);
        }
    }

    public void RemoveCard(Card card)
    {
        if (playerCards.Any(c => c.cardName == card.name))
        {
            playerCards.Remove(card);
        }
    }
}

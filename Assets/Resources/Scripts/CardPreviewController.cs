using UnityEngine;

public class CardPreviewController : MonoBehaviour
{
    public static CardPreviewController instance;
    public Card card;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        card.isBattleActive = false;
    }

    public void ShowCardPreview(CardEntity cardEntity)
    {
        card.InitCard(cardEntity);
        card.gameObject.SetActive(true);
    }
}
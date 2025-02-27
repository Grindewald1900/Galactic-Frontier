using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Unity.VisualStudio.Editor;
public class CardResultManager : MonoBehaviour
{
    public static CardResultManager Instance;
    public Transform cardContent;
    public Transform tierContent;
    public Transform characterContent;
    public GameObject cardPrefab;
    // Both tier and character share the same prefab
    public GameObject reportPrefab;
    public List<Card> cards = new List<Card>();
    private List<CardEntity> cardEntities = new List<CardEntity>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void InitCards(int cardCount)
    {
        cards.Clear();
        // TODO: Replace with actual data fetching logic
        FakeData(cardCount);
        foreach (var cardEntity in cardEntities)
        {
            AddCard(cardEntity);
        }
        ShowReport(cardEntities);
    }

    private void AddCard(CardEntity cardEntity)
    {
        GameObject cardGO = Instantiate(cardPrefab, cardContent);
        Card card = cardGO.GetComponent<Card>();
        card.InitCard(cardEntity);
        cards.Add(card);
    }

    public void ShowReport(List<CardEntity> drawResults)
    {
        Dictionary<CharacterTier, int> tierDrawCount = GetTierDrawCount(drawResults);
        foreach (var tier in tierDrawCount)
        {
            ShowTier(tier.Key, tier.Value);
        }
        Dictionary<Character, int> characterDrawCount = GetCharacterDrawCount(drawResults);
        foreach (var character in characterDrawCount)
        {
            ShowCharacter(character.Key, character.Value);
        }
    }

    private Dictionary<Character, int> GetCharacterDrawCount(List<CardEntity> drawResults)
    {
        return drawResults.GroupBy(card => card.character)  // 按 `Character` 分组
                          .ToDictionary(group => group.Key, group => group.Count());
    }

    private Dictionary<CharacterTier, int> GetTierDrawCount(List<CardEntity> drawResults)
    {
        return drawResults.GroupBy(card => card.characterTier)  // 按 `Tier` 分组
                          .ToDictionary(group => group.Key, group => group.Count());
    }

    private void ShowCharacter(Character character, int count)
    {
        GameObject characterGO = Instantiate(reportPrefab, characterContent);
        characterGO.GetComponent<ReportSlot>().SetReport(character.ToString(), count);
    }

    private void ShowTier(CharacterTier tier, int count)
    {
        GameObject tierGO = Instantiate(reportPrefab, tierContent);
        tierGO.GetComponent<ReportSlot>().SetReport(tier.ToString(), count);
    }

    private List<CardEntity> FakeData(int count)
    {
        List<CardEntity> fakeCards = new List<CardEntity>();
        fakeCards.Add(new CardEntity().SetSpeed(UnityEngine.Random.Range(25f, 35f)).SetCardName("Asra").SetCharacter(Character.Asra).SetArchetype(Archetype.Mechanician).SetCharacterTier(CharacterTier.TierA));
        fakeCards.Add(new CardEntity().SetSpeed(UnityEngine.Random.Range(25f, 35f)).SetCardName("Sernia").SetCharacter(Character.Sernia).SetArchetype(Archetype.Magician).SetCharacterTier(CharacterTier.TierE));
        fakeCards.Add(new CardEntity().SetSpeed(UnityEngine.Random.Range(25f, 35f)).SetCardName("Magki").SetCharacter(Character.Magki).SetArchetype(Archetype.Monster).SetCharacterTier(CharacterTier.TierD));

        for (int i = 0; i < count; i++)
        {
            cardEntities.Add(fakeCards[UnityEngine.Random.Range(0, fakeCards.Count)]);
        }
        return cardEntities;
    }
}
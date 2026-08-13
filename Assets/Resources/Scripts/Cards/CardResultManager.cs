using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.UI;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Gacha;
using Assets.Resources.Scripts.Utils.Save;
using static Assets.Resources.Scripts.Cards.CardDataManager;
using static Assets.Resources.Scripts.Main.GameStatusManager;

namespace Assets.Resources.Scripts.Cards
{
    public class CardResultManager : MonoBehaviour
    {
        public static CardResultManager Instance;
        public Transform cardContent;
        public Transform tierContent;
        public Transform characterContent;
        public GameObject cardPrefab;
        // Both tier and character share the same prefab
        public GameObject reportPrefab;
        public Button confirmButton;
        public List<Card> cards = new();
        private List<CardEntity> cardEntities = new();
        private bool grantOnReport;

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
        }

        void OnDisable()
        {
            ClearCardResult();
        }

        private void Init()
        {
            confirmButton.onClick.AddListener(() => ConfirmCards());
        }

        public void InitCards(int cardCount)
        {
            cards.Clear();
            cardEntities.Clear();
            grantOnReport = true;

            if (DevData.IsActive)
            {
                var samples = DevData.Current.CreateSampleGachaResults(cardCount);
                cardEntities.AddRange(samples);
                Debug.Log($"[DEV-DATA] Created {cardEntities.Count} sample gacha results.");
            }
            else
            {
                DevData.LogSkipped(nameof(CardResultManager) + ".CreateSampleGachaResults");
                var result = GachaService.TryPull(cardCount, grantImmediately: false);
                if (!result.Success)
                {
                    Debug.LogWarning("[GACHA] " + result.Message);
                    return;
                }
                cardEntities.AddRange(result.Cards);
            }

            PresentLoadedCards();
        }

        public void PresentResults(List<CardEntity> entities, bool grantOnReport)
        {
            cards.Clear();
            cardEntities.Clear();
            this.grantOnReport = grantOnReport;
            if (entities != null)
                cardEntities.AddRange(entities);
            PresentLoadedCards();
        }

        private void PresentLoadedCards()
        {
            if (cardEntities.Count == 0)
                return;

            foreach (var cardEntity in cardEntities)
                AddCard(cardEntity);
            StartCoroutine(FlipAllCards());
        }

        private void AddCard(CardEntity cardEntity)
        {
            GameObject cardGO = Instantiate(cardPrefab, cardContent);
            Card card = cardGO.GetComponent<Card>();
            card.InitCard(cardEntity);
            card.FlipCard(true, false);
            cards.Add(card);
        }

        private void ConfirmCards()
        {
            // Cards are granted in ShowReport (flip complete). Confirm only returns to shop.
            MainScrollController.Instance.ShowPanel(CurrentScene.SHOP_MENU);
        }

        public void ClearCardResult()
        {
            confirmButton.interactable = false;
            cards.Clear();
            cardEntities.Clear();
            ClearLayout(cardContent);
            ClearLayout(tierContent);
            ClearLayout(characterContent);
        }

        private void ClearLayout(Transform transform)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private IEnumerator FlipAllCards()
        {
            GameStatusManager.Instance.IsDrawingCard = true;
            yield return new WaitForSeconds(1f);
            foreach (var card in cards)
            {
                card.FlipCard(false, true);
                Debug.Log("Flipped card: " + card.cardEntity.characterName.ToString());
                yield return new WaitForSeconds(0.5f);
            }
            ShowReport(cardEntities);
        }

        public void ShowReport(List<CardEntity> drawResults)
        {
            GameStatusManager.Instance.IsDrawingCard = false;
            Dictionary<CharacterTier, int> tierDrawCount = GetTierDrawCount(drawResults);
            foreach (var tier in tierDrawCount)
            {
                ShowTier(tier.Key, tier.Value);
            }
            foreach (var character in GetCharacterDrawCount(drawResults))
            {
                ShowCharacter(character.Key, character.Value);
            }
            confirmButton.interactable = true;
            if (grantOnReport)
                GachaService.Grant(cardEntities);
        }

        private Dictionary<CharacterName, int> GetCharacterDrawCount(List<CardEntity> drawResults)
        {
            return drawResults.GroupBy(card => card.characterName)  // 按 `Character` 分组
                              .ToDictionary(group => group.Key, group => group.Count());
        }

        private Dictionary<CharacterTier, int> GetTierDrawCount(List<CardEntity> drawResults)
        {
            return drawResults.GroupBy(card => card.CharacterTier)  // 按 `Tier` 分组
                              .ToDictionary(group => group.Key, group => group.Count());
        }

        private void ShowCharacter(CharacterName character, int count)
        {
            GameObject characterGO = Instantiate(reportPrefab, characterContent);
            characterGO.GetComponent<ReportSlot>().SetReport(character.ToString(), count);
        }

        private void ShowTier(CharacterTier tier, int count)
        {
            GameObject tierGO = Instantiate(reportPrefab, tierContent);
            tierGO.GetComponent<ReportSlot>().SetReport(tier.ToString(), count);
        }

    }
}
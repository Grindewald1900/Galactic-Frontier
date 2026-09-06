using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Characters;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Shared roster helpers for native Characters / Cards screens.</summary>
    internal static class CardCollectionUi
    {
        public static List<CardEntity> AllCards()
        {
            var manager = CardListManager.Instance;
            if (manager == null)
                return new List<CardEntity>();
            return new List<CardEntity>(manager.CardEntities);
        }

        public static string DisplayName(CardEntity card)
        {
            if (card == null) return "";
            return string.IsNullOrEmpty(card.cardName) ? card.characterName.ToString() : card.cardName;
        }

        public static Transform CreateScroll(Transform parent, Vector2 position, Vector2 size)
        {
            var viewport = new GameObject(
                "Roster Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);

            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 1f);
            viewportRect.anchorMax = new Vector2(0f, 1f);
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.anchoredPosition = new Vector2(position.x, -position.y);
            viewportRect.sizeDelta = size;

            var image = viewport.GetComponent<Image>();
            image.color = NexusTheme.WithAlpha(NexusTheme.Surface, 0.35f);
            image.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, size.y);

            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            return content.transform;
        }

        public static void DrawPortraitButton(
            Transform parent,
            CardEntity card,
            Vector2 position,
            Vector2 size,
            string footer,
            bool selected,
            UnityAction onClick)
        {
            var cell = NexusCardVisual.CreatePortraitCard(
                parent, "Card " + (card?.id ?? "?"), card, position, size, footer);
            var image = cell.GetComponent<Image>();
            image.raycastTarget = true;
            image.color = selected
                ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f)
                : NexusTheme.SurfaceRaised;

            var button = cell.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = NexusTheme.SurfaceHover;
            colors.pressedColor = NexusTheme.WithAlpha(NexusTheme.Gold, 0.45f);
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(onClick);
        }

        public static void DrawDetail(
            Transform parent,
            CardEntity card,
            Vector2 origin,
            Vector2 size,
            System.Action onDismantle,
            List<NexusProgressUi.XpBar> xpBars = null)
        {
            NexusUiFactory.CreateBox(
                parent, "Detail", origin, size, NexusTheme.Surface, NexusTheme.BorderSoft);

            if (card == null)
            {
                NexusUiFactory.CreateText(
                    parent, "Empty", UiText.CardSelectHint,
                    new Vector2(origin.x + 20f, origin.y + 40f), new Vector2(size.x - 40f, 40f),
                    14f, NexusTheme.DimText);
                return;
            }

            card.EnsureProgressionDefaults();

            NexusCardVisual.CreatePortraitCard(
                parent, "DetailArt", card,
                new Vector2(origin.x + 24f, origin.y + 20f), new Vector2(180f, 252f),
                UiText.TierShort(card.CharacterTier.ToString()));

            NexusUiFactory.CreateText(
                parent, "DetailName", DisplayName(card),
                new Vector2(origin.x + 220f, origin.y + 24f), new Vector2(size.x - 244f, 32f),
                20f, NexusTheme.Gold, TextAlignmentOptions.Left, FontStyles.Bold);

            var occ = DeckService.GetOccupation(card.id);
            var occLabel = occ == CardOccupationState.Idle
                ? UiText.OccupationIdle
                : UiText.OccupationBadge(occ.ToString());
            var faction = UiText.T(
                CharacterFactionCatalog.LabelEn(card.characterName),
                CharacterFactionCatalog.LabelZh(card.characterName));
            var bound = card.boundReason != CardBoundReason.None ? " · " + UiText.CardBound : "";
            var meta = NexusUiFactory.CreateText(
                parent, "DetailMeta",
                $"{UiText.TierShort(card.CharacterTier.ToString())}  ·  {UiText.ArchetypeLabel(card.archetype.ToString())}  ·  {faction}  ·  {occLabel}{bound}",
                new Vector2(origin.x + 220f, origin.y + 62f), new Vector2(size.x - 244f, 52f),
                12f, NexusTheme.MutedText);
            meta.textWrappingMode = TextWrappingModes.Normal;

            NexusUiFactory.CreateText(
                parent, "Stats",
                $"Lv.{card.Level}  {UiText.EnergyRankLabel(card.EnergyRank.ToString())}  ATK {card.Attack:0}  HP {card.Health:0}  DEF {card.Defense:0}\n" +
                $"POW {card.power:0}  ACC {card.Accuracy:0}  DOD {card.Dodge:0}",
                new Vector2(origin.x + 220f, origin.y + 120f), new Vector2(size.x - 244f, 48f),
                13f, NexusTheme.Cyan);

            float xpHeight = NexusProgressUi.DrawCardXpStack(
                parent,
                card,
                new Vector2(origin.x + 220f, origin.y + 176f),
                size.x - 244f,
                24f,
                xpBars);

            float actionY = origin.y + 176f + xpHeight + 8f;
            var block = CardDismantleService.Evaluate(card);
            bool can = block == DismantleBlock.None;
            NexusUiFactory.CreateButton(
                parent, "Dismantle", UiText.Dismantle,
                new Vector2(origin.x + 220f, actionY), new Vector2(220f, 44f),
                can ? () => onDismantle?.Invoke() : null,
                can ? NexusTheme.WithAlpha(NexusTheme.Red, 0.2f) : NexusTheme.SurfaceRaised,
                can ? NexusTheme.Red : NexusTheme.DimText,
                14f);

            if (!can)
            {
                NexusUiFactory.CreateText(
                    parent, "Block", BlockReason(block),
                    new Vector2(origin.x + 220f, actionY + 52f), new Vector2(size.x - 244f, 40f),
                    12f, NexusTheme.MutedText);
            }
        }

        public static string BlockReason(DismantleBlock block) => block switch
        {
            DismantleBlock.Bound => UiText.DismantleBlockedBound,
            DismantleBlock.Busy => UiText.DismantleBlockedBusy,
            DismantleBlock.Slotted => UiText.DismantleBlockedSlotted,
            DismantleBlock.Mascot => UiText.DismantleBlockedMascot,
            DismantleBlock.Missing => UiText.DismantleBlockedMissing,
            _ => ""
        };

        public static void ShowDismantleConfirm(Transform root, CardEntity card, System.Action onDone)
        {
            if (card == null) return;

            var overlay = NexusUiFactory.CreatePanel(
                root, "DismantleOverlay", NexusTheme.WithAlpha(Color.black, 0.62f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            overlay.transform.SetAsLastSibling();

            const float w = 520f;
            const float h = 280f;
            var dialog = NexusUiFactory.CreateBox(
                overlay.transform, "Dialog", Vector2.zero, new Vector2(w, h),
                NexusTheme.Surface, NexusTheme.Gold);
            var rect = dialog.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(w, h);
            dialog.GetComponent<Image>().raycastTarget = true;

            var credits = DismantleRules.CreditsForTier((int)card.CharacterTier);
            NexusUiFactory.CreateText(
                dialog.transform, "Title", UiText.DismantleConfirmTitle,
                new Vector2(24f, 18f), new Vector2(w - 48f, 28f), 16f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            var body = NexusUiFactory.CreateText(
                dialog.transform, "Body",
                UiText.DismantleConfirmBody(DisplayName(card), credits),
                new Vector2(24f, 56f), new Vector2(w - 48f, 90f), 13f, NexusTheme.MutedText);
            body.textWrappingMode = TextWrappingModes.Normal;

            NexusUiFactory.CreateButton(
                dialog.transform, "Cancel", UiText.Close,
                new Vector2(24f, 200f), new Vector2(200f, 44f),
                () => Object.Destroy(overlay),
                NexusTheme.SurfaceRaised, NexusTheme.Text, 14f);
            NexusUiFactory.CreateButton(
                dialog.transform, "Confirm", UiText.DismantleConfirm,
                new Vector2(296f, 200f), new Vector2(200f, 44f),
                () =>
                {
                    var result = CardDismantleService.TryDismantle(card);
                    Object.Destroy(overlay);
                    if (result.Success)
                    {
                        var lines = RewardPopup.FromPending(result.Items);
                        if (result.Credits > 0)
                        {
                            lines.Add(new RewardPopup.Line
                            {
                                FallbackName = UiText.CreditsName,
                                Quantity = result.Credits,
                                Quality = 1
                            });
                        }

                        RewardPopup.Show(UiText.DismantleRewardTitle, lines);
                    }

                    onDone?.Invoke();
                },
                NexusTheme.WithAlpha(NexusTheme.Red, 0.22f), NexusTheme.Red, 14f);
        }
    }
}

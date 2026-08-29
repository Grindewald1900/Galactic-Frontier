using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Entity;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Native card-instance roster: inspect copies and dismantle unbound idle cards.</summary>
    internal sealed class CardsScreen
    {
        private readonly Transform root;
        private CardEntity selected;
        private int filter;

        private CardsScreen(Transform root)
        {
            this.root = root;
        }

        public GameObject Root => root.gameObject;

        public static CardsScreen Build(Transform parent)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Cards Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new CardsScreen(panel.transform);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            var all = CardCollectionUi.AllCards();
            if (selected != null && all.Find(c => c != null && c.id == selected.id) == null)
                selected = null;

            NexusUiFactory.CreateText(
                root, "Title", UiText.CardsTitle,
                new Vector2(28f, 16f), new Vector2(640f, 32f), 22f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                root, "Hint", UiText.CardsHint(all.Count),
                new Vector2(28f, 48f), new Vector2(900f, 24f), 13f, NexusTheme.MutedText);

            DrawFilter(28f, 80f, 0, UiText.CardFilterAll);
            DrawFilter(168f, 80f, 1, UiText.CardFilterIdle);
            DrawFilter(308f, 80f, 2, UiText.CardFilterBound);

            var visible = ApplyFilter(all);
            const float cardW = 150f;
            const float cardH = 210f;
            const float gap = 12f;
            const int cols = 6;
            var content = CardCollectionUi.CreateScroll(root, new Vector2(28f, 128f), new Vector2(1080f, 760f));
            for (var i = 0; i < visible.Count; i++)
            {
                var card = visible[i];
                if (card == null) continue;
                int col = i % cols;
                int row = i / cols;
                var captured = card;
                CardCollectionUi.DrawPortraitButton(
                    content, captured,
                    new Vector2(10f + col * (cardW + gap), 10f + row * (cardH + gap)),
                    new Vector2(cardW, cardH),
                    UiText.TierShort(captured.CharacterTier.ToString()),
                    selected != null && selected.id == captured.id,
                    () =>
                    {
                        selected = captured;
                        Rebuild();
                    });
            }

            var rows = Mathf.Max(1, (visible.Count + cols - 1) / cols);
            content.GetComponent<RectTransform>().sizeDelta =
                new Vector2(0f, Mathf.Max(760f, 20f + rows * (cardH + gap)));

            CardCollectionUi.DrawDetail(
                root, selected, new Vector2(1130f, 128f), new Vector2(540f, 760f),
                () => CardCollectionUi.ShowDismantleConfirm(root, selected, Rebuild));
        }

        private void DrawFilter(float x, float y, int id, string label)
        {
            bool on = filter == id;
            NexusUiFactory.CreateButton(
                root, "Filter" + id, label,
                new Vector2(x, y), new Vector2(128f, 36f),
                () =>
                {
                    filter = id;
                    Rebuild();
                },
                on ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f) : NexusTheme.SurfaceRaised,
                on ? NexusTheme.Gold : NexusTheme.MutedText,
                12f);
        }

        private List<CardEntity> ApplyFilter(List<CardEntity> source)
        {
            var list = new List<CardEntity>();
            foreach (var card in source)
            {
                if (card == null) continue;
                if (filter == 1 && !CardDismantleService.CanDismantle(card)) continue;
                if (filter == 2 && card.boundReason == CardBoundReason.None) continue;
                list.Add(card);
            }

            list.Sort((a, b) =>
            {
                int t = b.CharacterTier.CompareTo(a.CharacterTier);
                if (t != 0) return t;
                return string.CompareOrdinal(CardCollectionUi.DisplayName(a), CardCollectionUi.DisplayName(b));
            });
            return list;
        }
    }
}

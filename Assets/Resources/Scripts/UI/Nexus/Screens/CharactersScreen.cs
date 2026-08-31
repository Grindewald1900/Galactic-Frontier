using System.Collections.Generic;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Entity;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Native character roster: unique names, copy counts, inspect / dismantle copies.</summary>
    internal sealed class CharactersScreen
    {
        private readonly Transform root;
        private CharacterName selectedName = CharacterName.Default;
        private CardEntity selectedCopy;

        private CharactersScreen(Transform root)
        {
            this.root = root;
        }

        public GameObject Root => root.gameObject;

        public static CharactersScreen Build(Transform parent)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Characters Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new CharactersScreen(panel.transform);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            var all = CardCollectionUi.AllCards();
            var groups = Group(all);
            if (selectedName != CharacterName.Default && !groups.ContainsKey(selectedName))
            {
                selectedName = CharacterName.Default;
                selectedCopy = null;
            }

            NexusUiFactory.CreateText(
                root, "Title", UiText.CharactersTitle,
                new Vector2(28f, 16f), new Vector2(640f, 32f), 22f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                root, "Hint", UiText.CharactersProfileHint + " · " + UiText.CharactersHint(groups.Count, all.Count),
                new Vector2(28f, 48f), new Vector2(1100f, 24f), 13f, NexusTheme.MutedText);

            const float cardW = 168f;
            const float cardH = 230f;
            const float gap = 14f;
            const int cols = 6;
            var names = new List<CharacterName>(groups.Keys);
            names.Sort((a, b) =>
            {
                int t = Best(groups[b]).CharacterTier.CompareTo(Best(groups[a]).CharacterTier);
                return t != 0 ? t : string.CompareOrdinal(a.ToString(), b.ToString());
            });

            var content = CardCollectionUi.CreateScroll(root, new Vector2(28f, 88f), new Vector2(1080f, 800f));
            for (var i = 0; i < names.Count; i++)
            {
                var name = names[i];
                var copies = groups[name];
                var best = Best(copies);
                int col = i % cols;
                int row = i / cols;
                var captured = name;
                CardCollectionUi.DrawPortraitButton(
                    content, best,
                    new Vector2(10f + col * (cardW + gap), 10f + row * (cardH + gap)),
                    new Vector2(cardW, cardH),
                    UiText.CharacterCopies(copies.Count),
                    selectedName == captured,
                    () =>
                    {
                        selectedName = captured;
                        selectedCopy = Best(groups[captured]);
                        Rebuild();
                    });
            }

            var rows = Mathf.Max(1, (names.Count + cols - 1) / cols);
            content.GetComponent<RectTransform>().sizeDelta =
                new Vector2(0f, Mathf.Max(800f, 20f + rows * (cardH + gap)));

            CardEntity detail = selectedCopy;
            if (detail == null && selectedName != CharacterName.Default && groups.ContainsKey(selectedName))
                detail = Best(groups[selectedName]);

            CardCollectionUi.DrawDetail(
                root, detail, new Vector2(1130f, 88f), new Vector2(540f, 520f),
                () => CardCollectionUi.ShowDismantleConfirm(root, detail, Rebuild));

            if (detail != null && groups.TryGetValue(detail.characterName, out var copiesOf))
                DrawCopyStrip(copiesOf);
        }

        private void DrawCopyStrip(List<CardEntity> copies)
        {
            NexusUiFactory.CreateText(
                root, "CopiesTitle", UiText.CharacterCopyList,
                new Vector2(1130f, 624f), new Vector2(540f, 24f), 13f, NexusTheme.MutedText,
                TextAlignmentOptions.Left, FontStyles.Bold);

            float x = 1130f;
            foreach (var copy in copies)
            {
                if (copy == null) continue;
                var captured = copy;
                bool on = selectedCopy != null && selectedCopy.id == captured.id;
                NexusUiFactory.CreateButton(
                    root, "Copy " + captured.id,
                    UiText.TierShort(captured.CharacterTier.ToString()) + " Lv." + captured.Level,
                    new Vector2(x, 656f), new Vector2(120f, 44f),
                    () =>
                    {
                        selectedCopy = captured;
                        Rebuild();
                    },
                    on ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f) : NexusTheme.SurfaceRaised,
                    on ? NexusTheme.Gold : NexusTheme.Text,
                    11f);
                x += 128f;
                if (x > 1540f) break;
            }
        }

        private static Dictionary<CharacterName, List<CardEntity>> Group(List<CardEntity> cards)
        {
            var map = new Dictionary<CharacterName, List<CardEntity>>();
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (!map.TryGetValue(card.characterName, out var list))
                {
                    list = new List<CardEntity>();
                    map[card.characterName] = list;
                }

                list.Add(card);
            }

            return map;
        }

        private static CardEntity Best(List<CardEntity> copies)
        {
            CardEntity best = null;
            foreach (var copy in copies)
            {
                if (copy == null) continue;
                if (best == null || copy.CharacterTier > best.CharacterTier ||
                    (copy.CharacterTier == best.CharacterTier && copy.Level > best.Level))
                    best = copy;
            }

            return best;
        }
    }
}

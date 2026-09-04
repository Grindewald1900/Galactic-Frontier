using System.Collections.Generic;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Shared modal that reports a batch of items the player just received.
    /// Dismissed by the confirm button or by clicking the scrim outside the dialog.
    /// </summary>
    /// <remarks>
    /// Lives on its own canvas parented to the <see cref="AppShell"/> rather than the calling screen,
    /// because callers typically rebuild their screen root right after claiming, which would otherwise
    /// destroy the popup along with the button that opened it.
    /// </remarks>
    internal static class RewardPopup
    {
        /// <summary>One stack of granted loot.</summary>
        public sealed class Line
        {
            public string DefId = "";
            public string FallbackName = "";
            public int Quantity = 1;
            public int Quality = EconomyConstants.DefaultQuality;
            public bool IsNote;
        }

        private const float DialogWidth = 560f;
        private const float RowHeight = 62f;
        private const float ListMinHeight = 74f;
        private const float ListMaxHeight = 330f;
        private const float ListTop = 92f;
        private const float FooterHeight = 84f;

        private static GameObject current;

        /// <summary>Converts claimed loot into display lines; names are localized at draw time.</summary>
        public static List<Line> FromPending(IList<PendingLootEntry> entries)
        {
            var lines = new List<Line>();
            if (entries == null)
                return lines;

            foreach (var entry in entries)
            {
                if (entry == null) continue;
                lines.Add(new Line
                {
                    DefId = entry.itemDefId,
                    FallbackName = entry.displayName,
                    Quantity = entry.quantity,
                    Quality = entry.quality
                });
            }

            return lines;
        }

        public static List<Line> FromProgressNotes(IList<OfflineProgressNote> notes)
        {
            var lines = new List<Line>();
            if (notes == null) return lines;
            foreach (var note in notes)
            {
                if (note == null) continue;
                var text = UiText.T(note.textEn, note.textZh);
                if (string.IsNullOrWhiteSpace(text)) continue;
                lines.Add(new Line { FallbackName = text, Quantity = 0, IsNote = true });
            }

            return lines;
        }

        public static void Close()
        {
            if (current != null)
                Object.Destroy(current);
            current = null;
        }

        public static void Show(string title, IList<Line> lines, System.Action onClosed = null)
        {
            Close();

            Canvas canvas = NexusUiFactory.CreateCanvas("Reward Popup", 500, true);
            if (AppShell.Instance != null)
                canvas.transform.SetParent(AppShell.Instance.transform, false);
            current = canvas.gameObject;

            GameObject scrim = NexusUiFactory.CreatePanel(
                canvas.transform,
                "Scrim",
                NexusTheme.WithAlpha(Color.black, 0.66f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                true);
            var dismiss = scrim.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(() => Dismiss(onClosed));

            int count = lines?.Count ?? 0;
            float listHeight = Mathf.Clamp(count * RowHeight + 12f, ListMinHeight, ListMaxHeight);
            float dialogHeight = ListTop + listHeight + FooterHeight;

            GameObject dialog = NexusUiFactory.CreateBox(
                scrim.transform,
                "Dialog",
                Vector2.zero,
                new Vector2(DialogWidth, dialogHeight),
                NexusTheme.Surface,
                NexusTheme.Gold);
            var dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(DialogWidth, dialogHeight);
            // Swallow clicks so the scrim behind the dialog does not dismiss it.
            dialog.GetComponent<Image>().raycastTarget = true;

            NexusUiFactory.CreateText(
                dialog.transform,
                "Title",
                string.IsNullOrEmpty(title) ? UiText.RewardTitle : title,
                new Vector2(24f, 18f),
                new Vector2(DialogWidth - 48f, 30f),
                18f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            NexusUiFactory.CreateText(
                dialog.transform,
                "Hint",
                count > 0 ? UiText.RewardHint : UiText.RewardEmpty,
                new Vector2(24f, 52f),
                new Vector2(DialogWidth - 48f, 24f),
                12f,
                NexusTheme.MutedText);

            BuildList(dialog.transform, lines, listHeight);

            const float confirmWidth = 200f;
            NexusUiFactory.CreateButton(
                dialog.transform,
                "Confirm",
                UiText.RewardConfirm,
                new Vector2((DialogWidth - confirmWidth) * 0.5f, ListTop + listHeight + 14f),
                new Vector2(confirmWidth, 44f),
                () => Dismiss(onClosed),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.2f),
                NexusTheme.Gold,
                14f);
            NexusUiFactory.CreateText(
                dialog.transform,
                "DismissHint",
                UiText.RewardDismissHint,
                new Vector2(24f, ListTop + listHeight + 62f),
                new Vector2(DialogWidth - 48f, 20f),
                10f,
                NexusTheme.DimText,
                TextAlignmentOptions.Center);
        }

        private static void Dismiss(System.Action onClosed)
        {
            Close();
            onClosed?.Invoke();
        }

        private static void BuildList(Transform dialog, IList<Line> lines, float listHeight)
        {
            float width = DialogWidth - 48f;
            Transform content = CreateScrollArea(dialog, new Vector2(24f, ListTop), new Vector2(width, listHeight));

            if (lines == null || lines.Count == 0)
            {
                NexusUiFactory.CreateText(
                    content,
                    "Empty",
                    UiText.RewardEmpty,
                    new Vector2(16f, 24f),
                    new Vector2(width - 32f, 24f),
                    12f,
                    NexusTheme.DimText);
                return;
            }

            float y = 6f;
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i] == null) continue;
                DrawRow(content, lines[i], i, y, width);
                y += RowHeight;
            }

            var contentRect = content.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0f, Mathf.Max(listHeight, y + 6f));
        }

        private static void DrawRow(Transform parent, Line line, int index, float y, float width)
        {
            if (line.IsNote)
            {
                NexusUiFactory.CreateText(
                    parent,
                    $"Note {index}",
                    line.FallbackName,
                    new Vector2(16f, y + 10f),
                    new Vector2(width - 32f, RowHeight - 16f),
                    13f,
                    NexusTheme.Cyan);
                return;
            }

            var def = ItemCatalog.Get(line.DefId);
            string name = def != null
                ? UiText.T(def.displayNameEn, def.displayNameZh)
                : (string.IsNullOrEmpty(line.FallbackName) ? line.DefId : line.FallbackName);
            Sprite sprite = ImageUtil.GetSpriteByName(
                ImageUtil.itemImagePath, ItemFactory.ResolveIcon(def?.icon));

            NexusUiFactory.CreateBox(
                parent,
                $"Row {index}",
                new Vector2(6f, y),
                new Vector2(width - 12f, RowHeight - 6f),
                NexusTheme.SurfaceRaised,
                NexusTheme.BorderSoft);
            NexusUiFactory.CreateIcon(
                parent,
                $"Icon {index}",
                sprite,
                new Vector2(16f, y + 7f),
                new Vector2(42f, 42f),
                Color.white);
            NexusUiFactory.CreateText(
                parent,
                $"Name {index}",
                name,
                new Vector2(70f, y + 7f),
                new Vector2(width - 210f, 24f),
                14f,
                QualityColor(line.Quality),
                TextAlignmentOptions.Left,
                FontStyles.Bold);
            NexusUiFactory.CreateText(
                parent,
                $"Meta {index}",
                UiText.InventoryQty(line.Quality, line.Quantity),
                new Vector2(70f, y + 30f),
                new Vector2(width - 210f, 20f),
                11f,
                NexusTheme.MutedText);
            NexusUiFactory.CreateText(
                parent,
                $"Qty {index}",
                $"+{line.Quantity}",
                new Vector2(width - 132f, y + 16f),
                new Vector2(110f, 26f),
                17f,
                NexusTheme.Gold,
                TextAlignmentOptions.Right,
                FontStyles.Bold);
        }

        private static Color QualityColor(int quality) => quality switch
        {
            <= 1 => NexusTheme.MutedText,
            2 => NexusTheme.Text,
            3 => NexusTheme.Cyan,
            4 => NexusTheme.Purple,
            _ => NexusTheme.Gold
        };

        private static Transform CreateScrollArea(Transform parent, Vector2 position, Vector2 size)
        {
            var viewport = new GameObject(
                "Reward List", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);

            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 1f);
            viewportRect.anchorMax = new Vector2(0f, 1f);
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.anchoredPosition = new Vector2(position.x, -position.y);
            viewportRect.sizeDelta = size;

            var image = viewport.GetComponent<Image>();
            image.color = NexusTheme.WithAlpha(NexusTheme.Background, 0.55f);
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
            scroll.scrollSensitivity = 24f;

            return content.transform;
        }
    }
}

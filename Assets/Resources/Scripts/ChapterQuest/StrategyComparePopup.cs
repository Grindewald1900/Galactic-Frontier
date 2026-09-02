using Assets.Resources.Scripts.UI.Nexus;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.ChapterQuest
{
    internal static class StrategyComparePopup
    {
        private static GameObject current;

        public static void Close()
        {
            if (current != null)
                Object.Destroy(current);
            current = null;
        }

        public static void Show()
        {
            Close();

            Canvas canvas = NexusUiFactory.CreateCanvas("Strategy Compare", NexusDialog.SortingOrder, true);
            if (AppShell.Instance != null)
                canvas.transform.SetParent(AppShell.Instance.transform, false);
            current = canvas.gameObject;

            GameObject scrim = NexusUiFactory.CreatePanel(
                canvas.transform, "Scrim",
                NexusTheme.WithAlpha(Color.black, 0.55f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);

            const float w = 520f;
            const float h = 220f;
            GameObject box = NexusUiFactory.CreateBox(
                scrim.transform, "Box", Vector2.zero, new Vector2(w, h),
                NexusTheme.Surface, NexusTheme.Gold);
            var rect = box.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            NexusUiFactory.CreateText(
                box.transform, "Title", UiText.ChapterStrategyTitle,
                new Vector2(24f, 16f), new Vector2(w - 48f, 28f), 16f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var body = NexusUiFactory.CreateText(
                box.transform, "Body", UiText.ChapterStrategyBody,
                new Vector2(24f, 52f), new Vector2(w - 48f, 100f), 13f, NexusTheme.Text);
            body.textWrappingMode = TextWrappingModes.Normal;

            NexusUiFactory.CreateButton(
                box.transform, "Ok", UiText.RewardConfirm,
                new Vector2((w - 160f) * 0.5f, h - 56f), new Vector2(160f, 40f),
                Close,
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.2f), NexusTheme.Gold, 14f);
        }
    }
}

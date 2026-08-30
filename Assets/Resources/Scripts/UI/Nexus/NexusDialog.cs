using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Shared modal (same visual family as <see cref="RewardPopup"/>).
    /// Destructive dialogs do not dismiss on scrim click.
    /// </summary>
    internal static class NexusDialog
    {
        public const int SortingOrder = 520;

        private const float Width = 560f;
        private static GameObject current;
        private static int openCount;

        public static bool IsOpen => openCount > 0;

        public static void Close()
        {
            if (current != null)
                UnityEngine.Object.Destroy(current);
            current = null;
            openCount = 0;
            NexusSnackbar.NotifyDialogClosed();
        }

        public static void Show(
            string title,
            string body,
            string primaryLabel,
            Action onPrimary,
            string secondaryLabel = null,
            Action onSecondary = null,
            bool dismissOnScrim = true)
        {
            Close();
            openCount = 1;

            Canvas canvas = NexusUiFactory.CreateCanvas("Nexus Dialog", SortingOrder, true);
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

            if (dismissOnScrim)
            {
                var dismiss = scrim.AddComponent<Button>();
                dismiss.transition = Selectable.Transition.None;
                dismiss.onClick.AddListener(() =>
                {
                    Close();
                    onSecondary?.Invoke();
                });
            }

            const float dialogH = 280f;
            GameObject dialog = NexusUiFactory.CreateBox(
                scrim.transform,
                "Dialog",
                Vector2.zero,
                new Vector2(Width, dialogH),
                NexusTheme.Surface,
                NexusTheme.Gold);
            dialog.GetComponent<Image>().raycastTarget = true;
            var dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;

            var block = dialog.AddComponent<Button>();
            block.transition = Selectable.Transition.None;

            NexusUiFactory.CreateText(
                dialog.transform,
                "Title",
                title ?? "",
                new Vector2(24f, 18f),
                new Vector2(Width - 48f, 30f),
                18f,
                NexusTheme.Gold,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            var bodyText = NexusUiFactory.CreateText(
                dialog.transform,
                "Body",
                body ?? "",
                new Vector2(24f, 56f),
                new Vector2(Width - 48f, 130f),
                14f,
                NexusTheme.Text);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.overflowMode = TextOverflowModes.Overflow;

            bool hasSecondary = !string.IsNullOrEmpty(secondaryLabel);
            float btnW = hasSecondary ? 200f : 220f;
            float btnY = 214f;

            if (hasSecondary)
            {
                NexusUiFactory.CreateButton(
                    dialog.transform,
                    "Secondary",
                    secondaryLabel,
                    new Vector2(24f, btnY),
                    new Vector2(btnW, 44f),
                    () =>
                    {
                        Close();
                        onSecondary?.Invoke();
                    },
                    NexusTheme.SurfaceRaised,
                    NexusTheme.Text,
                    14f);
            }

            float primaryX = hasSecondary ? Width - 24f - btnW : (Width - btnW) * 0.5f;
            NexusUiFactory.CreateButton(
                dialog.transform,
                "Primary",
                string.IsNullOrEmpty(primaryLabel) ? UiText.RewardConfirm : primaryLabel,
                new Vector2(primaryX, btnY),
                new Vector2(btnW, 44f),
                () =>
                {
                    Close();
                    onPrimary?.Invoke();
                },
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.2f),
                NexusTheme.Gold,
                14f);
        }
    }
}

using System;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.UI.Nexus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.Main
{
    /// <summary>Second-step confirmation before deleting a save on the main-menu load panel.</summary>
    internal static class LoadSaveDeleteDialog
    {
        private static GameObject overlay;

        public static void Show(Transform parent, PlayerEntity player, Action<PlayerEntity> onConfirmed)
        {
            Close();
            if (parent == null || player == null)
                return;

            overlay = NexusUiFactory.CreatePanel(
                parent, "Delete Save Overlay", NexusTheme.WithAlpha(Color.black, 0.66f),
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
            dialog.GetComponent<Image>().raycastTarget = true;

            string playerName = string.IsNullOrWhiteSpace(player.playerName) ? "Player" : player.playerName;
            NexusUiFactory.CreateText(
                dialog.transform, "Title", UiText.LoadDeleteConfirmTitle,
                new Vector2(24f, 18f), new Vector2(w - 48f, 28f), 16f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            var body = NexusUiFactory.CreateText(
                dialog.transform, "Body", UiText.LoadDeleteConfirmBody(playerName),
                new Vector2(24f, 56f), new Vector2(w - 48f, 110f), 13f, NexusTheme.MutedText);
            body.textWrappingMode = TextWrappingModes.Normal;

            NexusUiFactory.CreateButton(
                dialog.transform, "Cancel", UiText.Close,
                new Vector2(24f, 200f), new Vector2(200f, 44f),
                Close,
                NexusTheme.SurfaceRaised, NexusTheme.Text, 14f);
            NexusUiFactory.CreateButton(
                dialog.transform, "Confirm", UiText.LoadDeleteConfirm,
                new Vector2(296f, 200f), new Vector2(200f, 44f),
                () =>
                {
                    var target = player;
                    Close();
                    onConfirmed?.Invoke(target);
                },
                NexusTheme.WithAlpha(NexusTheme.Red, 0.22f), NexusTheme.Red, 14f);
        }

        public static void Close()
        {
            if (overlay != null)
                UnityEngine.Object.Destroy(overlay);
            overlay = null;
        }
    }
}

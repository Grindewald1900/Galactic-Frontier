using Assets.Resources.Scripts.Economy.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Inline shortage row: label + jump link (P6).</summary>
    internal static class ShortageJump
    {
        public static void Draw(
            Transform parent,
            string name,
            float y,
            string label,
            string jumpLabel,
            UnityAction onJump,
            bool canJump = true)
        {
            NexusUiFactory.CreateText(
                parent, name + "Label", label,
                new Vector2(0f, y), new Vector2(420f, 22f), 12f,
                canJump ? NexusTheme.Text : NexusTheme.MutedText);

            if (string.IsNullOrEmpty(jumpLabel) || onJump == null)
                return;

            var btn = NexusUiFactory.CreateButton(
                parent, name + "Jump", jumpLabel,
                new Vector2(430f, y - 2f), new Vector2(140f, 26f),
                onJump,
                canJump ? NexusTheme.WithAlpha(NexusTheme.Cyan, 0.12f) : NexusTheme.Surface,
                canJump ? NexusTheme.Cyan : NexusTheme.DimText,
                11f);
            if (!canJump)
                btn.interactable = false;
            else
            {
                var img = btn.GetComponent<Image>();
                var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
                NexusButtonStyles.Apply(btn, img, lbl, NexusButtonRole.Link);
            }
        }

        public static void DrawItem(
            Transform parent,
            string name,
            float y,
            ItemDef def,
            int need,
            int have)
        {
            if (def == null || need <= have)
                return;

            int shortfall = need - have;
            string itemName = UiText.ItemName(def);
            string label = UiText.ShortageItemLabel(itemName, shortfall);
            var sources = ItemAcquireCatalog.ForItem(def.itemDefId);
            ItemAcquireSourceDef best = null;
            foreach (var src in sources)
            {
                if (src != null && ItemTooltip.IsUnlocked(src))
                {
                    best = src;
                    break;
                }
            }

            string jump = best != null
                ? UiText.T(best.labelEn, best.labelZh)
                : UiText.ShortageGoAcquire;

            Draw(parent, name, y, label, jump, () =>
            {
                if (best != null)
                    ItemTooltip.Jump(best);
            }, best != null);
        }

        public static void DrawCredits(Transform parent, string name, float y, int need, int have)
        {
            if (need <= have) return;
            Draw(parent, name, y,
                UiText.ShortageCreditsLabel(need - have),
                UiText.ShortageGoMarket,
                () => AppShell.Instance?.Navigate(AppScreen.Market),
                true);
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    public enum NexusButtonRole
    {
        Primary,
        Secondary,
        Confirm,
        Danger,
        Disabled,
        Link
    }

    internal static class NexusButtonStyles
    {
        public static void Apply(Button button, Image image, TextMeshProUGUI label, NexusButtonRole role)
        {
            if (button == null || image == null) return;
            Color fill;
            Color text;
            bool interactable = role != NexusButtonRole.Disabled;

            switch (role)
            {
                case NexusButtonRole.Primary:
                    fill = NexusTheme.WithAlpha(NexusTheme.Gold, 0.28f);
                    text = NexusTheme.Gold;
                    break;
                case NexusButtonRole.Confirm:
                    fill = NexusTheme.WithAlpha(NexusTheme.Green, 0.22f);
                    text = NexusTheme.Green;
                    break;
                case NexusButtonRole.Danger:
                    fill = NexusTheme.WithAlpha(NexusTheme.Red, 0.22f);
                    text = NexusTheme.Red;
                    break;
                case NexusButtonRole.Link:
                    fill = Color.clear;
                    text = NexusTheme.Cyan;
                    break;
                case NexusButtonRole.Disabled:
                    fill = NexusTheme.WithAlpha(NexusTheme.Surface, 0.65f);
                    text = NexusTheme.DimText;
                    break;
                default:
                    fill = NexusTheme.WithAlpha(NexusTheme.Cyan, 0.14f);
                    text = NexusTheme.Cyan;
                    break;
            }

            image.color = fill;
            var colors = button.colors;
            colors.normalColor = fill;
            colors.highlightedColor = NexusTheme.SurfaceHover;
            colors.pressedColor = role == NexusButtonRole.Primary
                ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.45f)
                : NexusTheme.WithAlpha(NexusTheme.Cyan, 0.35f);
            colors.disabledColor = NexusTheme.WithAlpha(NexusTheme.Surface, 0.5f);
            button.colors = colors;
            button.interactable = interactable;
            if (label != null)
                label.color = text;
        }
    }
}

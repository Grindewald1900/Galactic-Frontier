using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// A self-updating horizontal progress bar. The fill width and caption are recomputed every frame
    /// from caller-supplied providers, so time-based jobs (manual craft / production lines) animate
    /// smoothly without the owning screen having to rebuild.
    /// </summary>
    internal sealed class NexusTimedBar : MonoBehaviour
    {
        private RectTransform fill;
        private float fullWidth;
        private float height;
        private TextMeshProUGUI label;
        private Func<float> ratioProvider;
        private Func<string> labelProvider;
        private Color fillColor = NexusTheme.Cyan;

        public static NexusTimedBar Attach(
            Transform parent, string name, Vector2 position, float width, float height,
            Func<float> ratio, Func<string> labelText, Color? fill = null)
        {
            var track = NexusUiFactory.CreateBox(
                parent, name, position, new Vector2(width, height),
                NexusTheme.WithAlpha(NexusTheme.Border, 0.45f), NexusTheme.BorderSoft);

            var color = fill ?? NexusTheme.Cyan;
            var fillBox = NexusUiFactory.CreateBox(
                track.transform, "Fill", new Vector2(0f, 0f),
                new Vector2(width, height), NexusTheme.WithAlpha(color, 0.55f));

            var lbl = NexusUiFactory.CreateText(
                track.transform, "Label", "",
                new Vector2(8f, 0f), new Vector2(width - 12f, height), 11f, NexusTheme.Text,
                TextAlignmentOptions.Left);

            var comp = track.AddComponent<NexusTimedBar>();
            comp.fill = fillBox.GetComponent<RectTransform>();
            comp.fullWidth = width;
            comp.height = height;
            comp.label = lbl;
            comp.ratioProvider = ratio;
            comp.labelProvider = labelText;
            comp.fillColor = color;
            comp.Apply();
            return comp;
        }

        private void Update() => Apply();

        private void Apply()
        {
            var r = Mathf.Clamp01(ratioProvider?.Invoke() ?? 0f);
            if (fill != null)
                fill.sizeDelta = new Vector2(fullWidth * r, height);
            if (label != null && labelProvider != null)
                label.text = labelProvider();
        }
    }
}

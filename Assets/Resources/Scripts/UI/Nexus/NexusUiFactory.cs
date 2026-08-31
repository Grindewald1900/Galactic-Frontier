using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    internal static class NexusUiFactory
    {
        public static Canvas CreateCanvas(string name, int sortingOrder, bool interactive)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (interactive)
                canvasObject.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        public static GameObject CreatePanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            bool raycastTarget = false)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return panel;
        }

        public static GameObject CreateBox(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            Color? outline = null)
        {
            var box = new GameObject(name, typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);
            var rect = box.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;

            var image = box.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            if (outline.HasValue)
            {
                var effect = box.AddComponent<Outline>();
                effect.effectColor = outline.Value;
                effect.effectDistance = new Vector2(1f, -1f);
                effect.useGraphicAlpha = false;
            }

            return box;
        }

        public static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            Vector2 position,
            Vector2 size,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left,
            FontStyles style = FontStyles.Normal)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = GetRuntimeFont();
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Vector2 size,
            UnityAction onClick,
            Color? color = null,
            Color? labelColor = null,
            float fontSize = 16f)
        {
            var buttonObject = CreateBox(
                parent,
                name,
                position,
                size,
                color ?? NexusTheme.SurfaceRaised,
                NexusTheme.Border);
            var image = buttonObject.GetComponent<Image>();
            image.raycastTarget = true;

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = color ?? NexusTheme.SurfaceRaised;
            colors.highlightedColor = NexusTheme.SurfaceHover;
            colors.pressedColor = NexusTheme.WithAlpha(NexusTheme.Gold, 0.65f);
            colors.selectedColor = NexusTheme.SurfaceHover;
            colors.disabledColor = NexusTheme.WithAlpha(NexusTheme.Surface, 0.65f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            var labelText = CreateText(
                buttonObject.transform,
                "Label",
                label,
                Vector2.zero,
                size,
                fontSize,
                labelColor ?? NexusTheme.Text,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            labelText.rectTransform.anchorMin = Vector2.zero;
            labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            labelText.rectTransform.anchoredPosition = Vector2.zero;
            labelText.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        public static Button CreateRoleButton(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Vector2 size,
            UnityAction onClick,
            NexusButtonRole role,
            float fontSize = 14f)
        {
            var button = CreateButton(parent, name, label, position, size, onClick,
                NexusTheme.SurfaceRaised, NexusTheme.Text, fontSize);
            NexusButtonStyles.Apply(button, button.GetComponent<Image>(),
                button.GetComponentInChildren<TextMeshProUGUI>(), role);
            return button;
        }

        public static TMP_InputField CreateInputField(
            Transform parent,
            string name,
            string placeholder,
            Vector2 position,
            Vector2 size,
            int fontSize = 14)
        {
            var root = CreateBox(parent, name, position, size, NexusTheme.SurfaceRaised, NexusTheme.Border);
            var image = root.GetComponent<Image>();
            image.raycastTarget = true;

            var input = root.AddComponent<TMP_InputField>();
            var text = CreateText(
                root.transform,
                "Text",
                string.Empty,
                new Vector2(8f, 4f),
                new Vector2(size.x - 16f, size.y - 8f),
                fontSize,
                NexusTheme.Text,
                TextAlignmentOptions.Left);
            text.raycastTarget = false;

            var placeholderText = CreateText(
                root.transform,
                "Placeholder",
                placeholder,
                new Vector2(8f, 4f),
                new Vector2(size.x - 16f, size.y - 8f),
                fontSize,
                NexusTheme.MutedText,
                TextAlignmentOptions.Left);
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.raycastTarget = false;

            input.textViewport = root.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.fontAsset = text.font;
            input.pointSize = fontSize;
            input.caretColor = NexusTheme.Gold;
            input.selectionColor = NexusTheme.WithAlpha(NexusTheme.Gold, 0.35f);
            return input;
        }

        public static Image CreateIcon(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            var iconObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            var rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;

            var image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static TMP_FontAsset GetRuntimeFont()
        {
            return CjkFontBootstrap.GetRuntimeFont();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Non-blocking marquee under the top bar. FIFO rotation of system notices.</summary>
    internal sealed class NexusNotificationBar : MonoBehaviour
    {
        private const float PixelsPerSecond = 70f;
        private const float HoldEmptySeconds = 0.4f;

        private static NexusNotificationBar instance;
        private readonly Queue<string> queue = new();
        private TextMeshProUGUI label;
        private RectTransform labelRect;
        private RectTransform viewportRect;
        private Coroutine loop;

        public static NexusNotificationBar Attach(Transform parent)
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
                instance = null;
            }

            var root = new GameObject("Notification Bar", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = root.GetComponent<Image>();
            image.color = NexusTheme.WithAlpha(NexusTheme.SurfaceRaised, 0.9f);
            image.raycastTarget = false;

            var text = NexusUiFactory.CreateText(
                root.transform,
                "Marquee",
                "",
                Vector2.zero,
                new Vector2(800f, 20f),
                12f,
                NexusTheme.Cyan);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(0f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;

            var bar = root.AddComponent<NexusNotificationBar>();
            bar.label = text;
            bar.labelRect = textRect;
            bar.viewportRect = rect;
            instance = bar;
            bar.loop = bar.StartCoroutine(bar.Run());
            return bar;
        }

        public static void Push(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            BridgeEventLog.Push(BridgeLogCategory.System, message, message);
            if (instance == null)
            {
                Debug.Log("[NOTIFY] " + message);
                return;
            }

            instance.queue.Enqueue(message);
        }

        public static void ClearQueue()
        {
            instance?.queue.Clear();
        }

        private IEnumerator Run()
        {
            while (true)
            {
                if (queue.Count == 0)
                {
                    label.text = UiText.NotificationIdle;
                    labelRect.anchoredPosition = new Vector2(12f, 0f);
                    yield return new WaitForSecondsRealtime(HoldEmptySeconds);
                    continue;
                }

                string next = queue.Dequeue();
                label.text = next;
                label.ForceMeshUpdate();
                float textW = Mathf.Max(label.preferredWidth + 24f, 200f);
                labelRect.sizeDelta = new Vector2(textW, 20f);
                float viewW = viewportRect.rect.width;
                labelRect.anchoredPosition = new Vector2(viewW, 0f);

                float distance = viewW + textW;
                float duration = Mathf.Max(4f, distance / PixelsPerSecond);
                float t = 0f;
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime;
                    float x = Mathf.Lerp(viewW, -textW, t / duration);
                    labelRect.anchoredPosition = new Vector2(x, 0f);
                    yield return null;
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}

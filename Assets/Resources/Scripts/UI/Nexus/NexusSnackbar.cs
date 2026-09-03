using System.Collections;
using System.Collections.Generic;
using Assets.Resources.Scripts.Deck.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Global bottom toast with message queue, configurable duration, tap-to-dismiss, and fade out.
    /// Messages wait while a modal dialog is open.
    /// </summary>
    internal sealed class NexusSnackbar : MonoBehaviour
    {
        public const int SortingOrder = 510;

        private const float DefaultHoldSeconds = 3.2f;
        private const float FadeSeconds = 0.28f;

        private struct QueuedMessage
        {
            public string Text;
            public float HoldSeconds;
        }

        private static NexusSnackbar instance;
        private static readonly Queue<QueuedMessage> queue = new();
        private static bool showing;

        private TextMeshProUGUI label;
        private CanvasGroup barGroup;
        private Coroutine routine;
        private bool dismissRequested;
        private float dismissEnabledAt;

        public static void Show(string message, float holdSeconds = DefaultHoldSeconds)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            Ensure();

            var item = new QueuedMessage { Text = message.Trim(), HoldSeconds = Mathf.Max(0.5f, holdSeconds) };
            if (NexusDialog.IsOpen)
            {
                queue.Enqueue(item);
                return;
            }

            if (showing)
            {
                queue.Enqueue(item);
                return;
            }

            instance.Present(item);
        }

        public static void Show(DeckCommandResult result, float holdSeconds = DefaultHoldSeconds)
        {
            if (result == null || result.Success) return;
            Show(UiText.DeckCommandMessage(result), holdSeconds);
        }

        public static void NotifyDialogClosed()
        {
            if (instance == null || NexusDialog.IsOpen) return;
            TryPresentNext();
        }

        public static void Close()
        {
            queue.Clear();
            if (instance == null) return;
            instance.StopRoutine();
            showing = false;
            instance.dismissRequested = false;
            instance.SetVisible(false);
        }

        private static void Ensure()
        {
            if (instance != null) return;

            Canvas canvas = NexusUiFactory.CreateCanvas("Nexus Snackbar", SortingOrder, true);
            if (AppShell.Instance != null)
                canvas.transform.SetParent(AppShell.Instance.transform, false);

            var host = canvas.gameObject.AddComponent<NexusSnackbar>();
            instance = host;
            host.BuildUi(canvas.transform);
        }

        private void BuildUi(Transform root)
        {
            GameObject bar = NexusUiFactory.CreateBox(
                root,
                "Bar",
                Vector2.zero,
                new Vector2(720f, 56f),
                NexusTheme.SurfaceRaised,
                NexusTheme.Gold);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 48f);
            bar.GetComponent<Image>().raycastTarget = true;

            barGroup = bar.AddComponent<CanvasGroup>();
            barGroup.alpha = 0f;
            barGroup.blocksRaycasts = false;

            label = NexusUiFactory.CreateText(
                bar.transform,
                "Text",
                "",
                new Vector2(20f, 12f),
                new Vector2(680f, 32f),
                14f,
                NexusTheme.Text,
                TextAlignmentOptions.Center);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void Present(QueuedMessage item)
        {
            StopRoutine();
            dismissRequested = false;
            label.text = item.Text;
            showing = true;
            routine = StartCoroutine(ShowRoutine(item.HoldSeconds));
        }

        private void Update()
        {
            if (!showing || dismissRequested) return;
            if (NexusDialog.IsOpen) return;
            if (Time.unscaledTime < dismissEnabledAt) return;
            if (Input.GetMouseButtonDown(0))
                RequestDismiss();
        }

        private IEnumerator ShowRoutine(float holdSeconds)
        {
            yield return Fade(barGroup, 0f, 1f, FadeSeconds);
            dismissEnabledAt = Time.unscaledTime + 0.18f;

            float elapsed = 0f;
            while (elapsed < holdSeconds && !dismissRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return Fade(barGroup, barGroup.alpha, 0f, FadeSeconds);
            FinishCurrent();
        }

        private void RequestDismiss()
        {
            if (!showing) return;
            dismissRequested = true;
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = StartCoroutine(DismissFadeOut());
            }
        }

        private IEnumerator DismissFadeOut()
        {
            yield return Fade(barGroup, barGroup.alpha, 0f, FadeSeconds);
            FinishCurrent();
        }

        private void FinishCurrent()
        {
            StopRoutine();
            showing = false;
            dismissRequested = false;
            barGroup.blocksRaycasts = false;
            TryPresentNext();
        }

        private static void TryPresentNext()
        {
            if (instance == null || showing || NexusDialog.IsOpen || queue.Count == 0)
                return;
            instance.Present(queue.Dequeue());
        }

        private void SetVisible(bool visible)
        {
            barGroup.alpha = visible ? 1f : 0f;
            barGroup.blocksRaycasts = false;
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float t = 0f;
            group.alpha = from;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }

            group.alpha = to;
        }

        private void StopRoutine()
        {
            if (routine == null) return;
            StopCoroutine(routine);
            routine = null;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}

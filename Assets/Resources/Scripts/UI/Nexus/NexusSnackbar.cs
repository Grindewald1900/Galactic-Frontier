using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Bottom toast. Queued while a dialog is open; auto-hides after a few seconds.</summary>
    internal sealed class NexusSnackbar : MonoBehaviour
    {
        private const int SortingOrder = 510;
        private const float HoldSeconds = 3.2f;

        private static NexusSnackbar instance;
        private static readonly System.Collections.Generic.Queue<string> pending = new();

        private TextMeshProUGUI label;
        private CanvasGroup group;
        private Coroutine routine;

        public static void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            Ensure();
            if (NexusDialog.IsOpen)
            {
                pending.Enqueue(message);
                return;
            }

            instance.Present(message);
        }

        public static void NotifyDialogClosed()
        {
            if (instance == null) return;
            if (pending.Count == 0) return;
            if (NexusDialog.IsOpen) return;
            instance.Present(pending.Dequeue());
        }

        public static void Close()
        {
            pending.Clear();
            if (instance == null) return;
            if (instance.routine != null)
                instance.StopCoroutine(instance.routine);
            instance.routine = null;
            instance.group.alpha = 0f;
            instance.group.blocksRaycasts = false;
        }

        private static void Ensure()
        {
            if (instance != null) return;

            Canvas canvas = NexusUiFactory.CreateCanvas("Nexus Snackbar", SortingOrder, true);
            if (AppShell.Instance != null)
                canvas.transform.SetParent(AppShell.Instance.transform, false);

            var host = canvas.gameObject.AddComponent<NexusSnackbar>();
            instance = host;

            GameObject bar = NexusUiFactory.CreateBox(
                canvas.transform,
                "Bar",
                Vector2.zero,
                new Vector2(640f, 52f),
                NexusTheme.SurfaceRaised,
                NexusTheme.Gold);
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            bar.GetComponent<Image>().raycastTarget = true;

            host.group = bar.AddComponent<CanvasGroup>();
            host.group.alpha = 0f;
            host.group.blocksRaycasts = false;

            host.label = NexusUiFactory.CreateText(
                bar.transform,
                "Text",
                "",
                new Vector2(16f, 10f),
                new Vector2(608f, 32f),
                14f,
                NexusTheme.Text,
                TextAlignmentOptions.Center);
            host.label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void Present(string message)
        {
            if (routine != null)
                StopCoroutine(routine);
            label.text = message;
            group.alpha = 1f;
            group.blocksRaycasts = true;
            routine = StartCoroutine(HoldThenHide());
        }

        private IEnumerator HoldThenHide()
        {
            yield return new WaitForSecondsRealtime(HoldSeconds);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            routine = null;
            if (pending.Count > 0 && !NexusDialog.IsOpen)
                Present(pending.Dequeue());
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}

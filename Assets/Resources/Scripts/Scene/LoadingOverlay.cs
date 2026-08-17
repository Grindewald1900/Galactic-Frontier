using System.Collections;
using Assets.Resources.Scripts.UI.Nexus;
using Assets.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.Scene
{
    /// <summary>
    /// Opaque black cover held across scene loads so the legacy prefab UI in a freshly loaded scene
    /// never flashes before the app shell has taken it over.
    /// </summary>
    /// <remarks>
    /// Self-bootstrapping and persistent, because scene loads are kicked off from several places that
    /// have no shared owner. The cover lifts when the loaded scene calls <see cref="NotifySceneReady"/>;
    /// <see cref="ReadyTimeoutSeconds"/> is a safety net so a scene without a signaller cannot strand it.
    /// </remarks>
    public sealed class LoadingOverlay : MonoBehaviour
    {
        private const int SortingOrder = 32000;
        private const float ReadyTimeoutSeconds = 15f;
        private const float FadeOutSeconds = 0.22f;

        private static LoadingOverlay instance;

        private CanvasGroup group;
        private TextMeshProUGUI label;
        private RectTransform pulseBar;
        private bool sceneReady;
        private float elapsed;

        /// <summary>Covers the screen, then loads the scene asynchronously so the animation keeps running.</summary>
        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new System.ArgumentException("Scene name cannot be null or empty.", nameof(sceneName));

            LoadingOverlay overlay = EnsureInstance();
            if (overlay == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            overlay.BeginLoad(sceneName);
        }

        /// <summary>Shows the cover without starting a load, for callers that navigate on their own.</summary>
        public static void Show()
        {
            EnsureInstance()?.SetVisible(true);
        }

        /// <summary>Lets the newly loaded scene report that its own UI is built and safe to reveal.</summary>
        public static void NotifySceneReady()
        {
            if (instance != null)
                instance.sceneReady = true;
        }

        private static LoadingOverlay EnsureInstance()
        {
            if (instance != null)
                return instance;

            var host = new GameObject("Loading Overlay");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<LoadingOverlay>();
            instance.Build();
            return instance;
        }

        private void Build()
        {
            LocalizationUtil.Initialize();

            Canvas canvas = NexusUiFactory.CreateCanvas("Loading Canvas", SortingOrder, true);
            canvas.transform.SetParent(transform, false);
            group = canvas.gameObject.AddComponent<CanvasGroup>();

            NexusUiFactory.CreatePanel(
                canvas.transform,
                "Blackout",
                Color.black,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                true);

            label = NexusUiFactory.CreateText(
                canvas.transform,
                "Loading Label",
                UiText.Loading,
                Vector2.zero,
                new Vector2(600f, 60f),
                34f,
                NexusTheme.Text,
                TextAlignmentOptions.Center,
                FontStyles.Bold);
            Center(label.rectTransform, new Vector2(0f, 12f), new Vector2(600f, 60f));

            GameObject bar = NexusUiFactory.CreateBox(
                canvas.transform,
                "Pulse Bar",
                Vector2.zero,
                new Vector2(140f, 3f),
                NexusTheme.Gold);
            pulseBar = bar.GetComponent<RectTransform>();
            Center(pulseBar, new Vector2(0f, -34f), new Vector2(140f, 3f));

            SetVisible(false);
        }

        private static void Center(RectTransform rect, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        private void BeginLoad(string sceneName)
        {
            SetVisible(true);
            StopAllCoroutines();
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            sceneReady = false;

            // Give the cover one frame to render before the loader stalls the main thread.
            yield return null;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"[LOADING] Could not start an async load for '{sceneName}'.");
                SetVisible(false);
                yield break;
            }

            while (!operation.isDone)
                yield return null;

            float deadline = Time.unscaledTime + ReadyTimeoutSeconds;
            while (!sceneReady && Time.unscaledTime < deadline)
                yield return null;

            if (!sceneReady)
                Debug.LogWarning($"[LOADING] Scene '{sceneName}' never reported ready; revealing anyway.");

            // One more frame so the shell's first layout pass lands behind the cover.
            yield return null;
            yield return FadeOut();
        }

        private IEnumerator FadeOut()
        {
            if (group == null)
            {
                SetVisible(false);
                yield break;
            }

            group.blocksRaycasts = false;
            for (float t = 0f; t < FadeOutSeconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / FadeOutSeconds;
                yield return null;
            }

            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (group == null)
                return;

            if (visible)
            {
                elapsed = 0f;
                RefreshLocalizedText();
            }

            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
            group.gameObject.SetActive(visible);
        }

        private void RefreshLocalizedText()
        {
            if (label != null)
                label.text = UiText.Loading;
        }

        private void Update()
        {
            if (group == null || !group.gameObject.activeSelf)
                return;

            elapsed += Time.unscaledDeltaTime;

            if (label != null)
            {
                int dots = Mathf.FloorToInt(elapsed * 2.5f) % 4;
                label.text = UiText.Loading + new string('.', dots);
                label.alpha = Mathf.Lerp(0.45f, 1f, (Mathf.Sin(elapsed * 3.2f) + 1f) * 0.5f);
            }

            if (pulseBar != null)
            {
                float width = Mathf.Lerp(60f, 220f, (Mathf.Sin(elapsed * 2.4f) + 1f) * 0.5f);
                pulseBar.sizeDelta = new Vector2(width, pulseBar.sizeDelta.y);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}

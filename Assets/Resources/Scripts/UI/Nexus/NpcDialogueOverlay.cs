using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Dialogue.Domain;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Fullscreen NPC dialogue overlay with portrait, controls, and history log.</summary>
    internal static class NpcDialogueOverlay
    {
        public const int SortingOrder = 510;

        private static NpcDialogueController active;

        public static bool IsOpen => active != null;

        public static void Show(DialogueScriptDef script, Action onComplete = null)
        {
            Close();

            Canvas canvas = NexusUiFactory.CreateCanvas("NPC Dialogue", SortingOrder, true);
            if (AppShell.Instance != null)
                canvas.transform.SetParent(AppShell.Instance.transform, false);

            active = canvas.gameObject.AddComponent<NpcDialogueController>();
            active.Begin(script, onComplete);
        }

        public static void Close()
        {
            if (active == null) return;
            var go = active.gameObject;
            active = null;
            UnityEngine.Object.Destroy(go);
        }
    }

    internal sealed class NpcDialogueController : MonoBehaviour
    {
        private const float DialogueBarHeight = 272f;
        private const float FastForwardInterval = 1.35f;

        private DialogueScriptDef script;
        private Action onComplete;
        private int currentIndex;
        private bool fastForward;
        private bool logOpen;
        private float fastForwardTimer;

        private TextMeshProUGUI speakerLabel;
        private TextMeshProUGUI bodyLabel;
        private TextMeshProUGUI pageLabel;
        private Image portraitImage;
        private GameObject portraitRoot;
        private GameObject logPanel;
        private Transform logContent;
        private Button fastForwardButton;
        private Button nextButton;
        private Button prevButton;

        public void Begin(DialogueScriptDef dialogueScript, Action completed)
        {
            script = dialogueScript;
            onComplete = completed;
            currentIndex = 0;
            fastForward = false;
            logOpen = false;
            BuildUi();
            RefreshLine();
        }

        private void Update()
        {
            if (script == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseDialogue();
                return;
            }

            if (logOpen) return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                Advance();

            if (!fastForward) return;

            fastForwardTimer -= Time.unscaledDeltaTime;
            if (fastForwardTimer <= 0f)
            {
                fastForwardTimer = FastForwardInterval;
                Advance();
            }
        }

        private void BuildUi()
        {
            Transform root = transform;

            NexusUiFactory.CreatePanel(
                root, "Scrim",
                NexusTheme.WithAlpha(Color.black, 0.55f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);

            portraitRoot = NexusUiFactory.CreateBox(
                root, "Portrait",
                new Vector2(72f, 1080f - DialogueBarHeight - 620f),
                new Vector2(420f, 600f),
                NexusTheme.WithAlpha(NexusTheme.Surface, 0.92f),
                NexusTheme.BorderSoft);

            var portraitIconGo = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
            portraitIconGo.transform.SetParent(portraitRoot.transform, false);
            var portraitRect = portraitIconGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = Vector2.zero;
            portraitRect.anchorMax = Vector2.one;
            portraitRect.offsetMin = new Vector2(12f, 12f);
            portraitRect.offsetMax = new Vector2(-12f, -12f);
            portraitImage = portraitIconGo.GetComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.color = Color.white;
            portraitImage.raycastTarget = false;

            GameObject dialogueBar = NexusUiFactory.CreatePanel(
                root, "DialogueBar",
                NexusTheme.WithAlpha(NexusTheme.Surface, 0.96f),
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, DialogueBarHeight));

            var barOutline = dialogueBar.AddComponent<Outline>();
            barOutline.effectColor = NexusTheme.BorderSoft;
            barOutline.effectDistance = new Vector2(0f, 2f);

            speakerLabel = NexusUiFactory.CreateText(
                dialogueBar.transform, "Speaker",
                "",
                new Vector2(32f, 18f), new Vector2(720f, 32f), 20f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);

            pageLabel = NexusUiFactory.CreateText(
                dialogueBar.transform, "Page",
                "",
                new Vector2(1720f, 22f), new Vector2(160f, 24f), 12f, NexusTheme.MutedText,
                TextAlignmentOptions.Right);

            bodyLabel = NexusUiFactory.CreateText(
                dialogueBar.transform, "Body",
                "",
                new Vector2(32f, 56f), new Vector2(1840f, 120f), 16f, NexusTheme.Text);
            bodyLabel.textWrappingMode = TextWrappingModes.Normal;
            bodyLabel.overflowMode = TextOverflowModes.Overflow;

            GameObject textHit = NexusUiFactory.CreatePanel(
                dialogueBar.transform, "TextHit",
                new Color(0f, 0f, 0f, 0.01f),
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(24f, 52f), new Vector2(-24f, -64f),
                true);
            var textBtn = textHit.AddComponent<Button>();
            textBtn.transition = Selectable.Transition.None;
            textBtn.onClick.AddListener(Advance);

            float btnY = DialogueBarHeight - 52f;
            const float btnW = 118f;
            const float gap = 10f;
            float x = 32f;

            prevButton = CreateControlButton(dialogueBar.transform, "Prev", UiText.DialogueBack,
                new Vector2(x, btnY), new Vector2(btnW, 40f), StepBack);
            x += btnW + gap;

            nextButton = CreateControlButton(dialogueBar.transform, "Next", UiText.DialogueForward,
                new Vector2(x, btnY), new Vector2(btnW, 40f), Advance);
            x += btnW + gap;

            fastForwardButton = CreateControlButton(dialogueBar.transform, "Fast", UiText.DialogueFastForward,
                new Vector2(x, btnY), new Vector2(btnW, 40f), ToggleFastForward);
            x += btnW + gap;

            CreateControlButton(dialogueBar.transform, "Skip", UiText.DialogueSkip,
                new Vector2(x, btnY), new Vector2(btnW, 40f), Skip);
            x += btnW + gap;

            CreateControlButton(dialogueBar.transform, "Log", UiText.DialogueLog,
                new Vector2(x, btnY), new Vector2(btnW, 40f), ToggleLog);
            x += btnW + gap;

            CreateControlButton(dialogueBar.transform, "Close", UiText.DialogueClose,
                new Vector2(x, btnY), new Vector2(btnW, 40f), CloseDialogue);

            BuildLogPanel(root);
        }

        private void BuildLogPanel(Transform root)
        {
            logPanel = NexusUiFactory.CreatePanel(
                root, "LogPanel",
                NexusTheme.WithAlpha(NexusTheme.Surface, 0.98f),
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-640f, 24f), new Vector2(-24f, -24f),
                true);
            logPanel.SetActive(false);

            NexusUiFactory.CreateText(
                logPanel.transform, "LogTitle", UiText.DialogueLogTitle,
                new Vector2(20f, 16f), new Vector2(520f, 28f), 16f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateButton(
                logPanel.transform, "LogClose", UiText.DialogueClose,
                new Vector2(520f, 12f), new Vector2(88f, 32f),
                () => SetLogOpen(false),
                NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);

            var scrollGo = new GameObject("LogScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(logPanel.transform, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(16f, 16f);
            scrollRect.offsetMax = new Vector2(-16f, -56f);
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.clear;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            logContent = content.transform;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 900f);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
        }

        private static Button CreateControlButton(
            Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityAction onClick)
        {
            return NexusUiFactory.CreateButton(
                parent, name, label, pos, size, onClick,
                NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);
        }

        private void RefreshLine()
        {
            if (script?.lines == null || script.lines.Count == 0)
                return;

            currentIndex = Mathf.Clamp(currentIndex, 0, script.lines.Count - 1);
            var line = script.lines[currentIndex];

            speakerLabel.text = UiText.T(line.speakerNameEn, line.speakerNameZh);
            bodyLabel.text = UiText.T(line.textEn, line.textZh);
            pageLabel.text = UiText.DialoguePage(currentIndex + 1, script.lines.Count);

            bool hasPortrait = !string.IsNullOrEmpty(line.portraitId);
            portraitRoot.SetActive(hasPortrait);
            if (hasPortrait)
            {
                var sprite = ImageUtil.GetSpriteByName(
                    ImageUtil.characterImagePath, line.portraitId + "_01");
                portraitImage.sprite = sprite;
                portraitImage.enabled = sprite != null;
            }

            prevButton.interactable = currentIndex > 0;
            nextButton.interactable = currentIndex < script.lines.Count - 1;

            RefreshFastForwardStyle();
            RefreshLog();
        }

        private void RefreshFastForwardStyle()
        {
            if (fastForwardButton == null) return;
            var colors = fastForwardButton.colors;
            colors.normalColor = fastForward
                ? NexusTheme.WithAlpha(NexusTheme.Cyan, 0.28f)
                : NexusTheme.SurfaceRaised;
            fastForwardButton.colors = colors;
        }

        private void RefreshLog()
        {
            if (logContent == null || script?.lines == null) return;
            if (!logOpen)
                return;

            for (int i = logContent.childCount - 1; i >= 0; i--)
                Destroy(logContent.GetChild(i).gameObject);

            int last = Mathf.Min(currentIndex, script.lines.Count - 1);
            const float rowH = 72f;
            float y = 0f;
            for (int i = 0; i <= last; i++)
            {
                var line = script.lines[i];
                bool current = i == currentIndex;
                string speaker = UiText.T(line.speakerNameEn, line.speakerNameZh);
                string text = UiText.T(line.textEn, line.textZh);
                string label = speaker + "\n" + text;
                int captured = i;

                var btn = NexusUiFactory.CreateButton(
                    logContent, "LogLine" + i, label,
                    new Vector2(0f, y), new Vector2(560f, rowH),
                    () =>
                    {
                        currentIndex = captured;
                        SetLogOpen(false);
                        RefreshLine();
                    },
                    current
                        ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f)
                        : NexusTheme.WithAlpha(NexusTheme.SurfaceRaised, 0.85f),
                    current ? NexusTheme.Gold : NexusTheme.Text,
                    11f);

                var labelTmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (labelTmp != null)
                {
                    labelTmp.textWrappingMode = TextWrappingModes.Normal;
                    labelTmp.overflowMode = TextOverflowModes.Ellipsis;
                    labelTmp.alignment = TextAlignmentOptions.TopLeft;
                }

                y += rowH + 8f;
            }

            var contentRect = logContent.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0f, Mathf.Max(120f, y));
        }

        private void Advance()
        {
            if (script?.lines == null) return;
            if (currentIndex < script.lines.Count - 1)
            {
                currentIndex++;
                fastForwardTimer = FastForwardInterval;
                RefreshLine();
            }
            else
            {
                CompleteDialogue();
            }
        }

        private void StepBack()
        {
            if (currentIndex <= 0) return;
            currentIndex--;
            fastForward = false;
            RefreshFastForwardStyle();
            RefreshLine();
        }

        private void ToggleFastForward()
        {
            fastForward = !fastForward;
            fastForwardTimer = 0.35f;
            RefreshFastForwardStyle();
        }

        private void Skip() => CompleteDialogue();

        private void ToggleLog() => SetLogOpen(!logOpen);

        private void SetLogOpen(bool open)
        {
            logOpen = open;
            if (logPanel != null)
                logPanel.SetActive(open);
            if (open)
            {
                fastForward = false;
                RefreshFastForwardStyle();
                RefreshLog();
            }
        }

        private void CloseDialogue()
        {
            var cb = onComplete;
            NpcDialogueOverlay.Close();
            cb?.Invoke();
        }

        private void CompleteDialogue()
        {
            var cb = onComplete;
            NpcDialogueOverlay.Close();
            cb?.Invoke();
        }
    }
}

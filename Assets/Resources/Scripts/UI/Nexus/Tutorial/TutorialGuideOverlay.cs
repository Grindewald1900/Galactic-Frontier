using System;
using Assets.Resources.Scripts.Dialogue.Domain;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus.Tutorial
{
  /// <summary>Spotlight overlay: dimmed blocker + highlighted target + compact NPC hint bar.</summary>
  internal static class TutorialGuideOverlay
  {
    public const int SortingOrder = 480;

    private static TutorialGuideController active;

    public static bool IsOpen => active != null;

    public static void Show(
      RectTransform target,
      Button targetButton,
      DialogueScriptDef script,
      Action onTargetActivated,
      Action onDismiss = null)
    {
      Close();

      if (target == null)
      {
        onDismiss?.Invoke();
        return;
      }

      Canvas canvas = NexusUiFactory.CreateCanvas("Tutorial Guide", SortingOrder, true);
      if (AppShell.Instance != null)
        canvas.transform.SetParent(AppShell.Instance.transform, false);

      active = canvas.gameObject.AddComponent<TutorialGuideController>();
      active.Begin(target, targetButton, script, onTargetActivated, onDismiss);
    }

    public static void Close()
    {
      if (active == null) return;
      var go = active.gameObject;
      active = null;
      UnityEngine.Object.Destroy(go);
    }
  }

  internal sealed class TutorialGuideController : MonoBehaviour
  {
    private const float HintBarHeight = 196f;
    private const float HighlightPadding = 10f;

    private RectTransform target;
    private Button targetButton;
    private DialogueScriptDef script;
    private Action onTargetActivated;
    private Action onDismiss;
    private int lineIndex;

    private RectTransform overlayRoot;
    private RectTransform topPanel;
    private RectTransform leftPanel;
    private RectTransform rightPanel;
    private RectTransform bottomPanel;
    private RectTransform highlightRing;
    private Button holeProxy;
    private TextMeshProUGUI speakerLabel;
    private TextMeshProUGUI bodyLabel;
    private Image portraitImage;
    private GameObject portraitRoot;

    public void Begin(
      RectTransform highlightTarget,
      Button highlightButton,
      DialogueScriptDef dialogueScript,
      Action targetActivated,
      Action dismissed)
    {
      target = highlightTarget;
      targetButton = highlightButton;
      script = dialogueScript;
      onTargetActivated = targetActivated;
      onDismiss = dismissed;
      lineIndex = 0;
      BuildUi();
      RefreshLine();
      UpdatePanels();
    }

    private void LateUpdate()
    {
      if (target == null) return;
      UpdatePanels();
      PulseHighlight();
    }

    private void BuildUi()
    {
      overlayRoot = GetComponent<RectTransform>();
      if (overlayRoot == null)
        overlayRoot = gameObject.AddComponent<RectTransform>();

      Color dim = NexusTheme.WithAlpha(Color.black, 0.62f);
      topPanel = CreateBlocker("Top", dim);
      leftPanel = CreateBlocker("Left", dim);
      rightPanel = CreateBlocker("Right", dim);
      bottomPanel = CreateBlocker("Bottom", dim);

      highlightRing = NexusUiFactory.CreateBox(
        transform, "Highlight",
        Vector2.zero, new Vector2(120f, 44f),
        new Color(1f, 1f, 1f, 0.04f),
        NexusTheme.Gold).GetComponent<RectTransform>();
      highlightRing.SetAsLastSibling();

      var holeGo = new GameObject("HoleProxy", typeof(RectTransform), typeof(Image), typeof(Button));
      holeGo.transform.SetParent(transform, false);
      holeProxy = holeGo.GetComponent<Button>();
      var holeImage = holeGo.GetComponent<Image>();
      holeImage.color = new Color(1f, 1f, 1f, 0.02f);
      holeImage.raycastTarget = true;
      holeProxy.transition = Selectable.Transition.None;
      holeProxy.onClick.AddListener(OnHoleClicked);

      BuildHintBar();
    }

    private RectTransform CreateBlocker(string name, Color color)
    {
      var panel = NexusUiFactory.CreatePanel(
        transform, name, color,
        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
      var btn = panel.AddComponent<Button>();
      btn.transition = Selectable.Transition.None;
      btn.onClick.AddListener(() => { });
      return panel.GetComponent<RectTransform>();
    }

    private void BuildHintBar()
    {
      GameObject bar = NexusUiFactory.CreatePanel(
        transform, "HintBar",
        NexusTheme.WithAlpha(NexusTheme.Surface, 0.97f),
        new Vector2(0f, 0f), new Vector2(1f, 0f),
        new Vector2(0f, 0f), new Vector2(0f, HintBarHeight));

      var outline = bar.AddComponent<Outline>();
      outline.effectColor = NexusTheme.BorderSoft;
      outline.effectDistance = new Vector2(0f, 2f);

      portraitRoot = NexusUiFactory.CreateBox(
        bar.transform, "Portrait",
        new Vector2(20f, 24f), new Vector2(96f, 148f),
        NexusTheme.WithAlpha(NexusTheme.SurfaceRaised, 0.95f),
        NexusTheme.Gold);

      var portraitIconGo = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
      portraitIconGo.transform.SetParent(portraitRoot.transform, false);
      var portraitRect = portraitIconGo.GetComponent<RectTransform>();
      portraitRect.anchorMin = Vector2.zero;
      portraitRect.anchorMax = Vector2.one;
      portraitRect.offsetMin = new Vector2(8f, 8f);
      portraitRect.offsetMax = new Vector2(-8f, -8f);
      portraitImage = portraitIconGo.GetComponent<Image>();
      portraitImage.preserveAspect = true;
      portraitImage.raycastTarget = false;

      speakerLabel = NexusUiFactory.CreateText(
        bar.transform, "Speaker", "",
        new Vector2(132f, 118f), new Vector2(520f, 28f), 18f, NexusTheme.Gold,
        TextAlignmentOptions.Left, FontStyles.Bold);

      bodyLabel = NexusUiFactory.CreateText(
        bar.transform, "Body", "",
        new Vector2(132f, 24f), new Vector2(1180f, 88f), 15f, NexusTheme.Text);
      bodyLabel.textWrappingMode = TextWrappingModes.Normal;

      NexusUiFactory.CreateButton(
        bar.transform, "Continue", UiText.DialogueForward,
        new Vector2(1340f, 24f), new Vector2(140f, 40f),
        AdvanceLine,
        NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan, 13f);
    }

    private void RefreshLine()
    {
      if (script?.lines == null || script.lines.Count == 0)
      {
        if (speakerLabel != null) speakerLabel.text = UiText.BrandTitle;
        if (bodyLabel != null) bodyLabel.text = "";
        if (portraitRoot != null) portraitRoot.SetActive(false);
        return;
      }

      lineIndex = Mathf.Clamp(lineIndex, 0, script.lines.Count - 1);
      var line = script.lines[lineIndex];
      if (speakerLabel != null)
        speakerLabel.text = UiText.T(line.speakerNameEn, line.speakerNameZh);
      if (bodyLabel != null)
        bodyLabel.text = UiText.T(line.textEn, line.textZh);

      bool hasPortrait = !string.IsNullOrEmpty(line.portraitId);
      if (portraitRoot != null)
        portraitRoot.SetActive(hasPortrait);
      if (hasPortrait && portraitImage != null)
      {
        var sprite = UnityEngine.Resources.Load<Sprite>(
          ImageUtil.characterImagePath + line.portraitId + "_01");
        portraitImage.sprite = sprite;
        portraitImage.enabled = sprite != null;
      }
    }

    private void AdvanceLine()
    {
      if (script?.lines == null || script.lines.Count <= 1)
        return;
      if (lineIndex < script.lines.Count - 1)
      {
        lineIndex++;
        RefreshLine();
      }
    }

    private void OnHoleClicked()
    {
      if (targetButton != null && targetButton.interactable)
        targetButton.onClick.Invoke();
      else if (targetButton != null)
        targetButton.onClick.Invoke();

      onTargetActivated?.Invoke();
    }

    private void UpdatePanels()
    {
      if (overlayRoot == null || target == null) return;

      var corners = new Vector3[4];
      target.GetWorldCorners(corners);

      Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
      Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
      if (min.x > max.x) (min.x, max.x) = (max.x, min.x);
      if (min.y > max.y) (min.y, max.y) = (max.y, min.y);

      min -= Vector2.one * HighlightPadding;
      max += Vector2.one * HighlightPadding;

      RectTransformUtility.ScreenPointToLocalPointInRectangle(
        overlayRoot, min, null, out Vector2 localMin);
      RectTransformUtility.ScreenPointToLocalPointInRectangle(
        overlayRoot, max, null, out Vector2 localMax);

      float holeLeft = localMin.x;
      float holeRight = localMax.x;
      float holeBottom = localMin.y;
      float holeTop = localMax.y;

      Rect overlay = overlayRoot.rect;
      SetPanel(topPanel, new Vector2(overlay.xMin, holeTop), new Vector2(overlay.xMax, overlay.yMax));
      SetPanel(bottomPanel, new Vector2(overlay.xMin, overlay.yMin), new Vector2(overlay.xMax, holeBottom));
      SetPanel(leftPanel, new Vector2(overlay.xMin, holeBottom), new Vector2(holeLeft, holeTop));
      SetPanel(rightPanel, new Vector2(holeRight, holeBottom), new Vector2(overlay.xMax, holeTop));

      if (highlightRing != null)
      {
        highlightRing.anchorMin = highlightRing.anchorMax = new Vector2(0.5f, 0.5f);
        highlightRing.pivot = new Vector2(0.5f, 0.5f);
        highlightRing.anchoredPosition = (localMin + localMax) * 0.5f;
        highlightRing.sizeDelta = localMax - localMin;
      }

      if (holeProxy != null)
      {
        var holeRect = holeProxy.GetComponent<RectTransform>();
        holeRect.anchorMin = holeRect.anchorMax = new Vector2(0.5f, 0.5f);
        holeRect.pivot = new Vector2(0.5f, 0.5f);
        holeRect.anchoredPosition = (localMin + localMax) * 0.5f;
        holeRect.sizeDelta = localMax - localMin;
      }
    }

    private static void SetPanel(RectTransform panel, Vector2 localMin, Vector2 localMax)
    {
      if (panel == null) return;
      panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
      panel.pivot = new Vector2(0.5f, 0.5f);
      panel.anchoredPosition = (localMin + localMax) * 0.5f;
      panel.sizeDelta = localMax - localMin;
    }

    private float pulseTime;

    private void PulseHighlight()
    {
      if (highlightRing == null) return;
      pulseTime += Time.unscaledDeltaTime;
      float alpha = 0.35f + Mathf.Sin(pulseTime * 4f) * 0.15f;
      var image = highlightRing.GetComponent<Image>();
      if (image != null)
        image.color = NexusTheme.WithAlpha(NexusTheme.Gold, alpha);
    }

    private void OnDestroy()
    {
      onDismiss?.Invoke();
    }
  }
}

using System.Collections;
using System.Collections.Generic;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.Dialogue;
using Assets.Resources.Scripts.Dialogue.Domain;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.UI.Nexus;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus.Tutorial
{
  /// <summary>Resolves active tutorial guides, registers UI anchors, and drives the spotlight overlay.</summary>
  public static class TutorialGuideService
  {
    private static readonly Dictionary<string, TutorialGuideAnchor> Anchors =
      new Dictionary<string, TutorialGuideAnchor>();

    private static AppScreen pendingScreen = AppScreen.Bridge;
    private static bool pendingPresent;
    private static string activeGuideId;

    public static void ClearAnchors() => Anchors.Clear();

    public static void RegisterAnchor(string key, RectTransform rect, Button button = null)
    {
      if (string.IsNullOrEmpty(key) || rect == null) return;
      Anchors[key] = new TutorialGuideAnchor { Rect = rect, Button = button };
    }

    public static void UnregisterAnchor(string key)
    {
      if (string.IsNullOrEmpty(key)) return;
      Anchors.Remove(key);
    }

    public static void PresentForScreen(AppScreen screen, AppShell shell)
    {
      if (shell == null) return;
      pendingScreen = screen;
      pendingPresent = true;
      shell.StartCoroutine(PresentAfterLayout(screen, shell));
    }

    public static void NotifyNavigated(AppScreen screen)
    {
      var guide = ResolveActiveGuide(pendingScreen);
      if (guide == null) return;
      if (guide.targetKind == TutorialGuideTargetKind.NavGroup
          && MatchesHideScreen(guide, screen))
        CompleteGuide(guide.guideId);
      else if (guide.targetKind == TutorialGuideTargetKind.NavScreen
               && guide.targetKey == screen.ToString())
        CompleteGuide(guide.guideId);
    }

    public static void Dismiss()
    {
      pendingPresent = false;
      activeGuideId = null;
      TutorialGuideOverlay.Close();
    }

    public static bool ShouldSuppressFullscreenDialogue(string chapterStepId) =>
      TutorialGuideCatalog.ShouldSuppressFullscreenDialogue(chapterStepId);

    private static IEnumerator PresentAfterLayout(AppScreen screen, AppShell shell)
    {
      yield return null;
      if (!pendingPresent) yield break;
      pendingPresent = false;
      PresentNow(screen, shell);
    }

    private static void PresentNow(AppScreen screen, AppShell shell)
    {
      if (DialogueService.IsPlaying || NpcDialogueOverlay.IsOpen)
      {
        Dismiss();
        return;
      }

      var guide = ResolveActiveGuide(screen);
      if (guide == null)
      {
        Dismiss();
        return;
      }

      if (!TryResolveTarget(guide, screen, shell, out var rect, out var button))
      {
        Dismiss();
        return;
      }

      if (activeGuideId == guide.guideId && TutorialGuideOverlay.IsOpen)
        return;

      activeGuideId = guide.guideId;
      DialogueScriptDef script = DialogueCatalog.Get(guide.dialogueId);
      TutorialGuideOverlay.Show(
        rect,
        button,
        script,
        () => CompleteGuide(guide.guideId),
        () => activeGuideId = null);
    }

    private static TutorialGuideDef ResolveActiveGuide(AppScreen screen)
    {
      var chapter = TryResolveForChain(TutorialGuideCatalog.ChainChapter, screen, GetChapterStepId());
      if (chapter != null) return chapter;

      if (!OnboardingService.IsChainComplete)
      {
        var onboarding = TryResolveForChain(
          TutorialGuideCatalog.ChainOnboarding, screen, OnboardingService.ActiveStep?.stepId);
        if (onboarding != null) return onboarding;
      }

      return null;
    }

    private static string GetChapterStepId()
    {
      ChapterQuestService.EnsureLoaded();
      if (ChapterQuestService.IsChapterComplete) return null;
      return ChapterQuestService.ActiveStep?.stepId;
    }

    private static TutorialGuideDef TryResolveForChain(string chain, AppScreen screen, string stepId)
    {
      if (string.IsNullOrEmpty(stepId)) return null;

      TutorialGuideDef best = null;
      foreach (var guide in TutorialGuideCatalog.All)
      {
        if (guide == null || guide.chain != chain || guide.stepId != stepId)
          continue;
        if (IsGuideComplete(chain, guide.guideId))
          continue;
        if (!MatchesScreen(guide, screen))
          continue;
        if (ShouldSkipFormationJoin(chain, stepId, guide))
          continue;
        if (best == null || guide.order < best.order)
          best = guide;
      }

      return best;
    }

    private static bool ShouldSkipFormationJoin(string chain, string stepId, TutorialGuideDef guide)
    {
      if (guide.targetKind != TutorialGuideTargetKind.UiAnchor
          || guide.targetKey != "formation_join")
        return false;

      bool formationReady = chain == TutorialGuideCatalog.ChainChapter
        ? ChapterQuestService.State?.flags?.formationReady == true
        : OnboardingService.State?.flags?.formationReady == true;

      if (formationReady)
      {
        CompleteGuide(guide.guideId);
        return true;
      }

      if (!Anchors.TryGetValue("formation_join", out var join) || join?.Button == null)
        return false;

      return !join.Button.interactable;
    }

    private static bool MatchesScreen(TutorialGuideDef guide, AppScreen screen)
    {
      if (!string.IsNullOrEmpty(guide.requiredScreen)
          && guide.requiredScreen != screen.ToString())
        return false;

      if (!string.IsNullOrEmpty(guide.hideOnScreen)
          && guide.hideOnScreen == screen.ToString())
        return false;

      return true;
    }

    private static bool MatchesHideScreen(TutorialGuideDef guide, AppScreen screen) =>
      !string.IsNullOrEmpty(guide.hideOnScreen) && guide.hideOnScreen == screen.ToString();

    private static bool TryResolveTarget(
      TutorialGuideDef guide,
      AppScreen screen,
      AppShell shell,
      out RectTransform rect,
      out Button button)
    {
      rect = null;
      button = null;
      if (shell == null) return false;

      switch (guide.targetKind)
      {
        case TutorialGuideTargetKind.NavGroup:
          if (!System.Enum.TryParse(guide.targetKey, out NavGroup group))
            return false;
          if (!string.IsNullOrEmpty(guide.hideOnScreen)
              && System.Enum.TryParse(guide.hideOnScreen, out AppScreen hideScreen)
              && NavGroupRules.GroupOf(screen) == group)
          {
            button = shell.TryGetSubNavButton(hideScreen);
          }

          if (button == null)
            button = shell.TryGetGroupNavButton(group);
          break;

        case TutorialGuideTargetKind.NavScreen:
          if (!System.Enum.TryParse(guide.targetKey, out AppScreen navScreen))
            return false;
          button = shell.TryGetSubNavButton(navScreen);
          if (button == null && NavGroupRules.GroupOf(screen) != NavGroupRules.GroupOf(navScreen))
          {
            button = shell.TryGetGroupNavButton(NavGroupRules.GroupOf(navScreen));
          }

          break;

        case TutorialGuideTargetKind.UiAnchor:
          if (!Anchors.TryGetValue(guide.targetKey, out var anchor) || anchor?.Rect == null)
            return false;
          rect = anchor.Rect;
          button = anchor.Button;
          return true;

        default:
          return false;
      }

      if (button == null) return false;
      rect = button.GetComponent<RectTransform>();
      return rect != null;
    }

    private static bool IsGuideComplete(string chain, string guideId)
    {
      if (string.IsNullOrEmpty(guideId)) return true;
      if (chain == TutorialGuideCatalog.ChainChapter)
      {
        ChapterQuestService.EnsureLoaded();
        var ids = ChapterQuestService.State?.completedGuideIds;
        return ids != null && ids.Contains(guideId);
      }

      OnboardingService.EnsureLoaded();
      var obIds = OnboardingService.State?.completedGuideIds;
      return obIds != null && obIds.Contains(guideId);
    }

    private static void CompleteGuide(string guideId)
    {
      if (string.IsNullOrEmpty(guideId)) return;

      foreach (var guide in TutorialGuideCatalog.All)
      {
        if (guide == null || guide.guideId != guideId) continue;

        if (guide.chain == TutorialGuideCatalog.ChainChapter)
        {
          ChapterQuestService.EnsureLoaded();
          ChapterQuestService.State.completedGuideIds ??= new List<string>();
          if (!ChapterQuestService.State.completedGuideIds.Contains(guideId))
            ChapterQuestService.State.completedGuideIds.Add(guideId);
          ChapterQuestService.Save();
        }
        else
        {
          OnboardingService.EnsureLoaded();
          OnboardingService.State.completedGuideIds ??= new List<string>();
          if (!OnboardingService.State.completedGuideIds.Contains(guideId))
            OnboardingService.State.completedGuideIds.Add(guideId);
          OnboardingService.Save();
        }

        break;
      }

      Dismiss();
      if (AppShell.Instance != null)
        PresentForScreen(pendingScreen, AppShell.Instance);
    }
  }
}

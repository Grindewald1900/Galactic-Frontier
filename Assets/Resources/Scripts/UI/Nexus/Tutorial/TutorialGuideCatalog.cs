using System.Collections.Generic;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.Onboarding.Domain;

namespace Assets.Resources.Scripts.UI.Nexus.Tutorial
{
  public static class TutorialGuideCatalog
  {
    public const string ChainOnboarding = "onboarding";
    public const string ChainChapter = "chapter";

    private static readonly List<TutorialGuideDef> Guides = new List<TutorialGuideDef>
    {
      // Onboarding — formation
      NavGuide("tg_ob_formation_nav", ChainOnboarding, OnboardingCatalog.StepFormation, "Formation",
        TutorialGuideTargetKind.NavGroup, "Fleet", "ob_guide_formation_nav", 0),
      AnchorGuide("tg_ob_formation_slot", ChainOnboarding, OnboardingCatalog.StepFormation, "Formation",
        "formation_empty_slot", "ob_guide_formation_slot", 1),
      AnchorGuide("tg_ob_formation_join", ChainOnboarding, OnboardingCatalog.StepFormation, "Formation",
        "formation_join", "ob_guide_formation_join", 2),

      // Onboarding — first battle
      NavGuide("tg_ob_battle_nav", ChainOnboarding, OnboardingCatalog.StepFirstBattle, "Battle",
        TutorialGuideTargetKind.NavGroup, "StarMap", "ob_guide_battle_nav", 0),
      AnchorGuide("tg_ob_battle_start", ChainOnboarding, OnboardingCatalog.StepFirstBattle, "Battle",
        "explore_start", "ob_guide_battle_start", 1),

      // Onboarding — gather
      NavGuide("tg_ob_gather_nav", ChainOnboarding, OnboardingCatalog.StepGather, "Battle",
        TutorialGuideTargetKind.NavGroup, "StarMap", "ob_guide_gather_nav", 0),
      AnchorGuide("tg_ob_gather_start", ChainOnboarding, OnboardingCatalog.StepGather, "Battle",
        "explore_gather", "ob_guide_gather_start", 1),

      // Onboarding — craft
      NavGuide("tg_ob_craft_nav", ChainOnboarding, OnboardingCatalog.StepCraft, "Inventory",
        TutorialGuideTargetKind.NavGroup, "Industry", "ob_guide_craft_nav", 0),
      SubNavGuide("tg_ob_craft_sub", ChainOnboarding, OnboardingCatalog.StepCraft,
        "Inventory", "Crafting", "ob_guide_craft_nav", 1),
      AnchorGuide("tg_ob_craft_start", ChainOnboarding, OnboardingCatalog.StepCraft, "Crafting",
        "crafting_start", "ob_guide_craft_start", 2),

      // Onboarding — starport shop
      NavGuide("tg_ob_shop_nav", ChainOnboarding, OnboardingCatalog.StepNpcShop, "Market",
        TutorialGuideTargetKind.NavGroup, "Starport", "ob_guide_shop_nav", 0),
      AnchorGuide("tg_ob_shop_buy", ChainOnboarding, OnboardingCatalog.StepNpcShop, "Market",
        "market_buy", "ob_guide_shop_buy", 1),

      // Chapter 1 — recruit / formation / explore
      NavGuide("tg_ch1_recruit_nav", ChainChapter, ChapterQuestCatalog.StepRecruitCole, "Market",
        TutorialGuideTargetKind.NavGroup, "Starport", "ch1_mita_recruit", 0),
      SubNavGuide("tg_ch1_recruit_sub", ChainChapter, ChapterQuestCatalog.StepRecruitCole,
        "Market", "Recruit", "ch1_mita_recruit", 1),
      AnchorGuide("tg_ch1_recruit_pull", ChainChapter, ChapterQuestCatalog.StepRecruitCole, "Recruit",
        "recruit_pull_one", "ch1_mita_recruit", 2),

      AnchorGuide("tg_ch1_formation_slot", ChainChapter, ChapterQuestCatalog.StepFormation, "Formation",
        "formation_empty_slot", "ch1_asra_formation", 0),
      AnchorGuide("tg_ch1_formation_join", ChainChapter, ChapterQuestCatalog.StepFormation, "Formation",
        "formation_join", "ch1_asra_formation", 1),

      NavGuide("tg_ch1_battle_nav", ChainChapter, ChapterQuestCatalog.StepOuterCleanup, "Battle",
        TutorialGuideTargetKind.NavGroup, "StarMap", "ob_guide_battle_nav", 0),
      AnchorGuide("tg_ch1_battle_start", ChainChapter, ChapterQuestCatalog.StepOuterCleanup, "Battle",
        "explore_start", "ob_guide_battle_start", 1),

      NavGuide("tg_ch1_scan_nav", ChainChapter, ChapterQuestCatalog.StepScanSignal, "Battle",
        TutorialGuideTargetKind.NavGroup, "StarMap", "ch1_sernia_signal", 0),
      AnchorGuide("tg_ch1_scan_probe", ChainChapter, ChapterQuestCatalog.StepScanSignal, "Battle",
        "explore_probe", "ch1_sernia_signal", 1),

      NavGuide("tg_ch1_mining_nav", ChainChapter, ChapterQuestCatalog.StepMiningSpur, "Battle",
        TutorialGuideTargetKind.NavGroup, "StarMap", "ch1_magki_armor", 0),
      AnchorGuide("tg_ch1_mining_start", ChainChapter, ChapterQuestCatalog.StepMiningSpur, "Battle",
        "explore_start", "ch1_magki_armor", 1)
    };

    public static IReadOnlyList<TutorialGuideDef> All => Guides;

    public static bool HasGuideForStep(string chain, string stepId)
    {
      if (string.IsNullOrEmpty(stepId)) return false;
      foreach (var g in Guides)
      {
        if (g != null && g.chain == chain && g.stepId == stepId)
          return true;
      }

      return false;
    }

    public static bool ShouldSuppressFullscreenDialogue(string chapterStepId)
    {
      if (string.IsNullOrEmpty(chapterStepId)) return false;
      foreach (var g in Guides)
      {
        if (g == null || g.chain != ChainChapter || g.stepId != chapterStepId)
          continue;
        if (!string.IsNullOrEmpty(g.dialogueId))
          return true;
      }

      return false;
    }

    private static TutorialGuideDef NavGuide(
      string guideId, string chain, string stepId, string hideOnScreen,
      TutorialGuideTargetKind kind, string targetKey, string dialogueId, int order) =>
      new TutorialGuideDef
      {
        guideId = guideId,
        chain = chain,
        stepId = stepId,
        hideOnScreen = hideOnScreen,
        targetKind = kind,
        targetKey = targetKey,
        dialogueId = dialogueId,
        order = order
      };

    private static TutorialGuideDef SubNavGuide(
      string guideId, string chain, string stepId, string requiredScreen,
      string navScreen, string dialogueId, int order) =>
      new TutorialGuideDef
      {
        guideId = guideId,
        chain = chain,
        stepId = stepId,
        requiredScreen = requiredScreen,
        hideOnScreen = navScreen,
        targetKind = TutorialGuideTargetKind.NavScreen,
        targetKey = navScreen,
        dialogueId = dialogueId,
        order = order
      };

    private static TutorialGuideDef AnchorGuide(
      string guideId, string chain, string stepId, string requiredScreen,
      string anchorKey, string dialogueId, int order) =>
      new TutorialGuideDef
      {
        guideId = guideId,
        chain = chain,
        stepId = stepId,
        requiredScreen = requiredScreen,
        targetKind = TutorialGuideTargetKind.UiAnchor,
        targetKey = anchorKey,
        dialogueId = dialogueId,
        order = order
      };
  }
}

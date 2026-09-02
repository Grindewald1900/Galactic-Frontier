using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Right-edge quest tracker drawer (P6). Replaces missions as primary entry.</summary>
    internal sealed class QuestTrackerDrawer
    {
        private Transform host;
        private RectTransform panel;
        private Transform content;
        private bool open;
        private System.Action<AppScreen> navigate;

        public bool IsOpen => open;

        public void Attach(Transform chromeRoot, System.Action<AppScreen> navigate)
        {
            this.navigate = navigate;
            host = chromeRoot;
            BuildShell();
            SetOpen(false);
        }

        public void Toggle() => SetOpen(!open);

        public void SetOpen(bool value)
        {
            open = value;
            if (panel != null)
                panel.gameObject.SetActive(open);
            if (open)
                Rebuild();
        }

        public void Rebuild()
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(content.GetChild(i).gameObject);

            if (DataUtil.Instance != null)
            {
                DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance?.cardEntities);
                WorldService.EnsureLoaded(DataUtil.Instance);
                OnboardingService.EnsureLoaded(DataUtil.Instance);
                ChapterQuestService.EnsureLoaded(DataUtil.Instance);
            }

            float y = 8f;
            NexusUiFactory.CreateText(
                content, "Title", UiText.QuestTrackerTitle,
                new Vector2(12f, y), new Vector2(320f, 24f), 14f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            y += 32f;

            if (!ChapterQuestService.IsChapterComplete)
            {
                NexusUiFactory.CreateText(
                    content, "ChapterHead", UiText.QuestChapterTitle,
                    new Vector2(12f, y), new Vector2(320f, 20f), 12f, NexusTheme.Cyan,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                y += 24f;
                foreach (var view in ChapterQuestService.GetStepViews())
                {
                    if (view?.Def == null) continue;
                    DrawChapterStep(view, ref y);
                }

                y += 8f;
                NexusUiFactory.CreateText(
                    content, "OnboardingHead", UiText.QuestOnboardingTitle,
                    new Vector2(12f, y), new Vector2(320f, 20f), 12f, NexusTheme.MutedText,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                y += 24f;
            }

            if (OnboardingService.IsChainComplete && ChapterQuestService.IsChapterComplete)
            {
                NexusUiFactory.CreateText(
                    content, "NextGoal", UiText.QuestNextGoalTitle,
                    new Vector2(12f, y), new Vector2(320f, 20f), 12f, NexusTheme.Cyan,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                y += 24f;
                var hint = NexusUiFactory.CreateText(
                    content, "NextSteps", UiText.QuestNextGoalSteps,
                    new Vector2(12f, y), new Vector2(320f, 80f), 11f, NexusTheme.MutedText);
                hint.textWrappingMode = TextWrappingModes.Normal;
                y += 88f;
            }

            foreach (var view in OnboardingService.GetStepViews())
            {
                if (view?.Def == null) continue;
                DrawStep(view, ref y);
            }

            panel.sizeDelta = new Vector2(360f, Mathf.Min(720f, y + 16f));
        }

        private void DrawChapterStep(ChapterQuestStepView view, ref float y)
        {
            bool active = view.Status == ChapterQuestStepStatus.Active;
            bool done = view.Status == ChapterQuestStepStatus.Completed;
            bool claimable = view.CanClaim;

            Color row = active
                ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                : done
                    ? NexusTheme.WithAlpha(NexusTheme.SurfaceRaised, 0.5f)
                    : NexusTheme.SurfaceRaised;

            NexusUiFactory.CreateBox(
                content, "ChStep_" + view.Def.stepId,
                new Vector2(8f, y), new Vector2(344f, claimable ? 88f : 64f),
                row, NexusTheme.BorderSoft);

            string title = UiText.T(view.Def.titleEn, view.Def.titleZh);
            NexusUiFactory.CreateText(
                content, "ChT_" + view.Def.stepId, title,
                new Vector2(16f, y + 8f), new Vector2(320f, 20f), 11f,
                active ? NexusTheme.Text : NexusTheme.MutedText,
                TextAlignmentOptions.Left, FontStyles.Bold);

            string status = done
                ? UiText.QuestStepDone
                : active
                    ? UiText.QuestStepActive
                    : UiText.QuestStepLocked;
            NexusUiFactory.CreateText(
                content, "ChS_" + view.Def.stepId, status,
                new Vector2(16f, y + 30f), new Vector2(320f, 18f), 10f, NexusTheme.DimText);

            if (claimable)
            {
                NexusUiFactory.CreateButton(
                    content, "ChClaim_" + view.Def.stepId, UiText.QuestClaim,
                    new Vector2(16f, y + 50f), new Vector2(120f, 28f),
                    () =>
                    {
                        ChapterQuestService.TryClaim(view.Def.stepId);
                        Rebuild();
                    },
                    NexusTheme.WithAlpha(NexusTheme.Green, 0.2f), NexusTheme.Green, 11f);
            }
            else if (active && !string.IsNullOrEmpty(view.Def.targetScreen))
            {
                var target = MapTarget(view.Def.targetScreen);
                NexusUiFactory.CreateButton(
                    content, "ChGo_" + view.Def.stepId, UiText.QuestGo,
                    new Vector2(16f, y + 50f), new Vector2(120f, 28f),
                    () =>
                    {
                        SetOpen(false);
                        navigate?.Invoke(target);
                    },
                    NexusTheme.WithAlpha(NexusTheme.Cyan, 0.14f), NexusTheme.Cyan, 11f);
            }

            y += claimable ? 96f : 72f;
        }

        private void DrawStep(OnboardingStepView view, ref float y)
        {
            bool active = view.Status == OnboardingStepStatus.Active;
            bool done = view.Status == OnboardingStepStatus.Completed;
            bool claimable = view.CanClaim;

            Color row = active
                ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.12f)
                : done
                    ? NexusTheme.WithAlpha(NexusTheme.SurfaceRaised, 0.5f)
                    : NexusTheme.SurfaceRaised;

            NexusUiFactory.CreateBox(
                content, "Step_" + view.Def.stepId,
                new Vector2(8f, y), new Vector2(344f, claimable ? 88f : 64f),
                row, NexusTheme.BorderSoft);

            string title = UiText.T(view.Def.titleEn, view.Def.titleZh);
            NexusUiFactory.CreateText(
                content, "T_" + view.Def.stepId, title,
                new Vector2(16f, y + 8f), new Vector2(320f, 20f), 11f,
                active ? NexusTheme.Text : NexusTheme.MutedText,
                TextAlignmentOptions.Left, FontStyles.Bold);

            string status = done
                ? UiText.QuestStepDone
                : active
                    ? UiText.QuestStepActive
                    : UiText.QuestStepLocked;
            NexusUiFactory.CreateText(
                content, "S_" + view.Def.stepId, status,
                new Vector2(16f, y + 30f), new Vector2(320f, 18f), 10f, NexusTheme.DimText);

            if (claimable)
            {
                NexusUiFactory.CreateButton(
                    content, "Claim_" + view.Def.stepId, UiText.QuestClaim,
                    new Vector2(16f, y + 50f), new Vector2(120f, 28f),
                    () =>
                    {
                        OnboardingService.TryClaim(view.Def.stepId);
                        Rebuild();
                    },
                    NexusTheme.WithAlpha(NexusTheme.Green, 0.2f), NexusTheme.Green, 11f);
            }
            else if (active && !string.IsNullOrEmpty(view.Def.targetScreen))
            {
                var target = MapTarget(view.Def.targetScreen);
                NexusUiFactory.CreateButton(
                    content, "Go_" + view.Def.stepId, UiText.QuestGo,
                    new Vector2(16f, y + 50f), new Vector2(120f, 28f),
                    () =>
                    {
                        SetOpen(false);
                        navigate?.Invoke(target);
                    },
                    NexusTheme.WithAlpha(NexusTheme.Cyan, 0.14f), NexusTheme.Cyan, 11f);
            }

            y += claimable ? 96f : 72f;
        }

        private static AppScreen MapTarget(string targetScreen) => targetScreen switch
        {
            "Formation" => AppScreen.Formation,
            "Battle" => AppScreen.Battle,
            "Recruit" => AppScreen.Recruit,
            "Crafting" => AppScreen.Crafting,
            "Market" => AppScreen.Market,
            "Ship" => AppScreen.Ship,
            "Inventory" => AppScreen.Inventory,
            _ => AppScreen.Bridge
        };

        private void BuildShell()
        {
            var canvas = host.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var root = new GameObject("Quest Tracker", typeof(RectTransform));
            root.transform.SetParent(host, false);
            panel = root.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(1f, 0.5f);
            panel.anchorMax = new Vector2(1f, 0.5f);
            panel.pivot = new Vector2(1f, 0.5f);
            panel.anchoredPosition = new Vector2(-8f, 0f);
            panel.sizeDelta = new Vector2(360f, 520f);

            NexusUiFactory.CreateBox(
                root.transform, "Bg", Vector2.zero, panel.sizeDelta,
                NexusTheme.SurfaceRaised, NexusTheme.Border);

            var close = NexusUiFactory.CreateButton(
                root.transform, "Close", "×",
                new Vector2(320f, 8f), new Vector2(32f, 28f),
                () => SetOpen(false),
                NexusTheme.Surface, NexusTheme.MutedText, 18f);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(root.transform, false);
            content = contentGo.transform;
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.offsetMin = new Vector2(0f, 0f);
            contentRect.offsetMax = new Vector2(0f, -44f);
        }
    }
}

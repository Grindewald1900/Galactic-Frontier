using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Native Missions screen for onboarding_v1 chain (systems/10 / P5.4a).</summary>
    internal sealed class MissionsScreen
    {
        private readonly Transform root;
        private readonly System.Action<AppScreen> navigate;
        private string statusMessage = "";

        private MissionsScreen(Transform root, System.Action<AppScreen> navigate)
        {
            this.root = root;
            this.navigate = navigate;
        }

        public GameObject Root => root.gameObject;

        public static MissionsScreen Build(Transform parent, System.Action<AppScreen> navigate)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Missions Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new MissionsScreen(panel.transform, navigate);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            if (DataUtil.Instance != null)
            {
                DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance?.cardEntities);
                WorldService.EnsureLoaded(DataUtil.Instance);
                OnboardingService.EnsureLoaded(DataUtil.Instance);
                ChapterQuestService.EnsureLoaded(DataUtil.Instance);
            }

            NexusUiFactory.CreateText(
                root, "Title", UiText.MissionsTitle,
                new Vector2(28f, 16f), new Vector2(640f, 32f), 22f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                root, "Hint", UiText.MissionsOnboardingHint,
                new Vector2(28f, 48f), new Vector2(1200f, 24f), 12f, NexusTheme.MutedText);

            float y = 90f;
            if (!ChapterQuestService.IsChapterComplete)
            {
                NexusUiFactory.CreateText(
                    root, "ChapterHead", UiText.QuestChapterTitle,
                    new Vector2(28f, y), new Vector2(900f, 28f), 16f, NexusTheme.Cyan,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                y += 36f;
                foreach (var view in ChapterQuestService.GetStepViews())
                {
                    if (view?.Def == null) continue;
                    DrawChapterRow(view, ref y);
                }

                y += 12f;
                NexusUiFactory.CreateText(
                    root, "OnboardingHead", UiText.QuestOnboardingTitle,
                    new Vector2(28f, y), new Vector2(900f, 24f), 14f, NexusTheme.MutedText,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                y += 32f;
            }
            else if (OnboardingService.IsChainComplete)
            {
                NexusUiFactory.CreateText(
                    root, "Done", UiText.MissionsChainComplete,
                    new Vector2(28f, y), new Vector2(900f, 36f), 16f, NexusTheme.Cyan,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                NexusUiFactory.CreateButton(
                    root, "GoExplore", UiText.EnterExploreBattle,
                    new Vector2(28f, y + 50f), new Vector2(240f, 44f),
                    () => navigate?.Invoke(AppScreen.Battle),
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 14f);
                NexusUiFactory.CreateButton(
                    root, "GoShip", UiText.OpenShipBay,
                    new Vector2(288f, y + 50f), new Vector2(240f, 44f),
                    () => navigate?.Invoke(AppScreen.Ship),
                    NexusTheme.SurfaceRaised, NexusTheme.Text, 14f);
                y += 120f;
            }

            foreach (var view in OnboardingService.GetStepViews())
            {
                if (view?.Def == null) continue;
                DrawStepRow(view, y);
                y += 86f;
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                NexusUiFactory.CreateText(
                    root, "Status", statusMessage,
                    new Vector2(28f, y + 8f), new Vector2(1100f, 28f), 12f, NexusTheme.Cyan);
            }
        }

        private void DrawChapterRow(ChapterQuestStepView view, ref float y)
        {
            var def = view.Def;
            var title = UiText.T(def.titleEn, def.titleZh);
            var statusLabel = view.Status switch
            {
                ChapterQuestStepStatus.Completed => UiText.MissionsStatusDone,
                ChapterQuestStepStatus.Active => UiText.MissionsStatusActive,
                _ => UiText.MissionsStatusLocked
            };
            var bg = view.Status == ChapterQuestStepStatus.Active
                ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                : NexusTheme.SurfaceRaised;

            NexusUiFactory.CreateBox(
                root, "ChStep " + def.stepId,
                new Vector2(28f, y), new Vector2(1480f, 78f),
                bg, NexusTheme.BorderSoft);

            NexusUiFactory.CreateText(
                root, "ChTitle " + def.stepId,
                $"{def.order}. {title}  ·  {statusLabel}",
                new Vector2(44f, y + 8f), new Vector2(900f, 24f), 15f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                root, "ChHint " + def.stepId,
                UiText.T(def.hintEn, def.hintZh),
                new Vector2(44f, y + 36f), new Vector2(980f, 28f), 12f, NexusTheme.MutedText);

            if (view.Status == ChapterQuestStepStatus.Active || view.Status == ChapterQuestStepStatus.Completed)
            {
                var target = MapTarget(def.targetScreen);
                NexusUiFactory.CreateButton(
                    root, "ChGo " + def.stepId, UiText.MissionsGo,
                    new Vector2(1080f, y + 20f), new Vector2(140f, 40f),
                    () => navigate?.Invoke(target),
                    NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);
            }

            if (view.CanClaim)
            {
                NexusUiFactory.CreateButton(
                    root, "ChClaim " + def.stepId, UiText.MissionsClaim,
                    new Vector2(1240f, y + 20f), new Vector2(160f, 40f),
                    () =>
                    {
                        var r = ChapterQuestService.TryClaim(def.stepId);
                        statusMessage = r.Success
                            ? UiText.MissionsClaimed(UiText.T(def.titleEn, def.titleZh))
                            : r.Message;
                        Rebuild();
                    },
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f), NexusTheme.Gold, 12f);
            }

            y += 86f;
        }

        private void DrawStepRow(OnboardingStepView view, float y)
        {
            var def = view.Def;
            var title = UiText.T(def.titleEn, def.titleZh);
            var statusLabel = view.Status switch
            {
                OnboardingStepStatus.Completed => UiText.MissionsStatusDone,
                OnboardingStepStatus.Active => UiText.MissionsStatusActive,
                _ => UiText.MissionsStatusLocked
            };
            var bg = view.Status == OnboardingStepStatus.Active
                ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.14f)
                : NexusTheme.SurfaceRaised;

            NexusUiFactory.CreateBox(
                root, "Step " + def.stepId,
                new Vector2(28f, y), new Vector2(1480f, 78f),
                bg, NexusTheme.BorderSoft);

            NexusUiFactory.CreateText(
                root, "StepTitle " + def.stepId,
                $"{def.order}. {title}  ·  {statusLabel}",
                new Vector2(44f, y + 8f), new Vector2(900f, 24f), 15f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                root, "StepHint " + def.stepId,
                UiText.T(def.hintEn, def.hintZh),
                new Vector2(44f, y + 36f), new Vector2(980f, 28f), 12f, NexusTheme.MutedText);

            if (view.Status == OnboardingStepStatus.Active || view.Status == OnboardingStepStatus.Completed)
            {
                var target = MapTarget(def.targetScreen);
                NexusUiFactory.CreateButton(
                    root, "Go " + def.stepId, UiText.MissionsGo,
                    new Vector2(1080f, y + 20f), new Vector2(140f, 40f),
                    () => navigate?.Invoke(target),
                    NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);
            }

            if (view.CanClaim)
            {
                NexusUiFactory.CreateButton(
                    root, "Claim " + def.stepId, UiText.MissionsClaim,
                    new Vector2(1240f, y + 20f), new Vector2(160f, 40f),
                    () =>
                    {
                        var r = OnboardingService.TryClaim(def.stepId);
                        statusMessage = r.Success
                            ? UiText.MissionsClaimed(UiText.T(def.titleEn, def.titleZh))
                            : r.Message;
                        Debug.Log("[ONBOARD] claim " + def.stepId + ": " + (r.Success ? "ok" : r.Message));
                        Rebuild();
                    },
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f), NexusTheme.Gold, 12f);
            }
        }

        private static AppScreen MapTarget(string targetScreen) =>
            targetScreen switch
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
    }
}

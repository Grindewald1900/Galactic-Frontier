using System.Collections.Generic;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Gacha;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.UI.Nexus.Tutorial;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Nexus recruit / gacha page. Independent from Market (NPC shop).</summary>
    internal sealed class RecruitScreen
    {
        private readonly Transform root;
        private readonly System.Action<AppScreen> navigate;
        private string statusMessage = "";
        private List<CardEntity> lastPull = new();

        private RecruitScreen(Transform root, System.Action<AppScreen> navigate)
        {
            this.root = root;
            this.navigate = navigate;
        }

        public GameObject Root => root.gameObject;

        public static RecruitScreen Build(Transform parent, System.Action<AppScreen> navigate)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Recruit Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new RecruitScreen(panel.transform, navigate);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            GachaService.EnsureLoaded();
            var tickets = GachaService.TicketCount();
            var pity = GachaService.State?.pityCounter ?? 0;
            var canOne = GachaService.CanAfford(1, out _);
            var canTen = GachaService.CanAfford(10, out _);

            NexusUiFactory.CreateText(
                root, "Title", UiText.RecruitTitle,
                new Vector2(28f, 20f), new Vector2(900f, 36f), 22f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                root, "Subtitle",
                UiText.RecruitSubtitle(tickets, pity, GachaRules.PityThreshold),
                new Vector2(28f, 60f), new Vector2(1200f, 28f), 13f, NexusTheme.MutedText);

            NexusUiFactory.CreateText(
                root, "Hint", UiText.RecruitHint,
                new Vector2(28f, 92f), new Vector2(1200f, 48f), 12f, NexusTheme.DimText);

            ChapterQuestService.EnsureLoaded(DataUtil.Instance);
            var chapterActive = ChapterQuestService.ActiveStep;
            if (chapterActive != null && chapterActive.stepId == ChapterQuestCatalog.StepRecruitCole)
            {
                NexusUiFactory.CreateText(
                    root, "ChapterHint", UiText.ChapterRecruitHint,
                    new Vector2(28f, 148f), new Vector2(1200f, 36f), 12f, NexusTheme.Cyan);
                NexusUiFactory.CreateButton(
                    root, "GoFormation", UiText.QuestGo + " · " + UiText.Breadcrumb(AppScreen.Formation),
                    new Vector2(640f, 200f), new Vector2(260f, 44f),
                    () => navigate?.Invoke(AppScreen.Formation),
                    NexusTheme.WithAlpha(NexusTheme.Cyan, 0.14f), NexusTheme.Cyan, 13f);
            }

            float poolY = chapterActive?.stepId == ChapterQuestCatalog.StepRecruitCole ? 260f : 120f;
            NexusUiFactory.CreateText(
                root, "PoolTitle", UiText.RecruitPoolTitle,
                new Vector2(28f, poolY), new Vector2(600f, 28f), 18f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                root, "Pity", UiText.RecruitPity(pity, GachaRules.PityThreshold),
                new Vector2(28f, poolY + 32f), new Vector2(600f, 22f), 12f, NexusTheme.Cyan);
            NexusUiFactory.CreateText(
                root, "TicketSrc", UiText.RecruitTicketSources,
                new Vector2(640f, poolY), new Vector2(520f, 48f), 11f, NexusTheme.MutedText);

            float pullY = poolY + 80f;
            var pullOneFill = NexusTheme.WithAlpha(NexusTheme.Gold, canOne ? 0.28f : 0.16f);
            var pullOne = NexusUiFactory.CreateButton(
                root, "PullOne", UiText.RecruitPullOne,
                new Vector2(28f, pullY), new Vector2(220f, 48f),
                () => Pull(1),
                pullOneFill,
                canOne ? NexusTheme.Gold : NexusTheme.DimText,
                14f);
            KeepDisabledColor(pullOne, pullOneFill);
            pullOne.interactable = canOne;
            TutorialGuideService.RegisterAnchor(
                "recruit_pull_one",
                pullOne.GetComponent<RectTransform>(),
                pullOne);

            var pullTenFill = NexusTheme.WithAlpha(NexusTheme.Cyan, canTen ? 0.24f : 0.16f);
            var pullTen = NexusUiFactory.CreateButton(
                root, "PullTen", UiText.RecruitPullTen,
                new Vector2(268f, pullY), new Vector2(220f, 48f),
                () => Pull(10),
                pullTenFill,
                canTen ? NexusTheme.Cyan : NexusTheme.DimText,
                14f);
            KeepDisabledColor(pullTen, pullTenFill);
            pullTen.interactable = canTen;

            NexusUiFactory.CreateButton(
                root, "BuyTickets", UiText.RecruitBuyTickets,
                new Vector2(508f, pullY), new Vector2(240f, 48f),
                () => navigate?.Invoke(AppScreen.Market),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f), NexusTheme.Gold, 13f);

            float resultY = pullY + 68f;
            if (!canOne && (lastPull == null || lastPull.Count == 0))
            {
                NexusUiFactory.CreateText(
                    root, "Need", UiText.RecruitNeedTickets,
                    new Vector2(28f, resultY), new Vector2(900f, 24f), 12f, NexusTheme.Gold);
                resultY += 32f;
            }

            DrawPullResult(resultY);
        }

        private static void KeepDisabledColor(Button button, Color fill)
        {
            if (button == null)
                return;
            var colors = button.colors;
            colors.disabledColor = fill;
            button.colors = colors;
        }

        private void DrawPullResult(float y)
        {
            if (lastPull != null && lastPull.Count > 0)
            {
                var header = NexusUiFactory.CreateText(
                    root, "Status", statusMessage,
                    new Vector2(28f, y), new Vector2(1100f, 24f), 14f, NexusTheme.Text,
                    TextAlignmentOptions.Left, FontStyles.Bold);
                header.textWrappingMode = TextWrappingModes.NoWrap;

                float x = 28f;
                int shown = 0;
                foreach (var card in lastPull)
                {
                    if (card == null || shown >= 5)
                        continue;
                    var view = NexusCardVisual.CreatePortraitCard(
                        root,
                        "PullCard" + shown,
                        card,
                        new Vector2(x, y + 32f),
                        new Vector2(156f, 214f),
                        "Lv." + card.Level);
                    var motion = view.AddComponent<NexusUiMotion>();
                    motion.Press = false;
                    motion.Appear = true;
                    x += 168f;
                    shown++;
                }

                return;
            }

            if (string.IsNullOrEmpty(statusMessage))
                return;

            var status = NexusUiFactory.CreateText(
                root, "Status", statusMessage,
                new Vector2(28f, y), new Vector2(1100f, 72f), 14f, NexusTheme.Text);
            status.textWrappingMode = TextWrappingModes.Normal;
        }

        private void Pull(int count)
        {
            var result = GachaService.TryPull(count, grantImmediately: true);
            if (!result.Success)
            {
                statusMessage = result.Message;
                lastPull = new List<CardEntity>();
                Rebuild();
                return;
            }

            statusMessage = UiText.RecruitResultHeader(result.TicketsSpent, result.PityAfter);
            lastPull = result.Cards != null ? new List<CardEntity>(result.Cards) : new List<CardEntity>();
            Rebuild();
        }
    }
}

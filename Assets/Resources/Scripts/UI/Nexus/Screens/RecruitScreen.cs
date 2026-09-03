using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Gacha;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.UI.Nexus.Tutorial;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Nexus recruit / gacha page. Independent from Market (NPC shop).</summary>
    internal sealed class RecruitScreen
    {
        private readonly Transform root;
        private readonly System.Action<AppScreen> navigate;
        private string statusMessage = "";

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
            var pullOne = NexusUiFactory.CreateButton(
                root, "PullOne", UiText.RecruitPullOne,
                new Vector2(28f, pullY), new Vector2(220f, 48f),
                () => Pull(1),
                canOne
                    ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                    : NexusTheme.SurfaceRaised,
                canOne ? NexusTheme.Gold : NexusTheme.DimText,
                14f);
            TutorialGuideService.RegisterAnchor(
                "recruit_pull_one",
                pullOne.GetComponent<RectTransform>(),
                pullOne);

            NexusUiFactory.CreateButton(
                root, "PullTen", UiText.RecruitPullTen,
                new Vector2(268f, pullY), new Vector2(220f, 48f),
                () => Pull(10),
                canTen
                    ? NexusTheme.WithAlpha(NexusTheme.Cyan, 0.18f)
                    : NexusTheme.SurfaceRaised,
                canTen ? NexusTheme.Cyan : NexusTheme.DimText,
                14f);

            NexusUiFactory.CreateButton(
                root, "BuyTickets", UiText.RecruitBuyTickets,
                new Vector2(508f, pullY), new Vector2(240f, 48f),
                () => navigate?.Invoke(AppScreen.Market),
                NexusTheme.SurfaceRaised, NexusTheme.Text, 13f);

            if (!canOne)
            {
                NexusUiFactory.CreateText(
                    root, "Need", UiText.RecruitNeedTickets,
                    new Vector2(28f, 220f), new Vector2(900f, 24f), 12f, NexusTheme.Gold);
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                var status = NexusUiFactory.CreateText(
                    root, "Status", statusMessage,
                    new Vector2(28f, 250f), new Vector2(1200f, 420f), 13f, NexusTheme.Text);
                status.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private void Pull(int count)
        {
            var result = GachaService.TryPull(count, grantImmediately: true);
            if (!result.Success)
            {
                statusMessage = result.Message;
                Rebuild();
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(UiText.RecruitResultHeader(result.TicketsSpent, result.PityAfter));
            if (result.Cards != null)
            {
                foreach (var card in result.Cards)
                {
                    if (card == null) continue;
                    sb.AppendLine($"· {card.cardName}  {card.CharacterTier}  Lv.{card.Level}");
                }
            }

            statusMessage = sb.ToString();
            Rebuild();
        }
    }
}

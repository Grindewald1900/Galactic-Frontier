using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Quest;
using Assets.Resources.Scripts.Quest.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Missions board for the unified MVP quest catalog (doc 25).</summary>
    internal sealed class MissionsScreen
    {
        private static readonly QuestType[] Tabs =
        {
            QuestType.Story,
            QuestType.Onboarding,
            QuestType.Expedition,
            QuestType.Ship,
            QuestType.Loop
        };

        private readonly Transform root;
        private readonly System.Action<AppScreen> navigate;
        private QuestType selectedTab = QuestType.Story;
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
                QuestService.EnsureLoaded(DataUtil.Instance);
            }

            NexusUiFactory.CreateText(
                root, "Title", UiText.MissionsTitle,
                new Vector2(28f, 16f), new Vector2(640f, 32f), 22f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                root, "Hint", UiText.MissionsOnboardingHint,
                new Vector2(28f, 48f), new Vector2(1200f, 24f), 12f, NexusTheme.MutedText);

            float tabX = 28f;
            foreach (var type in Tabs)
            {
                var captured = type;
                bool selected = captured == selectedTab;
                var tab = NexusUiFactory.CreateButton(
                    root, "Tab_" + captured, TabLabel(captured),
                    new Vector2(tabX, 84f), new Vector2(168f, 36f),
                    () =>
                    {
                        selectedTab = captured;
                        statusMessage = "";
                        Rebuild();
                    },
                    selected
                        ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.28f)
                        : NexusTheme.SurfaceRaised,
                    selected ? NexusTheme.Gold : NexusTheme.MutedText,
                    13f);
                tabX += 176f;
                if (selected)
                    tab.GetComponent<Image>().color = NexusTheme.WithAlpha(NexusTheme.Gold, 0.28f);
            }

            var visible = VisibleInTab(selectedTab);
            int claimable = 0;
            foreach (var view in visible)
            {
                if (view != null && view.CanClaim)
                    claimable++;
            }

            var claimAll = NexusUiFactory.CreateButton(
                root, "ClaimTab", UiText.MissionsClaimTab,
                new Vector2(1180f, 84f), new Vector2(280f, 36f),
                ClaimCurrentTab,
                claimable > 0
                    ? NexusTheme.WithAlpha(NexusTheme.Green, 0.22f)
                    : NexusTheme.WithAlpha(NexusTheme.Surface, 0.65f),
                claimable > 0 ? NexusTheme.Green : NexusTheme.DimText,
                13f);
            claimAll.interactable = claimable > 0;

            NexusUiFactory.CreateText(
                root, "ClaimHint",
                claimable > 0
                    ? UiText.MissionsClaimTabCount(claimable)
                    : UiText.MissionsClaimTabEmpty,
                new Vector2(1180f, 122f), new Vector2(320f, 20f), 11f,
                claimable > 0 ? NexusTheme.Green : NexusTheme.DimText,
                TextAlignmentOptions.Right);

            float y = 156f;
            if (visible.Count == 0)
            {
                NexusUiFactory.CreateText(
                    root, "EmptyTab", UiText.MissionsTabEmpty(TabLabel(selectedTab)),
                    new Vector2(28f, y), new Vector2(900f, 48f), 14f, NexusTheme.MutedText);
                y += 56f;
            }
            else
            {
                foreach (var view in visible)
                    DrawRow(view, ref y);
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                NexusUiFactory.CreateText(
                    root, "Status", statusMessage,
                    new Vector2(28f, y + 8f), new Vector2(1400f, 28f), 12f, NexusTheme.Cyan);
            }
        }

        private void ClaimCurrentTab()
        {
            var ids = new List<string>();
            foreach (var view in VisibleInTab(selectedTab))
            {
                if (view?.Def != null && view.CanClaim)
                    ids.Add(view.Def.questId);
            }

            if (ids.Count == 0)
            {
                statusMessage = UiText.MissionsClaimTabEmpty;
                Rebuild();
                return;
            }

            int before = CurrencyService.Credits;
            int claimed = 0;
            foreach (var id in ids)
            {
                var result = QuestService.TryClaim(id);
                if (result.Success)
                    claimed++;
            }

            int delta = CurrencyService.Credits - before;
            statusMessage = claimed > 0
                ? UiText.MissionsClaimTabResult(claimed, delta)
                : UiText.MissionsClaimTabEmpty;
            Rebuild();
        }

        private static List<QuestView> VisibleInTab(QuestType type)
        {
            var list = new List<QuestView>();
            foreach (var view in QuestService.GetViews(type))
            {
                if (view?.Def == null) continue;
                if (view.Status == QuestStatus.Locked) continue;
                list.Add(view);
            }

            return list;
        }

        private static string TabLabel(QuestType type) => type switch
        {
            QuestType.Story => UiText.QuestChapterTitle,
            QuestType.Onboarding => UiText.QuestOnboardingTitle,
            QuestType.Expedition => UiText.QuestExpeditionTitle,
            QuestType.Ship => UiText.QuestShipTitle,
            QuestType.Loop => UiText.QuestLoopTitle,
            _ => type.ToString()
        };

        private void DrawRow(QuestView view, ref float y)
        {
            if (view?.Def == null) return;
            var def = view.Def;
            var title = UiText.T(def.titleEn, def.titleZh);
            var statusLabel = view.Status switch
            {
                QuestStatus.Claimed => UiText.MissionsStatusDone,
                QuestStatus.Ready => UiText.QuestStepReady,
                QuestStatus.Active => def.type == QuestType.Loop && view.LoopTarget > 0
                    ? UiText.QuestLoopProgress(view.LoopCount, view.LoopTarget)
                    : UiText.MissionsStatusActive,
                _ => UiText.MissionsStatusLocked
            };
            var bg = view.Status == QuestStatus.Ready
                ? NexusTheme.WithAlpha(NexusTheme.Green, 0.16f)
                : view.Status == QuestStatus.Active
                    ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f)
                    : NexusTheme.SurfaceRaised;

            NexusUiFactory.CreateBox(
                root, "Step " + def.questId,
                new Vector2(28f, y), new Vector2(1480f, 78f),
                bg, NexusTheme.BorderSoft);

            NexusUiFactory.CreateText(
                root, "Title " + def.questId,
                $"{def.displayId}  {title}  ·  {statusLabel}",
                new Vector2(44f, y + 8f), new Vector2(900f, 24f), 15f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                root, "Hint " + def.questId,
                QuestService.LocalizedHint(def),
                new Vector2(44f, y + 36f), new Vector2(980f, 28f), 12f, NexusTheme.MutedText);

            bool claimed = view.Status == QuestStatus.Claimed;
            if (!claimed && (view.Status == QuestStatus.Active || view.Status == QuestStatus.Ready)
                && !string.IsNullOrEmpty(def.targetScreen))
            {
                var target = MapTarget(QuestService.TargetScreen(def));
                NexusUiFactory.CreateButton(
                    root, "Go " + def.questId, UiText.MissionsGo,
                    new Vector2(1080f, y + 20f), new Vector2(140f, 40f),
                    () => navigate?.Invoke(target),
                    NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);
            }

            if (view.CanClaim)
            {
                NexusUiFactory.CreateButton(
                    root, "Claim " + def.questId, UiText.MissionsClaim,
                    new Vector2(1240f, y + 20f), new Vector2(160f, 40f),
                    () =>
                    {
                        var r = QuestService.TryClaim(def.questId);
                        statusMessage = r.Success
                            ? UiText.MissionsClaimed(title)
                            : r.Message;
                        Rebuild();
                    },
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f), NexusTheme.Gold, 12f);
            }

            y += 86f;
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

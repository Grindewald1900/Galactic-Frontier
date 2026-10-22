using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Quest;
using Assets.Resources.Scripts.Quest.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Right-edge quest tracker (doc 25 §10): story / expedition-or-ship / onboarding / loops.</summary>
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
                QuestService.EnsureLoaded(DataUtil.Instance);
            }

            float y = 8f;
            NexusUiFactory.CreateText(
                content, "Title", UiText.QuestTrackerTitle,
                new Vector2(12f, y), new Vector2(320f, 24f), 14f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            y += 32f;

            var views = QuestService.GetTrackerViews();
            if (views.Count == 0)
            {
                NexusUiFactory.CreateText(
                    content, "Empty", UiText.QuestTrackerEmpty,
                    new Vector2(12f, y), new Vector2(320f, 48f), 11f, NexusTheme.MutedText);
                y += 56f;
            }
            else
            {
                QuestType? lastType = null;
                foreach (var view in views)
                {
                    if (view?.Def == null) continue;
                    if (lastType != view.Def.type)
                    {
                        NexusUiFactory.CreateText(
                            content, "Head_" + view.Def.type, LayerTitle(view.Def.type),
                            new Vector2(12f, y), new Vector2(320f, 18f), 11f, LayerColor(view.Def.type),
                            TextAlignmentOptions.Left, FontStyles.Bold);
                        y += 22f;
                        lastType = view.Def.type;
                    }

                    DrawQuest(view, ref y);
                }
            }

            panel.sizeDelta = new Vector2(360f, Mathf.Min(720f, y + 16f));
        }

        private void DrawQuest(QuestView view, ref float y)
        {
            bool ready = view.Status == QuestStatus.Ready;
            bool active = view.Status == QuestStatus.Active || ready;
            bool claimable = view.CanClaim;
            bool showGo = active && !claimable && view.Def.type != QuestType.Loop
                && view.Status != QuestStatus.Claimed
                && !string.IsNullOrEmpty(view.Def.targetScreen);
            float height = claimable || showGo || view.Def.type == QuestType.Loop ? 88f : 64f;

            Color row = ready
                ? NexusTheme.WithAlpha(NexusTheme.Green, 0.18f)
                : active
                    ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f)
                    : NexusTheme.SurfaceRaised;

            NexusUiFactory.CreateBox(
                content, "Q_" + view.Def.questId,
                new Vector2(8f, y), new Vector2(344f, height),
                row, NexusTheme.BorderSoft);

            string title = UiText.T(view.Def.titleEn, view.Def.titleZh);
            NexusUiFactory.CreateText(
                content, "QT_" + view.Def.questId, title,
                new Vector2(16f, y + 8f), new Vector2(320f, 20f), 11f,
                active ? NexusTheme.Text : NexusTheme.MutedText,
                TextAlignmentOptions.Left, FontStyles.Bold);

            string status = ready
                ? UiText.QuestStepReady
                : active
                    ? LoopStatus(view)
                    : UiText.QuestStepLocked;
            NexusUiFactory.CreateText(
                content, "QS_" + view.Def.questId, status,
                new Vector2(16f, y + 30f), new Vector2(320f, 18f), 10f, NexusTheme.DimText);

            if (claimable)
            {
                NexusUiFactory.CreateButton(
                    content, "QC_" + view.Def.questId, UiText.QuestClaim,
                    new Vector2(16f, y + 50f), new Vector2(120f, 28f),
                    () =>
                    {
                        QuestService.TryClaim(view.Def.questId);
                        Rebuild();
                    },
                    NexusTheme.WithAlpha(NexusTheme.Green, 0.2f), NexusTheme.Green, 11f);
            }
            else if (showGo)
            {
                var target = MapTarget(QuestService.TargetScreen(view.Def));
                NexusUiFactory.CreateButton(
                    content, "QG_" + view.Def.questId, UiText.QuestGo,
                    new Vector2(16f, y + 50f), new Vector2(120f, 28f),
                    () =>
                    {
                        SetOpen(false);
                        if (view.Def.questId == QuestIds.ChPrologue)
                            ChapterQuestService.StartPrologueBattle();
                        else
                            navigate?.Invoke(target);
                    },
                    NexusTheme.WithAlpha(NexusTheme.Cyan, 0.14f), NexusTheme.Cyan, 11f);
            }

            y += height + 8f;
        }

        private static string LoopStatus(QuestView view)
        {
            if (view.Def.type == QuestType.Loop && view.LoopTarget > 0)
                return UiText.QuestLoopProgress(view.LoopCount, view.LoopTarget);
            return UiText.QuestStepActive;
        }

        private static string LayerTitle(QuestType type) => type switch
        {
            QuestType.Story => UiText.QuestChapterTitle,
            QuestType.Expedition => UiText.QuestExpeditionTitle,
            QuestType.Ship => UiText.QuestShipTitle,
            QuestType.Onboarding => UiText.QuestOnboardingTitle,
            QuestType.Loop => UiText.QuestLoopTitle,
            _ => ""
        };

        private static Color LayerColor(QuestType type) => type switch
        {
            QuestType.Story => NexusTheme.Cyan,
            QuestType.Expedition => NexusTheme.Gold,
            QuestType.Ship => NexusTheme.Purple,
            _ => NexusTheme.MutedText
        };

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

            NexusUiFactory.CreateButton(
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

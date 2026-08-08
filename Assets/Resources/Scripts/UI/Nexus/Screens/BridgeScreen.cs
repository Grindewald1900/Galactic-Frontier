using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Bridge command dashboard: live player/fleet data plus legacy planet/event art.
    /// Aligns with product loop: auto-battle explore, formation, parallel ops placeholders.
    /// </summary>
    internal static class BridgeScreen
    {
        public static GameObject Build(
            Transform parent,
            System.Action openMissions,
            System.Action openFormation,
            System.Action openExplore)
        {
            GameObject root = NexusUiFactory.CreatePanel(
                parent,
                "Bridge Screen",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            NexusUiFactory.CreateText(
                root.transform,
                "Title",
                UiText.BridgeTitle,
                new Vector2(28f, 16f),
                new Vector2(520f, 36f),
                22f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            string commander = DataUtil.Instance?.currentPlayer?.playerName ?? "COMMANDER";
            NexusUiFactory.CreateText(
                root.transform,
                "Subtitle",
                UiText.BridgeSubtitle(commander),
                new Vector2(28f, 52f),
                new Vector2(720f, 22f),
                12f,
                NexusTheme.MutedText);

            NexusUiFactory.CreateText(
                root.transform,
                "AutoBadge",
                UiText.BridgeAutoCombatBadge,
                new Vector2(1460f, 20f),
                new Vector2(280f, 28f),
                12f,
                NexusTheme.Cyan,
                TextAlignmentOptions.Right,
                FontStyles.Bold);

            GameObject banner = NexusUiFactory.CreateBox(
                root.transform,
                "Event Banner",
                new Vector2(28f, 88f),
                new Vector2(1740f, 44f),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.08f),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.25f));
            NexusUiFactory.CreateIcon(
                banner.transform,
                "Icon",
                NexusCardVisual.UiIcon("Battle"),
                new Vector2(12f, 8f),
                new Vector2(28f, 28f),
                NexusTheme.Gold);
            var bannerText = NexusUiFactory.CreateText(
                banner.transform,
                "Text",
                UiText.BridgeBanner,
                new Vector2(48f, 12f),
                new Vector2(1600f, 22f),
                13f,
                NexusTheme.Gold);
            bannerText.textWrappingMode = TextWrappingModes.NoWrap;

            int power = DataUtil.Instance?.currentPlayer?.combatPower ?? 0;
            int credits = DataUtil.Instance?.currentPlayer?.creditPoints ?? 0;
            int progress = DataUtil.Instance?.currentPlayer?.explorationProgress ?? 0;
            int cards = CardListManager.Instance?.GetCardEntities()?.Count ?? 0;
            int inLine = CardListManager.Instance?.GetInLineCardEntities()?.Count ?? 0;
            if (power <= 0 && inLine > 0)
            {
                foreach (var e in CardListManager.Instance.GetInLineCardEntities())
                    if (e != null) power += Mathf.RoundToInt(e.power);
            }

            AddStat(root.transform, new Vector2(28f, 148f), UiText.StatCombatPower, power.ToString("N0"), NexusTheme.Gold);
            AddStat(root.transform, new Vector2(372f, 148f), UiText.StatExploration, $"{progress}%", NexusTheme.Cyan);
            AddStat(root.transform, new Vector2(716f, 148f), UiText.StatCredits, credits.ToString("N0") + "₵", NexusTheme.Purple);
            AddStat(root.transform, new Vector2(1060f, 148f), UiText.StatRoster, UiText.RosterSummary(cards, inLine), NexusTheme.Green);

            // Active fleet with legacy card art
            GameObject fleet = NexusUiFactory.CreateBox(
                root.transform,
                "Fleet",
                new Vector2(28f, 288f),
                new Vector2(1040f, 360f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                fleet.transform,
                "Heading",
                UiText.ActiveFleet,
                new Vector2(20f, 12f),
                new Vector2(400f, 28f),
                16f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            List<CardEntity> lineup = CardListManager.Instance?.GetInLineCardEntities();
            float x = 20f;
            if (lineup != null && lineup.Count > 0)
            {
                foreach (CardEntity entity in lineup)
                {
                    if (entity == null) continue;
                    string footer = $"Lv.{entity.Level} · {UiText.SlotLabel((int)entity.GetLineupPosition())}";
                    NexusCardVisual.CreatePortraitCard(
                        fleet.transform,
                        $"Unit {entity.id}",
                        entity,
                        new Vector2(x, 52f),
                        new Vector2(180f, 280f),
                        footer);
                    x += 200f;
                }
            }
            else
            {
                NexusUiFactory.CreateText(
                    fleet.transform,
                    "Empty",
                    UiText.EmptyFleet,
                    new Vector2(24f, 140f),
                    new Vector2(700f, 40f),
                    14f,
                    NexusTheme.MutedText);
            }

            // Sector / planet strip (legacy planet art)
            GameObject sectors = NexusUiFactory.CreateBox(
                root.transform,
                "Sectors",
                new Vector2(28f, 668f),
                new Vector2(1040f, 200f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                sectors.transform,
                "Heading",
                UiText.BridgeSectors,
                new Vector2(20f, 12f),
                new Vector2(400f, 24f),
                15f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            string[] sectorNames =
            {
                UiText.SectorName(0), UiText.SectorName(1), UiText.SectorName(2),
                UiText.SectorName(3), UiText.SectorName(4)
            };
            for (int i = 0; i < 5; i++)
            {
                float sx = 20f + i * 200f;
                GameObject cell = NexusUiFactory.CreateBox(
                    sectors.transform,
                    $"Sector {i}",
                    new Vector2(sx, 48f),
                    new Vector2(180f, 130f),
                    NexusTheme.SurfaceRaised,
                    NexusTheme.BorderSoft);
                NexusUiFactory.CreateIcon(
                    cell.transform,
                    "Planet",
                    NexusCardVisual.PlanetSprite(i * 3),
                    new Vector2(40f, 8f),
                    new Vector2(100f, 80f),
                    Color.white);
                NexusUiFactory.CreateText(
                    cell.transform,
                    "Name",
                    sectorNames[i],
                    new Vector2(8f, 96f),
                    new Vector2(164f, 28f),
                    12f,
                    NexusTheme.MutedText,
                    TextAlignmentOptions.Center);
            }

            // Event log with legacy event icons
            GameObject log = NexusUiFactory.CreateBox(
                root.transform,
                "Log",
                new Vector2(1090f, 288f),
                new Vector2(678f, 420f),
                NexusTheme.Surface,
                NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                log.transform,
                "Heading",
                UiText.EventLog,
                new Vector2(20f, 12f),
                new Vector2(400f, 28f),
                16f,
                NexusTheme.Text,
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            string[] logLines =
            {
                UiText.BridgeLogLine(0, inLine, cards),
                UiText.BridgeLogLine(1, progress, credits),
                UiText.BridgeLogLine(2, power, 0),
                UiText.BridgeLogLine(3, 0, 0),
                UiText.BridgeLogLine(4, 0, 0)
            };
            for (int i = 0; i < logLines.Length; i++)
            {
                float ly = 56f + i * 68f;
                GameObject row = NexusUiFactory.CreateBox(
                    log.transform,
                    $"LogRow {i}",
                    new Vector2(16f, ly),
                    new Vector2(646f, 56f),
                    NexusTheme.SurfaceRaised,
                    NexusTheme.BorderSoft);
                NexusUiFactory.CreateIcon(
                    row.transform,
                    "Icon",
                    NexusCardVisual.EventSprite(i),
                    new Vector2(10f, 8f),
                    new Vector2(40f, 40f),
                    Color.white);
                var line = NexusUiFactory.CreateText(
                    row.transform,
                    "Text",
                    logLines[i],
                    new Vector2(60f, 10f),
                    new Vector2(560f, 36f),
                    12f,
                    NexusTheme.Text);
                line.textWrappingMode = TextWrappingModes.Normal;
            }

            // Quick actions aligned with product: formation, explore auto-battle, missions
            NexusUiFactory.CreateButton(
                root.transform,
                "CTA Formation",
                UiText.BridgeOpenFormation,
                new Vector2(1090f, 730f),
                new Vector2(210f, 44f),
                () => openFormation?.Invoke(),
                NexusTheme.SurfaceRaised,
                NexusTheme.Text,
                13f);
            NexusUiFactory.CreateButton(
                root.transform,
                "CTA Explore",
                UiText.BridgeStartAutoBattle,
                new Vector2(1320f, 730f),
                new Vector2(210f, 44f),
                () => openExplore?.Invoke(),
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f),
                NexusTheme.Gold,
                13f);
            NexusUiFactory.CreateButton(
                root.transform,
                "CTA Missions",
                UiText.TodaysMissions,
                new Vector2(1550f, 730f),
                new Vector2(210f, 44f),
                () => openMissions?.Invoke(),
                NexusTheme.SurfaceRaised,
                NexusTheme.Cyan,
                13f);

            NexusUiFactory.CreateText(
                root.transform,
                "OpsHint",
                UiText.BridgeOpsHint,
                new Vector2(1090f, 790f),
                new Vector2(670f, 40f),
                11f,
                NexusTheme.DimText);

            return root;
        }

        private static void AddStat(Transform parent, Vector2 position, string label, string value, Color accent)
        {
            GameObject card = NexusUiFactory.CreateBox(parent, $"Stat {label}", position, new Vector2(320f, 118f), NexusTheme.SurfaceRaised, NexusTheme.BorderSoft);
            NexusUiFactory.CreatePanel(card.transform, "Accent", accent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 0f));
            NexusUiFactory.CreateText(card.transform, "Value", value, new Vector2(18f, 20f), new Vector2(280f, 36f), 24f, NexusTheme.Text, TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(card.transform, "Label", label, new Vector2(18f, 68f), new Vector2(280f, 22f), 12f, NexusTheme.MutedText);
        }
    }
}

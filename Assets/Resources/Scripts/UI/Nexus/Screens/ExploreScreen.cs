using System.Collections.Generic;
using System.Text;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Explore region select with prereq/ship hard gates and farm CTA (P2).
    /// </summary>
    internal sealed class ExploreScreen
    {
        private readonly Transform root;
        private readonly System.Action openFormation;
        private readonly System.Action openShip;

        private ExploreScreen(Transform root, System.Action openFormation, System.Action openShip)
        {
            this.root = root;
            this.openFormation = openFormation;
            this.openShip = openShip;
        }

        public GameObject Root => root.gameObject;

        public static ExploreScreen Build(Transform parent, System.Action openFormation, System.Action openShip = null)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent,
                "Explore Screen",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            var screen = new ExploreScreen(panel.transform, openFormation, openShip);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            if (DataUtil.Instance != null)
            {
                WorldService.EnsureLoaded(DataUtil.Instance);
                ShipService.EnsureLoaded(DataUtil.Instance);
                if (CardListManager.Instance != null)
                    DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance.cardEntities);
            }

            NexusUiFactory.CreateText(
                root, "Title", UiText.ExploreTitle,
                new Vector2(28f, 20f), new Vector2(640f, 40f), 22f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var hint = NexusUiFactory.CreateText(
                root, "Hint", UiText.ExploreHint,
                new Vector2(28f, 64f), new Vector2(1100f, 40f), 13f, NexusTheme.MutedText);
            hint.textWrappingMode = TextWrappingModes.Normal;
            OnboardingBanner.TryDraw(root, AppScreen.Battle, new Vector2(28f, 100f));

            int inLine = CardListManager.Instance?.GetInLineCardEntities()?.Count ?? 0;
            NexusUiFactory.CreateText(
                root, "FleetReady", UiText.ExploreFleetReady(inLine),
                new Vector2(28f, 110f), new Vector2(600f, 24f), 13f,
                inLine > 0 ? NexusTheme.Green : NexusTheme.Red);

            if (WorldService.IsSectorComplete())
            {
                NexusUiFactory.CreateText(
                    root, "SectorComplete", UiText.SectorComplete,
                    new Vector2(650f, 110f), new Vector2(500f, 24f), 13f, NexusTheme.Gold,
                    TextAlignmentOptions.Left, FontStyles.Bold);
            }

            var views = new List<RegionView>();
            foreach (var v in WorldService.GetAllRegionViews())
                views.Add(v);
            views.Sort((a, b) => (a.Config?.sortOrder ?? 0).CompareTo(b.Config?.sortOrder ?? 0));

            for (var i = 0; i < views.Count; i++)
            {
                var view = views[i];
                float y = 150f + i * 118f;
                BuildRegionRow(view, y, i);
            }

            GameObject side = NexusUiFactory.CreateBox(
                root, "Side", new Vector2(1260f, 150f), new Vector2(460f, 620f),
                NexusTheme.Surface, NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                side.transform, "SideTitle", UiText.ExploreSideTitle,
                new Vector2(20f, 16f), new Vector2(400f, 28f), 15f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            var body = NexusUiFactory.CreateText(
                side.transform, "SideBody", UiText.ExploreSideBody,
                new Vector2(20f, 56f), new Vector2(420f, 220f), 13f, NexusTheme.MutedText);
            body.textWrappingMode = TextWrappingModes.Normal;

            NexusUiFactory.CreateButton(
                side.transform, "Formation", UiText.BridgeOpenFormation,
                new Vector2(20f, 320f), new Vector2(420f, 48f),
                () => openFormation?.Invoke(),
                NexusTheme.SurfaceRaised, NexusTheme.Text, 14f);
            NexusUiFactory.CreateButton(
                side.transform, "Ship", UiText.OpenShipBay,
                new Vector2(20f, 380f), new Vector2(420f, 48f),
                () => openShip?.Invoke(),
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan, 14f);

            BuildGatherBank(side.transform);
        }

        /// <summary>
        /// Gather output waits here instead of trickling into the warehouse, so collecting it is an
        /// explicit act with a reward summary.
        /// </summary>
        private void BuildGatherBank(Transform side)
        {
            Economy.IdleSettlementService.EnsureLoaded();
            int total = Economy.IdleSettlementService.GatherBankTotal;
            bool full = Economy.IdleSettlementService.IsGatherBankFull;

            NexusUiFactory.CreateBox(
                side, "GatherBank", new Vector2(20f, 450f), new Vector2(420f, 150f),
                NexusTheme.SurfaceRaised, NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                side, "GatherBankTitle", UiText.GatherBankTitle,
                new Vector2(36f, 466f), new Vector2(388f, 24f), 14f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            string body = total <= 0
                ? UiText.GatherBankEmpty
                : full ? UiText.GatherBankFull : UiText.GatherBankHint;
            var bodyText = NexusUiFactory.CreateText(
                side, "GatherBankBody", body,
                new Vector2(36f, 494f), new Vector2(388f, 40f), 12f,
                full ? NexusTheme.Red : NexusTheme.MutedText);
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            if (total <= 0)
                return;

            NexusUiFactory.CreateButton(
                side, "CollectGather", UiText.CollectGather(total),
                new Vector2(36f, 540f), new Vector2(388f, 44f),
                CollectGather,
                NexusTheme.WithAlpha(NexusTheme.Green, 0.2f), NexusTheme.Green, 13f);
        }

        private void CollectGather()
        {
            var result = Economy.IdleSettlementService.CollectGatherBank(out var collected);
            if (!result.Success)
                Debug.LogWarning("[GATHER] collect: " + result.Message);

            RewardPopup.Show(UiText.RewardTitle, RewardPopup.FromPending(collected));
            Rebuild();
        }

        private void BuildRegionRow(RegionView view, float y, int visualIndex)
        {
            var cfg = view.Config;
            if (cfg == null) return;
            string regionId = cfg.regionId;

            GameObject row = NexusUiFactory.CreateBox(
                root, $"Region {regionId}",
                new Vector2(28f, y), new Vector2(1200f, 108f),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            NexusUiFactory.CreateIcon(
                row.transform, "Planet", NexusCardVisual.PlanetSprite(visualIndex * 3),
                new Vector2(16f, 14f), new Vector2(70f, 70f), Color.white);

            string name = UiText.T(cfg.displayNameEn, cfg.displayNameZh);
            if (cfg.bossRegion)
                name = "★ " + name;

            NexusUiFactory.CreateText(
                row.transform, "Name", name,
                new Vector2(100f, 8f), new Vector2(480f, 24f), 15f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var blurb = UiText.T(cfg.blurbEn, cfg.blurbZh);
            if (!string.IsNullOrEmpty(blurb))
            {
                NexusUiFactory.CreateText(
                    row.transform, "Blurb", blurb,
                    new Vector2(100f, 34f), new Vector2(700f, 22f), 11f, NexusTheme.Cyan);
            }

            NexusUiFactory.CreateText(
                row.transform, "Meta", BuildMeta(view),
                new Vector2(100f, 58f), new Vector2(700f, 36f), 11f, NexusTheme.MutedText);

            if (view.CanEnter)
            {
                NexusUiFactory.CreateButton(
                    row.transform, "Start", UiText.StartAutoBattle,
                    new Vector2(820f, 32f), new Vector2(170f, 44f),
                    () => StartBattle(regionId),
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 13f);
            }
            else
            {
                NexusUiFactory.CreateText(
                    row.transform, "Locked", UiText.RegionLocked,
                    new Vector2(820f, 40f), new Vector2(170f, 30f), 12f, NexusTheme.DimText,
                    TextAlignmentOptions.Center, FontStyles.Bold);
            }

            if (view.FarmUnlocked)
            {
                NexusUiFactory.CreateButton(
                    row.transform, "Farm", UiText.StartFarm,
                    new Vector2(1000f, 32f), new Vector2(90f, 44f),
                    () => StartFarm(regionId),
                    NexusTheme.WithAlpha(NexusTheme.Cyan, 0.18f), NexusTheme.Cyan, 12f);

                var nodes = Economy.Domain.GatherNodeCatalog.ForRegion(regionId);
                if (nodes.Count > 0)
                {
                    var nodeId = nodes[0].nodeId;
                    NexusUiFactory.CreateButton(
                        row.transform, "Gather", UiText.StartGather,
                        new Vector2(1100f, 32f), new Vector2(90f, 44f),
                        () => StartGather(nodeId),
                        NexusTheme.WithAlpha(NexusTheme.Green, 0.18f), NexusTheme.Green, 12f);
                }
            }
        }

        private void StartGather(string nodeId)
        {
            var result = Economy.ProductionService.TryStartGather(
                nodeId, CardListManager.Instance?.cardEntities);
            if (!result.Success)
                Debug.LogWarning("[GATHER] " + result.Message);
            else
                Debug.Log("[GATHER] started " + nodeId);
            Rebuild();
        }

        private static string BuildMeta(RegionView view)
        {
            var sb = new StringBuilder();
            sb.Append(UiText.RegionProgressLabel(view.Progress.ToString()));
            sb.Append(" · ");
            sb.Append(UiText.ExploreRecPower(view.Config.recommendedPower));
            var faction = FactionTags.DisplayEn(view.Config.factionTag);
            var factionZh = FactionTags.DisplayZh(view.Config.factionTag);
            if (!string.IsNullOrEmpty(faction))
            {
                sb.Append(" · ");
                sb.Append(UiText.T(faction, factionZh));
            }

            if (view.BlockReasons.Count > 0)
            {
                sb.Append(" · ");
                sb.Append(string.Join("; ", view.BlockReasons));
            }
            else if (view.FarmUnlocked)
            {
                sb.Append(" · ");
                sb.Append(UiText.FarmAvailable);
            }

            return sb.ToString();
        }

        private void StartBattle(string regionId)
        {
            if (!WorldService.CanEnter(regionId, out var view) || view?.Config == null)
            {
                Debug.LogWarning("[EXPLORE] CanEnter failed for " + regionId);
                Rebuild();
                return;
            }

            int inLine = CardListManager.Instance?.GetInLineCardEntities()?.Count ?? 0;
            if (inLine <= 0)
            {
                openFormation?.Invoke();
                return;
            }

            var deck = DeckService.GetActiveCombatDeck();
            if (deck == null || deck.MemberCount < 1)
            {
                openFormation?.Invoke();
                return;
            }

            var start = DeckService.TryStart(
                deck.deckId, DeckActionType.MainCombat, regionId, CardListManager.Instance?.cardEntities);
            if (!start.Success)
            {
                Debug.LogWarning("[DECK] Cannot start battle: " + start.Message);
                return;
            }

            PlayerPrefs.SetString("nexus_last_region_id", regionId);
            PlayerPrefs.Save();

            BattleController.PendingBattleTargetId = regionId;
            BattleController.PendingEncounterId = view.Config.mainEncounterId;
            BattleController.PendingBattleSeed =
                unchecked(regionId.GetHashCode() * 1_000_003L ^ System.DateTime.UtcNow.Ticks);
            LoadingOverlay.LoadScene(nameof(SceneLoader.SceneName.BattleScene));
        }

        private void StartFarm(string regionId)
        {
            if (!WorldService.IsFarmUnlocked(regionId))
            {
                Debug.LogWarning("[EXPLORE] Farm locked for " + regionId);
                return;
            }

            var deck = DeckService.GetActiveCombatDeck();
            if (deck == null || deck.MemberCount < 1)
            {
                openFormation?.Invoke();
                return;
            }

            var start = DeckService.TryStart(
                deck.deckId, DeckActionType.AutoCombat, regionId, CardListManager.Instance?.cardEntities);
            if (!start.Success)
            {
                Debug.LogWarning("[DECK] Cannot start farm: " + start.Message);
                return;
            }

            Debug.Log("[FARM] AutoCombat started on " + regionId);
            Rebuild();
        }
    }
}

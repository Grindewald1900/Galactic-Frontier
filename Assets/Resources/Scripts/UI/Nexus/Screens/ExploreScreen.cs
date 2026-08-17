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
    /// M1 stellar map: Frontier VII 2D sector with ship position, radar reveal, and cruise-to-dock.
    /// Combat/farm/gather still use region gates from WorldRules (doc 03).
    /// </summary>
    internal sealed class ExploreScreen
    {
        private const float MapX = 28f;
        private const float MapY = 140f;
        private const float MapW = 700f;
        private const float MapH = 640f;
        private const float MapPad = 28f;
        /// <summary>Visual-only shrink so the radar footprint does not dominate the compact map.</summary>
        private const float RadarVisualScale = 0.55f;
        private const float ChartedX = 748f;
        private const float ChartedW = 470f;
        private const float SideX = 1240f;

        private readonly Transform root;
        private readonly System.Action openFormation;
        private readonly System.Action openShip;

        private string selectedBodyId = "body_outer_haven";
        private string lastNavMessage = "";

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

            NavigationService.EnsureReady();

            NexusUiFactory.CreateText(
                root, "Title", UiText.ExploreTitle,
                new Vector2(28f, 20f), new Vector2(720f, 40f), 22f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var hint = NexusUiFactory.CreateText(
                root, "Hint", UiText.ExploreHint,
                new Vector2(28f, 64f), new Vector2(1500f, 40f), 13f, NexusTheme.MutedText);
            hint.textWrappingMode = TextWrappingModes.Normal;
            OnboardingBanner.TryDraw(root, AppScreen.Battle, new Vector2(28f, 100f));

            int inLine = CardListManager.Instance?.GetInLineCardEntities()?.Count ?? 0;
            NexusUiFactory.CreateText(
                root, "FleetReady", UiText.ExploreFleetReady(inLine),
                new Vector2(28f, 110f), new Vector2(600f, 24f), 13f,
                inLine > 0 ? NexusTheme.Green : NexusTheme.Red);

            float radar = NavigationService.GetRadarRange();
            NexusUiFactory.CreateText(
                root, "NavStatus",
                UiText.ExploreNavStatus(NavigationService.ShipX, NavigationService.ShipY, radar),
                new Vector2(640f, 110f), new Vector2(560f, 24f), 13f, NexusTheme.Cyan);

            if (WorldService.IsSectorComplete())
            {
                NexusUiFactory.CreateText(
                    root, "SectorComplete", UiText.SectorComplete,
                    new Vector2(1240f, 110f), new Vector2(420f, 24f), 13f, NexusTheme.Gold,
                    TextAlignmentOptions.Left, FontStyles.Bold);
            }

            BuildSectorMap();
            BuildChartedList();
            BuildSidePanel();
        }

        private void BuildSectorMap()
        {
            GameObject map = NexusUiFactory.CreateBox(
                root, "SectorMap", new Vector2(MapX, MapY), new Vector2(MapW, MapH),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            NexusUiFactory.CreateText(
                map.transform, "MapLabel", UiText.ExploreMapLabel,
                new Vector2(12f, 8f), new Vector2(420f, 22f), 12f, NexusTheme.MutedText,
                TextAlignmentOptions.Left, FontStyles.Bold);

            // Soft sector cluster ring (center of Frontier VII) — compact visual.
            float clusterScale = 0.7f;
            DrawMapMarker(
                map.transform, "ClusterRing",
                SectorMapCatalog.SectorCenterX, SectorMapCatalog.SectorCenterY,
                SectorMapCatalog.SectorRadius * 2f * MapInnerW / SectorMapCatalog.MapMax * clusterScale,
                SectorMapCatalog.SectorRadius * 2f * MapInnerH / SectorMapCatalog.MapMax * clusterScale,
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.05f));

            float radarPx = NavigationService.GetRadarRange() * 2f * MapInnerW / SectorMapCatalog.MapMax * RadarVisualScale;
            float radarPy = NavigationService.GetRadarRange() * 2f * MapInnerH / SectorMapCatalog.MapMax * RadarVisualScale;
            DrawMapMarker(
                map.transform, "Radar",
                NavigationService.ShipX, NavigationService.ShipY,
                radarPx, radarPy,
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.14f));

            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null || !NavigationService.IsVisible(body))
                    continue;
                BuildBodyMarker(map.transform, body);
            }

            // Ship on top.
            var shipPos = SectorToLocal(NavigationService.ShipX, NavigationService.ShipY);
            const float shipSize = 16f;
            NexusUiFactory.CreateBox(
                map.transform, "Ship",
                new Vector2(shipPos.x - shipSize * 0.5f, shipPos.y - shipSize * 0.5f),
                new Vector2(shipSize, shipSize),
                NexusTheme.Gold, NexusTheme.Gold);
            NexusUiFactory.CreateText(
                map.transform, "ShipLabel", UiText.ExploreShipMarker,
                new Vector2(shipPos.x - 28f, shipPos.y + 10f), new Vector2(56f, 16f), 10f, NexusTheme.Gold,
                TextAlignmentOptions.Center, FontStyles.Bold);

            NexusUiFactory.CreateButton(
                map.transform, "CruiseRandom", UiText.ExploreCruiseRandom,
                new Vector2(12f, MapH - 48f), new Vector2(170f, 36f),
                CruiseRandom,
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan, 11f);

            if (!string.IsNullOrEmpty(lastNavMessage))
            {
                var msg = NexusUiFactory.CreateText(
                    map.transform, "NavMsg", lastNavMessage,
                    new Vector2(192f, MapH - 44f), new Vector2(480f, 28f), 11f, NexusTheme.MutedText);
                msg.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private void BuildChartedList()
        {
            GameObject list = NexusUiFactory.CreateBox(
                root, "ChartedList", new Vector2(ChartedX, MapY), new Vector2(ChartedW, MapH),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            int knownCount = 0;
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body != null && NavigationService.IsKnown(body.bodyId))
                    knownCount++;
            }

            NexusUiFactory.CreateText(
                list.transform, "Title", UiText.ExploreChartedTitle(knownCount),
                new Vector2(16f, 14f), new Vector2(ChartedW - 32f, 28f), 15f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var hint = NexusUiFactory.CreateText(
                list.transform, "Hint", UiText.ExploreChartedHint,
                new Vector2(16f, 46f), new Vector2(ChartedW - 32f, 36f), 12f, NexusTheme.MutedText);
            hint.textWrappingMode = TextWrappingModes.Normal;

            if (knownCount <= 0)
            {
                NexusUiFactory.CreateText(
                    list.transform, "Empty", UiText.ExploreChartedEmpty,
                    new Vector2(16f, 100f), new Vector2(ChartedW - 32f, 40f), 13f, NexusTheme.DimText);
                return;
            }

            float y = 96f;
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null || !NavigationService.IsKnown(body.bodyId))
                    continue;
                BuildChartedRow(list.transform, body, y);
                y += 82f;
            }
        }

        private void BuildChartedRow(Transform list, StellarBodyDef body, float y)
        {
            bool selected = body.bodyId == selectedBodyId;
            bool docked = NavigationService.IsDocked(body);
            string regionId = PrimaryRegion(body);
            var view = string.IsNullOrEmpty(regionId) ? null : WorldService.GetRegionView(regionId);

            Color rowBg = selected
                ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f)
                : NexusTheme.SurfaceRaised;
            Color outline = selected ? NexusTheme.Gold : NexusTheme.BorderSoft;

            var row = NexusUiFactory.CreateButton(
                list, "Chart_" + body.bodyId, "",
                new Vector2(16f, y), new Vector2(ChartedW - 32f, 74f),
                () => SelectBody(body.bodyId),
                rowBg, NexusTheme.Text, 1f);
            var outlineFx = row.GetComponent<UnityEngine.UI.Outline>();
            if (outlineFx != null)
                outlineFx.effectColor = outline;

            var label = row.transform.Find("Label");
            if (label != null)
                Object.DestroyImmediate(label.gameObject);

            NexusUiFactory.CreateIcon(
                row.transform, "Icon",
                NexusCardVisual.PlanetSprite(body.planetSpriteIndex),
                new Vector2(10f, 10f), new Vector2(52f, 52f), Color.white);

            string name = UiText.T(body.displayNameEn, body.displayNameZh);
            if (view?.Config != null && view.Config.bossRegion)
                name = "★ " + name;

            NexusUiFactory.CreateText(
                row.transform, "Name", name,
                new Vector2(74f, 8f), new Vector2(340f, 22f), 14f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            string status = docked
                ? UiText.ExploreListDocked
                : UiText.ExploreListDist(NavigationService.DistanceToBody(body));
            if (view != null)
                status += " · " + UiText.RegionProgressLabel(view.Progress.ToString());

            var statusText = NexusUiFactory.CreateText(
                row.transform, "Status", status,
                new Vector2(74f, 34f), new Vector2(340f, 32f), 11f,
                docked ? NexusTheme.Green : NexusTheme.MutedText);
            statusText.textWrappingMode = TextWrappingModes.Normal;
        }

        private void BuildBodyMarker(Transform map, StellarBodyDef body)
        {
            var local = SectorToLocal(body.x, body.y);
            bool selected = body.bodyId == selectedBodyId;
            bool docked = NavigationService.IsDocked(body);
            float size = selected ? 42f : 34f;
            Color tint = BodyTint(body.bodyType);
            if (docked)
                tint = NexusTheme.Green;

            string regionId = PrimaryRegion(body);
            var view = string.IsNullOrEmpty(regionId) ? null : WorldService.GetRegionView(regionId);
            int spriteIndex = body.planetSpriteIndex;
            if (view?.Config != null)
                spriteIndex = Mathf.Max(spriteIndex, 0);

            var marker = NexusUiFactory.CreateButton(
                map, "Body_" + body.bodyId, "",
                new Vector2(local.x - size * 0.5f, local.y - size * 0.5f),
                new Vector2(size, size),
                () => SelectBody(body.bodyId),
                NexusTheme.WithAlpha(tint, selected ? 0.35f : 0.18f),
                tint, 1f);

            // Clear default empty label footprint; draw planet art instead.
            var label = marker.transform.Find("Label");
            if (label != null)
                Object.DestroyImmediate(label.gameObject);

            NexusUiFactory.CreateIcon(
                marker.transform, "Icon",
                NexusCardVisual.PlanetSprite(spriteIndex),
                new Vector2(4f, 4f), new Vector2(size - 8f, size - 8f), Color.white);

            string name = UiText.T(body.displayNameEn, body.displayNameZh);
            if (view?.Config != null && view.Config.bossRegion)
                name = "★ " + name;

            NexusUiFactory.CreateText(
                map, "Name_" + body.bodyId, name,
                new Vector2(local.x - 54f, local.y + size * 0.5f + 2f),
                new Vector2(108f, 16f), 10f,
                selected ? NexusTheme.Text : NexusTheme.MutedText,
                TextAlignmentOptions.Center, selected ? FontStyles.Bold : FontStyles.Normal);
        }

        private void BuildSidePanel()
        {
            GameObject side = NexusUiFactory.CreateBox(
                root, "Side", new Vector2(SideX, MapY), new Vector2(460f, MapH),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            NexusUiFactory.CreateText(
                side.transform, "SideTitle", UiText.ExploreSideTitle,
                new Vector2(20f, 16f), new Vector2(420f, 28f), 15f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var body = SectorMapCatalog.Get(selectedBodyId);
            if (body == null)
            {
                var fallback = NexusUiFactory.CreateText(
                    side.transform, "SideBody", UiText.ExploreSideBody,
                    new Vector2(20f, 56f), new Vector2(420f, 200f), 13f, NexusTheme.MutedText);
                fallback.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                BuildBodyDetail(side.transform, body);
            }

            NexusUiFactory.CreateButton(
                side.transform, "Formation", UiText.BridgeOpenFormation,
                new Vector2(20f, 460f), new Vector2(420f, 40f),
                () => openFormation?.Invoke(),
                NexusTheme.SurfaceRaised, NexusTheme.Text, 13f);
            NexusUiFactory.CreateButton(
                side.transform, "Ship", UiText.OpenShipBay,
                new Vector2(20f, 508f), new Vector2(420f, 40f),
                () => openShip?.Invoke(),
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan, 13f);

            BuildGatherBank(side.transform);
        }

        private void BuildBodyDetail(Transform side, StellarBodyDef body)
        {
            string name = UiText.T(body.displayNameEn, body.displayNameZh);
            NexusUiFactory.CreateText(
                side, "BodyName", name,
                new Vector2(20f, 52f), new Vector2(420f, 28f), 16f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            string regionId = PrimaryRegion(body);
            var view = string.IsNullOrEmpty(regionId) ? null : WorldService.GetRegionView(regionId);
            float dist = NavigationService.DistanceToBody(body);
            bool docked = NavigationService.IsDocked(body);
            float eta = NavigationService.EstimateCruiseSeconds(dist);

            var status = NexusUiFactory.CreateText(
                side, "BodyStatus",
                docked
                    ? UiText.ExploreDocked
                    : UiText.ExploreSailEta(dist, eta),
                new Vector2(20f, 84f), new Vector2(420f, 40f), 12f,
                docked ? NexusTheme.Green : NexusTheme.MutedText);
            status.textWrappingMode = TextWrappingModes.Normal;

            if (view?.Config != null)
            {
                var blurb = UiText.T(view.Config.blurbEn, view.Config.blurbZh);
                if (!string.IsNullOrEmpty(blurb))
                {
                    var blurbText = NexusUiFactory.CreateText(
                        side, "Blurb", blurb,
                        new Vector2(20f, 128f), new Vector2(420f, 48f), 12f, NexusTheme.Cyan);
                    blurbText.textWrappingMode = TextWrappingModes.Normal;
                }

                var meta = NexusUiFactory.CreateText(
                    side, "Meta", BuildMeta(view),
                    new Vector2(20f, 180f), new Vector2(420f, 72f), 12f, NexusTheme.MutedText);
                meta.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                var help = NexusUiFactory.CreateText(
                    side, "Help", UiText.ExploreSideBody,
                    new Vector2(20f, 128f), new Vector2(420f, 120f), 12f, NexusTheme.MutedText);
                help.textWrappingMode = TextWrappingModes.Normal;
            }

            float actionY = 270f;
            if (!docked)
            {
                NexusUiFactory.CreateButton(
                    side, "Sail", UiText.ExploreSailHere,
                    new Vector2(20f, actionY), new Vector2(420f, 48f),
                    () => CruiseToBody(body.bodyId),
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.2f), NexusTheme.Gold, 14f);
                return;
            }

            if (view == null || view.Config == null)
                return;

            if (view.CanEnter)
            {
                NexusUiFactory.CreateButton(
                    side, "Start", UiText.StartAutoBattle,
                    new Vector2(20f, actionY), new Vector2(420f, 48f),
                    () => StartBattle(regionId),
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 14f);
            }
            else
            {
                NexusUiFactory.CreateText(
                    side, "Locked", UiText.RegionLocked,
                    new Vector2(20f, actionY + 8f), new Vector2(420f, 32f), 13f, NexusTheme.DimText,
                    TextAlignmentOptions.Center, FontStyles.Bold);
            }

            actionY += 56f;
            if (view.FarmUnlocked)
            {
                NexusUiFactory.CreateButton(
                    side, "Farm", UiText.StartFarm,
                    new Vector2(20f, actionY), new Vector2(200f, 44f),
                    () => StartFarm(regionId),
                    NexusTheme.WithAlpha(NexusTheme.Cyan, 0.18f), NexusTheme.Cyan, 13f);

                var nodes = Economy.Domain.GatherNodeCatalog.ForRegion(regionId);
                if (nodes.Count > 0)
                {
                    var nodeId = nodes[0].nodeId;
                    NexusUiFactory.CreateButton(
                        side, "Gather", UiText.StartGather,
                        new Vector2(240f, actionY), new Vector2(200f, 44f),
                        () => StartGather(nodeId),
                        NexusTheme.WithAlpha(NexusTheme.Green, 0.18f), NexusTheme.Green, 13f);
                }
            }
        }

        private void BuildGatherBank(Transform side)
        {
            Economy.IdleSettlementService.EnsureLoaded();
            int total = Economy.IdleSettlementService.GatherBankTotal;
            bool full = Economy.IdleSettlementService.IsGatherBankFull;

            NexusUiFactory.CreateBox(
                side, "GatherBank", new Vector2(20f, 560f), new Vector2(420f, 64f),
                NexusTheme.SurfaceRaised, NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                side, "GatherBankTitle", UiText.GatherBankTitle,
                new Vector2(36f, 568f), new Vector2(200f, 22f), 12f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            string body = total <= 0
                ? UiText.GatherBankEmpty
                : full ? UiText.GatherBankFull : UiText.GatherBankHint;
            var bodyText = NexusUiFactory.CreateText(
                side, "GatherBankBody", body,
                new Vector2(36f, 592f), new Vector2(250f, 22f), 11f,
                full ? NexusTheme.Red : NexusTheme.MutedText);
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            if (total <= 0)
                return;

            NexusUiFactory.CreateButton(
                side, "CollectGather", UiText.CollectGather(total),
                new Vector2(280f, 574f), new Vector2(144f, 36f),
                CollectGather,
                NexusTheme.WithAlpha(NexusTheme.Green, 0.2f), NexusTheme.Green, 12f);
        }

        private void SelectBody(string bodyId)
        {
            selectedBodyId = bodyId ?? "";
            Rebuild();
        }

        private void CruiseToBody(string bodyId)
        {
            var result = NavigationService.TryCruiseToBody(bodyId);
            lastNavMessage = string.IsNullOrEmpty(result.Message)
                ? (result.Success ? UiText.ExploreArrived : UiText.ExploreCruiseFailed)
                : result.Message;
            if (result.Success)
                selectedBodyId = bodyId;
            Rebuild();
        }

        private void CruiseRandom()
        {
            var result = NavigationService.TryCruiseRandom(18f);
            lastNavMessage = string.IsNullOrEmpty(result.Message)
                ? (result.Success ? UiText.ExploreArrived : UiText.ExploreCruiseFailed)
                : result.Message;
            Rebuild();
        }

        private void CollectGather()
        {
            var result = Economy.IdleSettlementService.CollectGatherBank(out var collected);
            if (!result.Success)
                Debug.LogWarning("[GATHER] collect: " + result.Message);

            RewardPopup.Show(UiText.RewardTitle, RewardPopup.FromPending(collected));
            Rebuild();
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

        private static float MapInnerW => MapW - MapPad * 2f;
        private static float MapInnerH => MapH - MapPad * 2f - 28f;

        private static Vector2 SectorToLocal(float sx, float sy)
        {
            float nx = Mathf.InverseLerp(SectorMapCatalog.MapMin, SectorMapCatalog.MapMax, sx);
            // High sector Y (outer edge / spawn) maps near the top of the panel.
            float ny = Mathf.InverseLerp(SectorMapCatalog.MapMin, SectorMapCatalog.MapMax, sy);
            return new Vector2(MapPad + nx * MapInnerW, MapPad + 20f + (1f - ny) * MapInnerH);
        }

        private static void DrawMapMarker(
            Transform map, string name, float sx, float sy, float w, float h, Color color)
        {
            var center = SectorToLocal(sx, sy);
            NexusUiFactory.CreateBox(
                map, name,
                new Vector2(center.x - w * 0.5f, center.y - h * 0.5f),
                new Vector2(Mathf.Max(8f, w), Mathf.Max(8f, h)),
                color);
        }

        private static Color BodyTint(StellarBodyType type) => type switch
        {
            StellarBodyType.Station => NexusTheme.Cyan,
            StellarBodyType.Anomaly => NexusTheme.Gold,
            StellarBodyType.Hub => NexusTheme.Gold,
            _ => NexusTheme.Text
        };

        private static string PrimaryRegion(StellarBodyDef body)
        {
            if (body?.regionIds == null || body.regionIds.Length == 0)
                return "";
            return body.regionIds[0] ?? "";
        }
    }
}

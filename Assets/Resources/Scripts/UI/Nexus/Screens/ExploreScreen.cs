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
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Two-layer Explore map: zoom out of a sector jumps to the universe;
    /// returning uses Enter Sector. Locked nodes render as entropy fog.
    /// </summary>
    internal sealed class ExploreScreen
    {
        private const float MapX = 28f;
        private const float MapY = 140f;
        private const float MapW = 1252f;
        private const float MapH = 640f;
        private const float MapPad = 28f;
        private const float SideX = 1296f;

        private readonly Transform root;
        private readonly System.Action openFormation;
        private readonly System.Action openShip;

        private string selectedBodyId = "body_outer_haven";
        private string selectedSectorId = WorldConstants.SectorId;
        private string lastNavMessage = "";
        private ExploreMapRig mapRig;

        private bool IsUniverseView => mapRig != null && mapRig.IsUniverseLod;

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

        public void OnMapLodChanged(bool universe)
        {
            RefreshMapContent();
            RebuildDockPanels();
        }

        public void Rebuild()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (child.name == ExploreMapRig.HostName)
                    continue;
                Object.DestroyImmediate(child);
            }

            if (DataUtil.Instance != null)
            {
                WorldService.EnsureLoaded(DataUtil.Instance);
                ShipService.EnsureLoaded(DataUtil.Instance);
                if (CardListManager.Instance != null)
                    DeckService.EnsureLoaded(DataUtil.Instance, CardListManager.Instance.cardEntities);
            }

            NavigationService.EnsureReady();
            GridService.EnsureReady();

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
            int gridPct = WorldService.State?.explorationProgress ?? 0;
            NexusUiFactory.CreateText(
                root, "NavStatus",
                UiText.ExploreNavStatus(NavigationService.ShipX, NavigationService.ShipY, radar)
                + "  ·  " + UiText.ExploreGridProgress(gridPct),
                new Vector2(640f, 110f), new Vector2(760f, 24f), 13f, NexusTheme.Cyan);

            if (WorldService.IsSectorComplete())
            {
                NexusUiFactory.CreateText(
                    root, "SectorComplete", UiText.SectorComplete,
                    new Vector2(1240f, 110f), new Vector2(420f, 24f), 13f, NexusTheme.Gold,
                    TextAlignmentOptions.Left, FontStyles.Bold);
            }

            EnsureMapHost();
            RefreshMapContent();
            BuildSidePanel();
        }

        private void RebuildDockPanels()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name == "Side")
                    Object.DestroyImmediate(child.gameObject);
            }

            BuildSidePanel();
        }

        private void EnsureMapHost()
        {
            Transform existing = root.Find(ExploreMapRig.HostName);
            if (existing != null)
            {
                mapRig = existing.GetComponent<ExploreMapRig>() ?? existing.GetComponentInChildren<ExploreMapRig>();
                if (mapRig != null)
                {
                    mapRig.Screen = this;
                    if (mapRig.ZoomLabel == null)
                    {
                        var label = existing.Find("ZoomLabel");
                        if (label != null)
                            mapRig.ZoomLabel = label.GetComponent<TextMeshProUGUI>();
                    }
                }
                EnsureMapBackdrop(existing);
                return;
            }

            GameObject host = NexusUiFactory.CreateBox(
                root, ExploreMapRig.HostName, new Vector2(MapX, MapY), new Vector2(MapW, MapH),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ExploreMapRig));
            viewportGo.transform.SetParent(host.transform, false);
            var viewport = viewportGo.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewport.pivot = new Vector2(0f, 1f);
            var vpImage = viewportGo.GetComponent<Image>();
            vpImage.color = Color.clear;
            vpImage.raycastTarget = true;

            var contentGo = new GameObject("MapContent", typeof(RectTransform));
            contentGo.transform.SetParent(viewport, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.sizeDelta = new Vector2(MapW, MapH);
            content.anchoredPosition = Vector2.zero;

            mapRig = viewportGo.GetComponent<ExploreMapRig>();
            mapRig.Screen = this;
            mapRig.Viewport = viewport;
            mapRig.Content = content;
            mapRig.Snap(1.15f, Vector2.zero);

            EnsureMapBackdrop(host.transform);
            BuildZoomControls(host.transform);
        }

        private void EnsureMapBackdrop(Transform host)
        {
            Transform viewport = host.Find("Viewport");
            if (viewport == null)
                return;
            if (viewport.Find("MapBackdrop") != null)
                return;

            var backdropGo = new GameObject("MapBackdrop", typeof(RectTransform), typeof(Image));
            backdropGo.transform.SetParent(viewport, false);
            backdropGo.transform.SetAsFirstSibling();
            var rect = backdropGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            var image = backdropGo.GetComponent<Image>();
            image.color = NexusTheme.Surface;
            image.raycastTarget = false;

            float ringW = MapW * 0.72f;
            float ringH = MapH * 0.72f;
            NexusUiFactory.CreateBox(
                backdropGo.transform, "ClusterRing",
                new Vector2((MapW - ringW) * 0.5f, (MapH - ringH) * 0.5f),
                new Vector2(ringW, ringH),
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.05f));

            var random = new System.Random(1900);
            for (int i = 0; i < 36; i++)
            {
                float x = 8f + (float)random.NextDouble() * (MapW - 16f);
                float y = 8f + (float)random.NextDouble() * (MapH - 16f);
                float size = random.Next(0, 3) == 0 ? 3f : 2f;
                NexusUiFactory.CreateBox(
                    backdropGo.transform,
                    "Star" + i,
                    new Vector2(x, y),
                    new Vector2(size, size),
                    NexusTheme.WithAlpha(NexusTheme.Text, 0.12f + (float)random.NextDouble() * 0.22f));
            }
        }

        private void RefreshMapContent()
        {
            if (mapRig?.Content == null)
                return;
            Transform content = mapRig.Content;
            for (int i = content.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(content.GetChild(i).gameObject);

            if (IsUniverseView)
                BuildUniverseMap(content);
            else
                BuildSectorMap(content);

            var overlayMsg = root.Find(ExploreMapRig.HostName)?.Find("NavMsg");
            if (overlayMsg != null)
                overlayMsg.GetComponent<TextMeshProUGUI>().text = lastNavMessage ?? "";
            var cruise = root.Find(ExploreMapRig.HostName)?.Find("CruiseRandom");
            if (cruise != null)
                cruise.gameObject.SetActive(!IsUniverseView);
            var layerLabel = root.Find(ExploreMapRig.HostName)?.Find("MapLabel");
            if (layerLabel != null)
                layerLabel.GetComponent<TextMeshProUGUI>().text =
                    IsUniverseView ? UiText.ExploreUniverseLabel : UiText.ExploreMapLabel;
        }

        private void BuildSectorMap(Transform map)
        {
            DrawInnerLanes(map);

            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null || !GridService.IsBodyOnMap(body.bodyId))
                    continue;
                BuildBodyMarker(map, body);
            }
        }

        private void BuildUniverseMap(Transform map)
        {
            foreach (var edge in UniverseMapCatalog.Edges)
            {
                if (edge == null) continue;
                if (!GridService.IsSectorOnMap(edge.a) || !GridService.IsSectorOnMap(edge.b))
                    continue;
                var a = UniverseMapCatalog.Get(edge.a);
                var b = UniverseMapCatalog.Get(edge.b);
                if (a == null || b == null) continue;
                var from = SectorToLocal(a.x, a.y);
                var to = SectorToLocal(b.x, b.y);
                bool fogLane = GridService.SectorShowsFog(edge.a) || GridService.SectorShowsFog(edge.b);
                Color color = fogLane
                    ? NexusTheme.WithAlpha(NexusTheme.Purple, 0.22f)
                    : LaneColor(edge, GridEdgeTier.Unknown);
                DrawLane(map, "ULane_" + edge.edgeId, from, to, color);
            }

            foreach (var sector in UniverseMapCatalog.Sectors)
            {
                if (sector == null || !GridService.IsSectorOnMap(sector.sectorId))
                    continue;
                BuildUniverseSectorMarker(map, sector);
            }
        }

        private void BuildUniverseSectorMarker(Transform map, SectorNodeDef sector)
        {
            var local = SectorToLocal(sector.x, sector.y);
            bool selected = sector.sectorId == selectedSectorId;
            bool fogged = GridService.SectorShowsFog(sector.sectorId);
            float size = selected ? 52f : fogged ? 36f : 46f;
            Color tint = fogged ? NexusTheme.Purple : NexusTheme.Cyan;
            if (sector.sectorId == WorldConstants.SectorId)
                tint = fogged ? NexusTheme.Purple : NexusTheme.Gold;

            var marker = NexusUiFactory.CreateButton(
                map, "Sector_" + sector.sectorId, "",
                new Vector2(local.x - size * 0.5f, local.y - size * 0.5f),
                new Vector2(size, size),
                () => SelectSector(sector.sectorId),
                NexusTheme.WithAlpha(tint, selected ? 0.35f : fogged ? 0.20f : 0.18f),
                tint, 1f);

            var label = marker.transform.Find("Label");
            if (label != null)
                Object.DestroyImmediate(label.gameObject);

            NexusUiFactory.CreateIcon(
                marker.transform, "Icon",
                NexusCardVisual.PlanetSprite(sector.spriteIndex),
                new Vector2(4f, 4f), new Vector2(size - 8f, size - 8f),
                fogged ? NexusTheme.WithAlpha(Color.white, 0.22f) : Color.white);

            string name = fogged
                ? UiText.ExploreFogName
                : UiText.T(sector.displayNameEn, sector.displayNameZh);
            NexusUiFactory.CreateText(
                map, "SName_" + sector.sectorId, name,
                new Vector2(local.x - 64f, local.y + size * 0.5f + 2f),
                new Vector2(128f, 16f), 10f,
                selected ? NexusTheme.Text : fogged ? NexusTheme.DimText : NexusTheme.MutedText,
                TextAlignmentOptions.Center, selected ? FontStyles.Bold : FontStyles.Normal);
        }

        private void BuildZoomControls(Transform map)
        {
            NexusUiFactory.CreateText(
                map, "MapLabel",
                IsUniverseView ? UiText.ExploreUniverseLabel : UiText.ExploreMapLabel,
                new Vector2(12f, 8f), new Vector2(300f, 22f), 12f, NexusTheme.MutedText,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                map, "Legend", UiText.ExploreLegend,
                new Vector2(12f, 32f), new Vector2(MapW - 220f, 18f), 10f, NexusTheme.DimText);
            NexusUiFactory.CreateButton(
                map, "ZoomOut", UiText.ExploreZoomOut,
                new Vector2(MapW - 196f, 6f), new Vector2(36f, 28f),
                ZoomOut,
                NexusTheme.SurfaceRaised, NexusTheme.Text, 16f);
            var zoomLabel = NexusUiFactory.CreateText(
                map, "ZoomLabel",
                UiText.ExploreZoomScale(mapRig != null ? mapRig.Zoom : 1.15f, IsUniverseView),
                new Vector2(MapW - 156f, 8f), new Vector2(110f, 24f), 11f, NexusTheme.Cyan,
                TextAlignmentOptions.Center, FontStyles.Bold);
            if (mapRig != null)
                mapRig.ZoomLabel = zoomLabel;
            NexusUiFactory.CreateButton(
                map, "ZoomIn", UiText.ExploreZoomIn,
                new Vector2(MapW - 42f, 6f), new Vector2(36f, 28f),
                ZoomIn,
                NexusTheme.SurfaceRaised, NexusTheme.Text, 16f);
            NexusUiFactory.CreateButton(
                map, "CruiseRandom", UiText.ExploreCruiseRandom,
                new Vector2(12f, MapH - 48f), new Vector2(170f, 36f),
                CruiseRandom,
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan, 11f);
            var msg = NexusUiFactory.CreateText(
                map, "NavMsg", lastNavMessage ?? "",
                new Vector2(192f, MapH - 44f), new Vector2(480f, 28f), 11f, NexusTheme.MutedText);
            msg.textWrappingMode = TextWrappingModes.Normal;
        }

        private void BuildBodyMarker(Transform map, StellarBodyDef body)
        {
            var local = SectorToLocal(body.x, body.y);
            bool selected = body.bodyId == selectedBodyId;
            bool docked = NavigationService.IsDocked(body);
            var gridState = GridService.GetState(body.bodyId);
            bool fogged = GridService.ShowsFog(body.bodyId);
            float size = selected ? 42f : fogged ? 28f : 34f;
            Color tint = fogged ? NexusTheme.Purple : BodyTint(body.bodyType);
            if (docked)
                tint = NexusTheme.Green;

            string regionId = PrimaryRegion(body);
            var view = string.IsNullOrEmpty(regionId) || fogged ? null : WorldService.GetRegionView(regionId);
            int spriteIndex = body.planetSpriteIndex;

            var marker = NexusUiFactory.CreateButton(
                map, "Body_" + body.bodyId, "",
                new Vector2(local.x - size * 0.5f, local.y - size * 0.5f),
                new Vector2(size, size),
                () => SelectBody(body.bodyId),
                NexusTheme.WithAlpha(tint, selected ? 0.35f : fogged ? 0.22f : 0.18f),
                tint, 1f);

            var label = marker.transform.Find("Label");
            if (label != null)
                Object.DestroyImmediate(label.gameObject);

            NexusUiFactory.CreateIcon(
                marker.transform, "Icon",
                NexusCardVisual.PlanetSprite(spriteIndex),
                new Vector2(4f, 4f), new Vector2(size - 8f, size - 8f),
                fogged ? NexusTheme.WithAlpha(Color.white, 0.22f) : Color.white);

            string name = fogged
                ? "???"
                : UiText.T(body.displayNameEn, body.displayNameZh);
            if (!fogged && view?.Config != null && view.Config.bossRegion)
                name = "★ " + name;

            NexusUiFactory.CreateText(
                map, "Name_" + body.bodyId, name,
                new Vector2(local.x - 54f, local.y + size * 0.5f + 2f),
                new Vector2(108f, 16f), 10f,
                selected ? NexusTheme.Text : fogged ? NexusTheme.DimText : NexusTheme.MutedText,
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

            if (IsUniverseView)
            {
                var sector = UniverseMapCatalog.Get(selectedSectorId);
                if (sector == null)
                {
                    var fallback = NexusUiFactory.CreateText(
                        side.transform, "SideBody", UiText.ExploreSideBody,
                        new Vector2(20f, 56f), new Vector2(420f, 200f), 13f, NexusTheme.MutedText);
                    fallback.textWrappingMode = TextWrappingModes.Normal;
                }
                else
                {
                    BuildUniverseSectorDetail(side.transform, sector);
                }
            }
            else
            {
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
            var gridState = GridService.GetState(body.bodyId);
            bool fogged = GridService.ShowsFog(body.bodyId);
            string name = fogged
                ? UiText.ExploreFogName
                : UiText.T(body.displayNameEn, body.displayNameZh);
            NexusUiFactory.CreateText(
                side, "BodyName", name,
                new Vector2(20f, 52f), new Vector2(420f, 28f), 16f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            string regionId = PrimaryRegion(body);
            var view = string.IsNullOrEmpty(regionId) || fogged ? null : WorldService.GetRegionView(regionId);
            float dist = NavigationService.DistanceToBody(body);
            bool docked = NavigationService.IsDocked(body);
            var cost = GridService.EstimateCruise(body);
            float eta = NavigationService.EstimateCruiseSeconds(dist, body);

            string discovery = ExploreDiscoveryLabel(gridState);
            string lane = ExploreLaneLabel(body.bodyId);
            string dev = ExploreDevLabel(view);
            string fleet = docked ? UiText.ExploreDocked : UiText.ExploreSailEta(dist, eta);
            NexusUiFactory.CreateText(
                side, "AxisDiscovery", UiText.ExploreStatusDiscovery(discovery),
                new Vector2(20f, 80f), new Vector2(420f, 18f), 11f, NexusTheme.Cyan);
            NexusUiFactory.CreateText(
                side, "AxisLane", UiText.ExploreStatusLane(lane),
                new Vector2(20f, 100f), new Vector2(420f, 18f), 11f, NexusTheme.MutedText);
            NexusUiFactory.CreateText(
                side, "AxisDev", UiText.ExploreStatusDev(dev),
                new Vector2(20f, 120f), new Vector2(420f, 18f), 11f, NexusTheme.MutedText);
            var fleetLine = NexusUiFactory.CreateText(
                side, "AxisFleet", UiText.ExploreStatusFleet(fleet),
                new Vector2(20f, 140f), new Vector2(420f, 36f), 11f,
                docked ? NexusTheme.Green : NexusTheme.MutedText);
            fleetLine.textWrappingMode = TextWrappingModes.Normal;

            float y = 182f;
            if (!fogged && cost.Overleveled)
            {
                int extra = Mathf.RoundToInt((cost.Multiplier - 1f) * 100f);
                var gate = NexusUiFactory.CreateText(
                    side, "SoftGate", UiText.ExploreSoftGate(cost.RecommendedLv, extra),
                    new Vector2(20f, y), new Vector2(420f, 36f), 11f, NexusTheme.Gold);
                gate.textWrappingMode = TextWrappingModes.Normal;
                y += 38f;
            }
            else if (!fogged)
            {
                NexusUiFactory.CreateText(
                    side, "CruiseCost", UiText.ExploreCruiseCost(cost.Multiplier, cost.AutoReturn),
                    new Vector2(20f, y), new Vector2(420f, 20f), 11f, NexusTheme.MutedText);
                y += 24f;
            }

            if (view?.Config != null)
            {
                var blurb = UiText.T(view.Config.blurbEn, view.Config.blurbZh);
                if (!string.IsNullOrEmpty(blurb))
                {
                    var blurbText = NexusUiFactory.CreateText(
                        side, "Blurb", blurb,
                        new Vector2(20f, y), new Vector2(420f, 40f), 12f, NexusTheme.Cyan);
                    blurbText.textWrappingMode = TextWrappingModes.Normal;
                    y += 42f;
                }

                var meta = NexusUiFactory.CreateText(
                    side, "Meta", BuildMeta(view),
                    new Vector2(20f, y), new Vector2(420f, 48f), 12f, NexusTheme.MutedText);
                meta.textWrappingMode = TextWrappingModes.Normal;
                y += 52f;
            }
            else if (fogged)
            {
                var help = NexusUiFactory.CreateText(
                    side, "FogHelp", UiText.ExploreChartedHint,
                    new Vector2(20f, y), new Vector2(420f, 40f), 12f, NexusTheme.MutedText);
                help.textWrappingMode = TextWrappingModes.Normal;
                y += 44f;
            }

            float actionY = Mathf.Max(y, 248f);
            if (fogged)
            {
                NexusUiFactory.CreateButton(
                    side, "Probe", UiText.ExploreProbe,
                    new Vector2(20f, actionY), new Vector2(420f, 44f),
                    () => ProbeBody(body.bodyId),
                    NexusTheme.WithAlpha(NexusTheme.Purple, 0.22f), NexusTheme.Purple, 14f);
                return;
            }

            if (!docked)
            {
                NexusUiFactory.CreateButton(
                    side, "Sail", UiText.ExploreSailHere,
                    new Vector2(20f, actionY), new Vector2(420f, 44f),
                    () => CruiseToBody(body.bodyId),
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.2f), NexusTheme.Gold, 14f);
                return;
            }

            if (GridService.CanStabilize(body.bodyId))
            {
                NexusUiFactory.CreateButton(
                    side, "Stabilize", UiText.ExploreStabilize,
                    new Vector2(20f, actionY), new Vector2(420f, 40f),
                    () => StabilizeBody(body.bodyId),
                    NexusTheme.WithAlpha(NexusTheme.Cyan, 0.2f), NexusTheme.Cyan, 13f);
                actionY += 46f;
            }

            if (GridService.CanBuyChart())
            {
                NexusUiFactory.CreateButton(
                    side, "BuyChart", UiText.ExploreBuyChart(WorldConstants.ChartCreditCost),
                    new Vector2(20f, actionY), new Vector2(420f, 40f),
                    BuyChart,
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f), NexusTheme.Gold, 13f);
                actionY += 46f;
            }

            if (view == null || view.Config == null)
                return;

            if (view.CanEnter)
            {
                NexusUiFactory.CreateButton(
                    side, "Start", UiText.StartAutoBattle,
                    new Vector2(20f, actionY), new Vector2(420f, 44f),
                    () => StartBattle(regionId),
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 14f);
            }
            else
            {
                NexusUiFactory.CreateText(
                    side, "Locked", UiText.RegionLocked,
                    new Vector2(20f, actionY + 6f), new Vector2(420f, 32f), 13f, NexusTheme.DimText,
                    TextAlignmentOptions.Center, FontStyles.Bold);
            }

            actionY += 50f;
            if (view.FarmUnlocked)
            {
                NexusUiFactory.CreateButton(
                    side, "Farm", UiText.StartFarm,
                    new Vector2(20f, actionY), new Vector2(200f, 40f),
                    () => StartFarm(regionId),
                    NexusTheme.WithAlpha(NexusTheme.Cyan, 0.18f), NexusTheme.Cyan, 13f);

                var nodes = Economy.Domain.GatherNodeCatalog.ForRegion(regionId);
                if (nodes.Count > 0)
                {
                    var nodeId = nodes[0].nodeId;
                    NexusUiFactory.CreateButton(
                        side, "Gather", UiText.StartGather,
                        new Vector2(240f, actionY), new Vector2(200f, 40f),
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

        private void BuildUniverseSectorDetail(Transform side, SectorNodeDef sector)
        {
            bool fogged = GridService.SectorShowsFog(sector.sectorId);
            var state = GridService.GetSectorState(sector.sectorId);
            string name = fogged
                ? UiText.ExploreFogName
                : UiText.T(sector.displayNameEn, sector.displayNameZh);

            NexusUiFactory.CreateText(
                side, "BodyName", name,
                new Vector2(20f, 52f), new Vector2(420f, 28f), 16f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            string meta = UiText.GridStateLabel(state);
            if (!fogged)
                meta += " · " + UiText.T(sector.ringEn, sector.ringZh);
            NexusUiFactory.CreateText(
                side, "GridState", meta,
                new Vector2(20f, 84f), new Vector2(420f, 22f), 12f, NexusTheme.Cyan);

            var help = NexusUiFactory.CreateText(
                side, "Help",
                fogged
                    ? UiText.ExploreFogName + "\n" + UiText.ExploreSectorListHint
                    : sector.playable
                        ? UiText.ExploreSideBody
                        : UiText.ExploreSectorSealed,
                new Vector2(20f, 114f), new Vector2(420f, 120f), 12f, NexusTheme.MutedText);
            help.textWrappingMode = TextWrappingModes.Normal;

            if (!fogged && !sector.playable)
            {
                NexusUiFactory.CreateText(
                    side, "SoftGate",
                    UiText.ExploreSoftGate(sector.recommendedExpeditionLv, 65),
                    new Vector2(20f, 244f), new Vector2(420f, 40f), 11f, NexusTheme.Gold);
            }

            if (GridService.CanEnterSector(sector.sectorId))
            {
                NexusUiFactory.CreateButton(
                    side, "EnterSector", UiText.ExploreEnterSector,
                    new Vector2(20f, 300f), new Vector2(420f, 44f),
                    () => EnterSector(sector.sectorId),
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.2f), NexusTheme.Gold, 14f);
            }
        }

        private void ZoomOut()
        {
            mapRig?.NudgeZoomCentered(1f / ExploreMapRig.ZoomStep);
        }

        private void ZoomIn()
        {
            mapRig?.NudgeZoomCentered(ExploreMapRig.ZoomStep);
        }

        private void SelectSector(string sectorId)
        {
            selectedSectorId = sectorId ?? WorldConstants.SectorId;
            Rebuild();
        }

        private void EnterSector(string sectorId)
        {
            if (!GridService.CanEnterSector(sectorId))
            {
                lastNavMessage = UiText.ExploreSectorSealed;
                Rebuild();
                return;
            }

            selectedSectorId = sectorId;
            if (WorldService.State != null)
                WorldService.State.currentSectorId = sectorId;
            lastNavMessage = "";
            mapRig?.OpenSectorView(ExploreMapRig.SectorEnterZoom, Vector2.zero);
            Rebuild();
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
            if (result.Success)
            {
                BridgeEventLog.Push(
                    BridgeLogCategory.Explore,
                    "Cruise complete. Inner-grid lane marked provisional.",
                    "巡航抵达。域内航道记为临时通航。");
            }
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

        private void ProbeBody(string bodyId)
        {
            var result = GridService.TryProbe(bodyId);
            lastNavMessage = string.IsNullOrEmpty(result.Message)
                ? UiText.ExploreCruiseFailed
                : result.Message;
            if (result.Success)
            {
                selectedBodyId = bodyId;
                BridgeEventLog.Push(
                    BridgeLogCategory.Explore,
                    "Probe locked a fogged node on the inner grid.",
                    "探测锁定了域内航网上一处熵雾节点。");
            }

            Rebuild();
        }

        private void StabilizeBody(string bodyId)
        {
            var result = GridService.TryStabilize(bodyId);
            lastNavMessage = string.IsNullOrEmpty(result.Message)
                ? UiText.ExploreCruiseFailed
                : result.Message;
            if (result.Success)
            {
                BridgeEventLog.Push(
                    BridgeLogCategory.Explore,
                    "Beacon repaired. A lane is now stable.",
                    "航标已修复。一段航线进入稳定通航。");
            }

            Rebuild();
        }

        private void BuyChart()
        {
            var result = GridService.TryBuyChart();
            lastNavMessage = string.IsNullOrEmpty(result.Message)
                ? UiText.ExploreCruiseFailed
                : result.Message;
            if (result.Success)
            {
                BridgeEventLog.Push(
                    BridgeLogCategory.Explore,
                    "Sector chart purchased. Silhouettes persist; still sail to dock.",
                    "已购入星域海图。轮廓常亮，仍须航行才能停靠。");
            }

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

        private static string ExploreDiscoveryLabel(GridNodeState state) => state switch
        {
            GridNodeState.Unobserved or GridNodeState.Fogged => UiText.GridStateLabel(GridNodeState.Fogged),
            GridNodeState.Located or GridNodeState.Provisional => UiText.GridStateLabel(GridNodeState.Located),
            _ => UiText.GridStateLabel(GridNodeState.Stable)
        };

        private static string ExploreLaneLabel(string bodyId)
        {
            GridEdgeTier best = GridEdgeTier.Unknown;
            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                if (edge.a != bodyId && edge.b != bodyId) continue;
                var t = GridService.GetEdgeTier(edge.edgeId);
                if (t > best) best = t;
            }

            return best switch
            {
                GridEdgeTier.Stable => UiText.GridStateLabel(GridNodeState.Stable),
                GridEdgeTier.Provisional => UiText.GridStateLabel(GridNodeState.Provisional),
                _ => UiText.RegionLocked
            };
        }

        private static string ExploreDevLabel(RegionView view)
        {
            if (view == null) return UiText.RegionLocked;
            return view.Progress switch
            {
                RegionProgressState.Cleared or RegionProgressState.BossDefeated => UiText.RegionProgressLabel("Cleared"),
                _ => UiText.RegionProgressLabel("Challengeable")
            };
        }

        private static Color BodyTint(StellarBodyType type) => type switch
        {
            StellarBodyType.Station => NexusTheme.Cyan,
            StellarBodyType.Anomaly => NexusTheme.Gold,
            StellarBodyType.Hub => NexusTheme.Gold,
            StellarBodyType.Beacon => NexusTheme.Purple,
            _ => NexusTheme.Text
        };

        private static void DrawInnerLanes(Transform map)
        {
            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                var a = SectorMapCatalog.Get(edge.a);
                var b = SectorMapCatalog.Get(edge.b);
                if (a == null || b == null) continue;
                if (!GridService.IsBodyOnMap(edge.a) || !GridService.IsBodyOnMap(edge.b))
                    continue;

                var from = SectorToLocal(a.x, a.y);
                var to = SectorToLocal(b.x, b.y);
                var tier = GridService.GetEdgeTier(edge.edgeId);
                Color color = LaneColor(edge, tier);
                DrawLane(map, "Lane_" + edge.edgeId, from, to, color);
            }
        }

        private static Color LaneColor(InnerEdgeDef edge, GridEdgeTier tier)
        {
            if (tier >= GridEdgeTier.Stable)
                return NexusTheme.WithAlpha(NexusTheme.Cyan, 0.55f);
            if (tier == GridEdgeTier.Provisional)
                return NexusTheme.WithAlpha(NexusTheme.Gold, 0.45f);
            if (edge.rift)
                return NexusTheme.WithAlpha(NexusTheme.Purple, 0.32f);
            return NexusTheme.WithAlpha(NexusTheme.MutedText, 0.28f);
        }

        private static void DrawLane(Transform map, string name, Vector2 from, Vector2 to, Color color)
        {
            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 4f) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(map, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float midX = (from.x + to.x) * 0.5f;
            float midY = (from.y + to.y) * 0.5f;
            rect.anchoredPosition = new Vector2(midX, -midY);
            rect.sizeDelta = new Vector2(len, 3f);
            float visualDy = from.y - to.y;
            rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(visualDy, dx) * Mathf.Rad2Deg);
            go.GetComponent<UnityEngine.UI.Image>().color = color;
            go.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            go.transform.SetAsFirstSibling();
        }

        private static string PrimaryRegion(StellarBodyDef body)
        {
            if (body?.regionIds == null || body.regionIds.Length == 0)
                return "";
            return body.regionIds[0] ?? "";
        }
    }
}

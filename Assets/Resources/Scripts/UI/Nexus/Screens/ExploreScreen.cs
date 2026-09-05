using System.Text;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Progression;
using Assets.Resources.Scripts.Progression.Domain;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.UI.Nexus.Tutorial;
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
        private readonly System.Action openMarket;

        private string selectedBodyId = "body_outer_haven";
        private string selectedSectorId = WorldConstants.SectorId;
        private ExploreMapRig mapRig;

        private bool IsUniverseView => mapRig != null && mapRig.IsUniverseLod;

        private ExploreScreen(
            Transform root, System.Action openFormation, System.Action openShip, System.Action openMarket)
        {
            this.root = root;
            this.openFormation = openFormation;
            this.openShip = openShip;
            this.openMarket = openMarket;
        }

        public GameObject Root => root.gameObject;

        public static ExploreScreen Build(
            Transform parent,
            System.Action openFormation,
            System.Action openShip = null,
            System.Action openMarket = null)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent,
                "Explore Screen",
                NexusTheme.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            var screen = new ExploreScreen(panel.transform, openFormation, openShip, openMarket);
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
            TutorialGuideService.UnregisterAnchor("explore_start");
            TutorialGuideService.UnregisterAnchor("explore_gather");
            TutorialGuideService.UnregisterAnchor("explore_probe");
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

            int gridPct = WorldService.State?.explorationProgress ?? 0;
            NexusUiFactory.CreateText(
                root, "GridProgress",
                UiText.ExploreGridProgress(gridPct),
                new Vector2(640f, 110f), new Vector2(420f, 24f), 13f, NexusTheme.Cyan);

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

            // Points of interest (shops / NPCs / events / wormholes) hang off the same lane network.
            // They are always charted (no fog / probe flow) and never affect grid scoring.
            foreach (var poi in SectorMapCatalog.PointsOfInterest)
            {
                if (poi == null || !IsNodeDisplayable(poi.bodyId))
                    continue;
                BuildBodyMarker(map, poi);
            }
        }

        /// <summary>A grid body once it is on the map, or any charted point of interest.</summary>
        private static bool IsNodeDisplayable(string bodyId)
        {
            if (SectorMapCatalog.IsPointOfInterest(bodyId))
                return true;
            return GridService.IsBodyOnMap(bodyId);
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
        }

        private void BuildBodyMarker(Transform map, StellarBodyDef body)
        {
            var local = SectorToLocal(body.x, body.y);
            bool selected = body.bodyId == selectedBodyId;
            string regionId = PrimaryRegion(body);
            // Points of interest carry no Region — they are always charted (never fogged).
            bool poi = string.IsNullOrEmpty(regionId);
            bool fogged = !poi && GridService.ShowsFog(body.bodyId);
            bool docked = NavigationService.IsDocked(body);
            float size = selected ? 42f : fogged ? 28f : 34f;

            var view = poi || fogged ? null : WorldService.GetRegionView(regionId);
            bool reachable = !fogged && (view == null || view.CanEnter || GridService.IsLocatedOrBetter(body.bodyId));
            bool cleared = view != null &&
                (view.Progress == RegionProgressState.Cleared || view.Progress == RegionProgressState.BossDefeated);

            Color border;
            if (fogged)
                border = NexusTheme.WithAlpha(NexusTheme.Purple, 0.85f);
            else if (poi)
                border = selected || docked ? NexusTheme.Green : NodeAccent(body.bodyType);
            else if (selected || docked || cleared)
                border = NexusTheme.Green;
            else if (reachable)
                border = NexusTheme.Cyan;
            else
                border = NexusTheme.BorderSoft;

            Color fill = fogged
                ? NexusTheme.WithAlpha(NexusTheme.Purple, selected ? 0.28f : 0.16f)
                : NexusTheme.WithAlpha(border, selected ? 0.32f : 0.14f);

            var marker = NexusUiFactory.CreateButton(
                map, "Body_" + body.bodyId, "",
                new Vector2(local.x - size * 0.5f, local.y - size * 0.5f),
                new Vector2(size, size),
                () => SelectBody(body.bodyId),
                fill,
                border, 1f);

            var outline = marker.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = border;
                outline.effectDistance = selected || docked
                    ? new Vector2(2.5f, -2.5f)
                    : new Vector2(1.5f, -1.5f);
            }

            var label = marker.transform.Find("Label");
            if (label != null)
                Object.DestroyImmediate(label.gameObject);

            if (fogged)
            {
                NexusUiFactory.CreateText(
                    marker.transform, "Signal", "???",
                    new Vector2(0f, 0f), new Vector2(size, size),
                    Mathf.Max(10f, size * 0.35f),
                    NexusTheme.WithAlpha(NexusTheme.Text, 0.55f),
                    TextAlignmentOptions.Center, FontStyles.Bold);
            }
            else
            {
                NexusUiFactory.CreateIcon(
                    marker.transform, "Icon",
                    NodeIcon(body),
                    new Vector2(4f, 4f), new Vector2(size - 8f, size - 8f),
                    poi ? NodeAccent(body.bodyType) : Color.white);
            }

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

        private static Sprite NodeIcon(StellarBodyDef body)
        {
            switch (body.bodyType)
            {
                case StellarBodyType.Station: return NexusCardVisual.UiIcon("Building");
                case StellarBodyType.Hub: return NexusCardVisual.UiIcon("World");
                case StellarBodyType.Beacon: return NexusCardVisual.UiIcon("Bell");
                case StellarBodyType.Shop: return NexusCardVisual.UiIcon("Shop");
                case StellarBodyType.Npc: return NexusCardVisual.UiIcon("Character");
                case StellarBodyType.Exit: return NexusCardVisual.UiIcon("World");
                case StellarBodyType.Anomaly: return NexusCardVisual.EventSprite(1);
                case StellarBodyType.Event: return NexusCardVisual.EventSprite(0);
                case StellarBodyType.Belt: return NexusCardVisual.EventSprite(2);
                case StellarBodyType.Relic: return NexusCardVisual.EventSprite(3);
                case StellarBodyType.Wormhole: return NexusCardVisual.EventSprite(4);
                default: return NexusCardVisual.PlanetSprite(body.planetSpriteIndex);
            }
        }

        private static Color NodeAccent(StellarBodyType type) => type switch
        {
            StellarBodyType.Shop => NexusTheme.Gold,
            StellarBodyType.Npc => NexusTheme.Green,
            StellarBodyType.Event => NexusTheme.Gold,
            StellarBodyType.Belt => NexusTheme.Cyan,
            StellarBodyType.Relic => NexusTheme.Purple,
            StellarBodyType.Wormhole => NexusTheme.Purple,
            StellarBodyType.Exit => NexusTheme.Cyan,
            _ => NexusTheme.Cyan
        };

        private static string NodeTypeLabel(StellarBodyType type) => type switch
        {
            StellarBodyType.Planet => UiText.T("Planet", "行星"),
            StellarBodyType.Station => UiText.T("Station", "空间站"),
            StellarBodyType.Anomaly => UiText.T("Anomaly", "熵雾异常"),
            StellarBodyType.Hub => UiText.T("Capital Hub", "星域主星"),
            StellarBodyType.Beacon => UiText.T("Beacon", "星域航标"),
            StellarBodyType.Belt => UiText.T("Resource Belt", "资源带"),
            StellarBodyType.Relic => UiText.T("Relic", "遗迹"),
            StellarBodyType.Exit => UiText.T("Sector Exit", "跨星域出口"),
            StellarBodyType.Shop => UiText.T("Faction Shop", "阵营商店"),
            StellarBodyType.Npc => UiText.T("Wandering NPC", "神秘NPC"),
            StellarBodyType.Event => UiText.T("Special Event", "特殊事件"),
            StellarBodyType.Wormhole => UiText.T("Wormhole", "虫洞"),
            _ => UiText.T("Point of Interest", "兴趣点")
        };

        private static string PoiDescription(StellarBodyType type) => type switch
        {
            StellarBodyType.Shop => UiText.T(
                "A faction trade post along the lane. Restock modules and materials.",
                "航道旁的阵营贸易站，可补给模块与材料。"),
            StellarBodyType.Npc => UiText.T(
                "A wandering merchant selling sector charts that reveal the whole sector.",
                "一位贩卖星域海图的流浪商人，购入后可揭示整个星域的节点。"),
            StellarBodyType.Event => UiText.T(
                "An anomalous signal — a special event may be unfolding here.",
                "异常信号——这里可能正在发生特殊事件。"),
            StellarBodyType.Wormhole => UiText.T(
                "An unstable wormhole. Charting it rewrites the lane network.",
                "一处不稳定虫洞，标定后会改写航路网。"),
            StellarBodyType.Exit => UiText.T(
                "A cross-sector exit lane leading beyond this sector.",
                "一条通往星域之外的跨域出口。"),
            StellarBodyType.Relic => UiText.T(
                "Derelict relic structures drifting in the dark.",
                "漂浮在黑暗中的遗迹残骸。"),
            StellarBodyType.Belt => UiText.T(
                "A resource belt rich in salvage.",
                "富含可打捞资源的资源带。"),
            _ => UiText.T(
                "A charted point of interest on the sector map.",
                "星图上已标定的兴趣点。")
        };

        private void BuildSidePanel()
        {
            GameObject side = NexusUiFactory.CreateBox(
                root, "Side", new Vector2(SideX, MapY), new Vector2(460f, MapH),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            NexusUiFactory.CreateText(
                side.transform, "SideTitle", UiText.ExploreSideTitle,
                new Vector2(20f, 16f), new Vector2(420f, 28f), 15f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            // Build the dynamic node/sector detail first and measure where it ends, so the fixed
            // secondary widgets (Formation / Ship / gather bank) can flow below it instead of being
            // pinned at a fixed Y where tall detail content would overlap and hide them.
            float contentBottom;
            if (IsUniverseView)
            {
                var sector = UniverseMapCatalog.Get(selectedSectorId);
                if (sector == null)
                {
                    var fallback = NexusUiFactory.CreateText(
                        side.transform, "SideBody", UiText.ExploreSideBody,
                        new Vector2(20f, 56f), new Vector2(420f, 200f), 13f, NexusTheme.MutedText);
                    fallback.textWrappingMode = TextWrappingModes.Normal;
                    contentBottom = 260f;
                }
                else
                {
                    contentBottom = BuildUniverseSectorDetail(side.transform, sector);
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
                    contentBottom = 260f;
                }
                else
                {
                    contentBottom = BuildBodyDetail(side.transform, body);
                }
            }

            // Keep a stable baseline for short content, but push the fixed block down when the
            // detail (soft-gate + voyage + actions) is tall — this is what previously clipped the
            // gather button behind the Formation button.
            float secY = Mathf.Max(contentBottom + 16f, 452f);
            NexusUiFactory.CreateButton(
                side.transform, "Formation", UiText.BridgeOpenFormation,
                new Vector2(20f, secY), new Vector2(420f, 40f),
                () => openFormation?.Invoke(),
                NexusTheme.SurfaceRaised, NexusTheme.Text, 13f);
            secY += 48f;
            NexusUiFactory.CreateButton(
                side.transform, "Ship", UiText.OpenShipBay,
                new Vector2(20f, secY), new Vector2(420f, 40f),
                () => openShip?.Invoke(),
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan, 13f);
            secY += 52f;

            BuildGatherBank(side.transform, secY);
        }

        private float BuildBodyDetail(Transform side, StellarBodyDef body)
        {
            // Non-combat points of interest get their own compact detail (no grid / region flow).
            if (string.IsNullOrEmpty(PrimaryRegion(body)))
                return BuildPoiDetail(side, body);

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
            ShipService.EnsureReady();
            int recLv = body.recommendedExpeditionLv;
            int shipLv = Mathf.Max(1, ShipService.State?.level ?? 1);
            bool softGate = shipLv < recLv;

            string discovery = ExploreDiscoveryLabel(gridState);
            string lane = ExploreLaneLabel(body.bodyId);
            string dev = ExploreDevLabel(view);
            NexusUiFactory.CreateText(
                side, "AxisDiscovery", UiText.ExploreStatusDiscovery(discovery),
                new Vector2(20f, 80f), new Vector2(420f, 18f), 11f, NexusTheme.Cyan);
            NexusUiFactory.CreateText(
                side, "AxisLane", UiText.ExploreStatusLane(lane),
                new Vector2(20f, 100f), new Vector2(420f, 18f), 11f, NexusTheme.MutedText);
            NexusUiFactory.CreateText(
                side, "AxisDev", UiText.ExploreStatusDev(dev),
                new Vector2(20f, 120f), new Vector2(420f, 18f), 11f, NexusTheme.MutedText);

            float y = 148f;
            if (!fogged && softGate)
            {
                int extra = (recLv - shipLv) * 15;
                var gate = NexusUiFactory.CreateText(
                    side, "SoftGate", UiText.ExploreSoftGate(recLv, extra),
                    new Vector2(20f, y), new Vector2(420f, 36f), 11f, NexusTheme.Gold);
                gate.textWrappingMode = TextWrappingModes.Normal;
                y += 38f;
            }

            y = DrawVoyageStub(side, body, view, y);

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

            float actionY = Mathf.Max(y, 220f);
            ChapterQuestService.EnsureLoaded(DataUtil.Instance);
            if (!ChapterQuestService.IsChapterComplete
                && ChapterQuestService.ActiveStep?.stepId == ChapterQuestCatalog.StepScanSignal
                && body.bodyId == "body_mining_spur"
                && fogged)
            {
                var scanHint = NexusUiFactory.CreateText(
                    side, "ChapterScan", UiText.ChapterScanHint,
                    new Vector2(20f, actionY), new Vector2(420f, 36f), 12f, NexusTheme.Cyan);
                scanHint.textWrappingMode = TextWrappingModes.Normal;
                actionY += 40f;
            }

            if (fogged)
            {
                var probe = NexusUiFactory.CreateButton(
                    side, "Probe", UiText.ExploreProbe,
                    new Vector2(20f, actionY), new Vector2(420f, 44f),
                    () => ProbeBody(body.bodyId),
                    NexusTheme.WithAlpha(NexusTheme.Purple, 0.22f), NexusTheme.Purple, 14f);
                TutorialGuideService.RegisterAnchor(
                    "explore_probe",
                    probe.GetComponent<RectTransform>(),
                    probe);
                return actionY + 48f;
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

            if (view == null || view.Config == null)
                return actionY;

            bool cleared = view.Progress == RegionProgressState.Cleared
                || view.Progress == RegionProgressState.BossDefeated;
            var nodes = Economy.Domain.GatherNodeCatalog.ForRegion(regionId);
            bool hasGather = nodes.Count > 0;

            if (!view.CanEnter)
            {
                // Progress==Locked means the spatial gate blocks it (no cleared neighbour);
                // otherwise the ship hard-gate is the blocker.
                string lockMsg = view.Progress == RegionProgressState.Locked
                    ? UiText.T(
                        "Clear an adjacent node to open this lane.",
                        "需先通关相邻节点以开启此航路。")
                    : UiText.RegionLocked;
                NexusUiFactory.CreateText(
                    side, "Locked", lockMsg,
                    new Vector2(20f, actionY + 6f), new Vector2(420f, 40f), 13f, NexusTheme.DimText,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                return actionY + 48f;
            }

            if (cleared)
            {
                if (view.FarmUnlocked)
                {
                    NexusUiFactory.CreateButton(
                        side, "Farm", UiText.StartFarm,
                        new Vector2(20f, actionY), new Vector2(420f, 44f),
                        () => StartFarm(regionId),
                        NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 14f);
                    actionY += 50f;
                }
                else if (hasGather)
                {
                    var nodeId = nodes[0].nodeId;
                    var gather = NexusUiFactory.CreateButton(
                        side, "Gather", UiText.StartGather,
                        new Vector2(20f, actionY), new Vector2(420f, 44f),
                        () => StartGather(nodeId),
                        NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 14f);
                    TutorialGuideService.RegisterAnchor(
                        "explore_gather",
                        gather.GetComponent<RectTransform>(),
                        gather);
                    actionY += 50f;
                    var best = ProgressionService.BestProfessionLevel(
                        CardListManager.Instance?.cardEntities, nodes[0].requiredProfession);
                    NexusUiFactory.CreateText(
                        side, "GatherReq",
                        UiText.GatherReqLine(
                            nodes[0].requiredProfession.ToString(),
                            nodes[0].requiredSkillLevel,
                            best),
                        new Vector2(20f, actionY), new Vector2(420f, 18f), 11f, NexusTheme.MutedText);
                    actionY += 24f;
                }

                NexusUiFactory.CreateButton(
                    side, "Rechallenge", UiText.ExploreRechallenge,
                    new Vector2(20f, actionY), new Vector2(420f, 40f),
                    () => StartBattle(regionId),
                    NexusTheme.SurfaceRaised, NexusTheme.MutedText, 13f);
                actionY += 46f;

                if (view.FarmUnlocked && hasGather)
                {
                    var nodeId = nodes[0].nodeId;
                    var gather = NexusUiFactory.CreateButton(
                        side, "Gather", UiText.StartGather,
                        new Vector2(20f, actionY), new Vector2(200f, 40f),
                        () => StartGather(nodeId),
                        NexusTheme.WithAlpha(NexusTheme.Green, 0.18f), NexusTheme.Green, 13f);
                    TutorialGuideService.RegisterAnchor(
                        "explore_gather",
                        gather.GetComponent<RectTransform>(),
                        gather);
                    var best = ProgressionService.BestProfessionLevel(
                        CardListManager.Instance?.cardEntities, nodes[0].requiredProfession);
                    NexusUiFactory.CreateText(
                        side, "GatherReq",
                        UiText.GatherReqLine(
                            nodes[0].requiredProfession.ToString(),
                            nodes[0].requiredSkillLevel,
                            best),
                        new Vector2(230f, actionY + 10f), new Vector2(210f, 18f), 11f, NexusTheme.MutedText);
                }
            }
            else
            {
                var start = NexusUiFactory.CreateButton(
                    side, "Start", UiText.StartAutoBattle,
                    new Vector2(20f, actionY), new Vector2(420f, 44f),
                    () => StartBattle(regionId),
                    NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 14f);
                TutorialGuideService.RegisterAnchor(
                    "explore_start",
                    start.GetComponent<RectTransform>(),
                    start);
                actionY += 50f;

                if (view.FarmUnlocked)
                {
                    NexusUiFactory.CreateButton(
                        side, "Farm", UiText.StartFarm,
                        new Vector2(20f, actionY), new Vector2(200f, 40f),
                        () => StartFarm(regionId),
                        NexusTheme.WithAlpha(NexusTheme.Cyan, 0.18f), NexusTheme.Cyan, 13f);
                }

                if (hasGather)
                {
                    var nodeId = nodes[0].nodeId;
                    float gx = view.FarmUnlocked ? 240f : 20f;
                    var gather = NexusUiFactory.CreateButton(
                        side, "Gather", UiText.StartGather,
                        new Vector2(gx, actionY), new Vector2(200f, 40f),
                        () => StartGather(nodeId),
                        NexusTheme.WithAlpha(NexusTheme.Green, 0.18f), NexusTheme.Green, 13f);
                    TutorialGuideService.RegisterAnchor(
                        "explore_gather",
                        gather.GetComponent<RectTransform>(),
                        gather);
                    var best = ProgressionService.BestProfessionLevel(
                        CardListManager.Instance?.cardEntities, nodes[0].requiredProfession);
                    NexusUiFactory.CreateText(
                        side, "GatherReq",
                        UiText.GatherReqLine(
                            nodes[0].requiredProfession.ToString(),
                            nodes[0].requiredSkillLevel,
                            best),
                        new Vector2(20f, actionY + 42f), new Vector2(420f, 18f), 11f, NexusTheme.MutedText);
                }
            }

            // Pad past the tallest trailing row (gather button + requirement line) so the fixed
            // secondary widgets flow below without clipping it.
            return actionY + 64f;
        }

        private float BuildPoiDetail(Transform side, StellarBodyDef body)
        {
            NexusUiFactory.CreateText(
                side, "BodyName", UiText.T(body.displayNameEn, body.displayNameZh),
                new Vector2(20f, 52f), new Vector2(420f, 28f), 16f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                side, "PoiType", NodeTypeLabel(body.bodyType),
                new Vector2(20f, 84f), new Vector2(420f, 20f), 12f, NodeAccent(body.bodyType));

            var desc = NexusUiFactory.CreateText(
                side, "PoiDesc", PoiDescription(body.bodyType),
                new Vector2(20f, 110f), new Vector2(420f, 72f), 12f, NexusTheme.MutedText);
            desc.textWrappingMode = TextWrappingModes.Normal;

            float y = 192f;
            switch (body.bodyType)
            {
                case StellarBodyType.Shop:
                    NexusUiFactory.CreateButton(
                        side, "PoiAction", UiText.T("Open Market", "打开市场"),
                        new Vector2(20f, y), new Vector2(420f, 44f),
                        () => openMarket?.Invoke(),
                        NexusTheme.WithAlpha(NexusTheme.Gold, 0.2f), NexusTheme.Gold, 14f);
                    y += 50f;
                    break;
                case StellarBodyType.Npc:
                    if (GridService.HasChart())
                    {
                        NexusUiFactory.CreateText(
                            side, "PoiChartOwned",
                            UiText.T(
                                "Sector chart acquired — every node in the sector is revealed.",
                                "已购入星域海图——星域内所有节点均已揭示。"),
                            new Vector2(20f, y), new Vector2(420f, 40f), 12f, NexusTheme.Green);
                        y += 44f;
                    }
                    else
                    {
                        NexusUiFactory.CreateButton(
                            side, "PoiAction", UiText.ExploreBuyChart(WorldConstants.ChartCreditCost),
                            new Vector2(20f, y), new Vector2(420f, 44f),
                            BuyChart,
                            NexusTheme.WithAlpha(NexusTheme.Gold, 0.2f), NexusTheme.Gold, 14f);
                        y += 50f;
                    }

                    break;
                case StellarBodyType.Wormhole:
                case StellarBodyType.Exit:
                    NexusUiFactory.CreateButton(
                        side, "PoiAction", UiText.T("Enter Wormhole", "进入虫洞"),
                        new Vector2(20f, y), new Vector2(420f, 44f),
                        () => NexusSnackbar.Show(UiText.T(
                            "Wormhole transit charting in progress…",
                            "虫洞跳跃标定中……")),
                        NexusTheme.WithAlpha(NexusTheme.Purple, 0.2f), NexusTheme.Purple, 14f);
                    y += 50f;
                    break;
                default:
                    NexusUiFactory.CreateButton(
                        side, "PoiAction", UiText.T("Investigate", "调查"),
                        new Vector2(20f, y), new Vector2(420f, 44f),
                        () => NexusSnackbar.Show(UiText.T(
                            "Nothing conclusive yet — return with better scanners.",
                            "暂无确切发现——升级扫描后再来。")),
                        NexusTheme.WithAlpha(NexusTheme.Gold, 0.16f), NexusTheme.Gold, 13f);
                    y += 50f;
                    break;
            }

            return y;
        }

        private float DrawVoyageStub(Transform side, StellarBodyDef body, RegionView view, float y)
        {
            NavigationService.EnsureReady();
            string fromId = NavigationService.DockedBodyId();
            if (string.IsNullOrEmpty(fromId))
                fromId = NavigationService.NearestLocatedBodyId();
            var fromBody = SectorMapCatalog.Get(fromId);
            string fromName = fromBody != null
                ? UiText.T(fromBody.displayNameEn, fromBody.displayNameZh)
                : UiText.ExploreShipMarker;
            string toName = GridService.ShowsFog(body.bodyId)
                ? UiText.ExploreFogName
                : UiText.T(body.displayNameEn, body.displayNameZh);

            NexusUiFactory.CreateText(
                side, "VoyagePlan", UiText.ExploreVoyagePlan(fromName, toName),
                new Vector2(20f, y), new Vector2(420f, 18f), 11f, NexusTheme.Text);
            y += 20f;

            float dist = NavigationService.DistanceToBody(body);
            float speed = Mathf.Max(1f, NavigationService.GetCruiseSpeed());
            float eta = dist / speed;
            NexusUiFactory.CreateText(
                side, "VoyageEta", UiText.ExploreSailEta(dist, eta),
                new Vector2(20f, y), new Vector2(420f, 18f), 11f, NexusTheme.Cyan);
            y += 20f;

            int power = NexusProgressUi.ActiveCombatPower();
            int recPower = view?.Config?.recommendedPower ?? 0;
            if (recPower > 0)
            {
                NexusUiFactory.CreateText(
                    side, "VoyageRisk", UiText.ExploreVoyageRisk(power, recPower),
                    new Vector2(20f, y), new Vector2(420f, 18f), 11f,
                    power < recPower ? NexusTheme.Gold : NexusTheme.Green);
                y += 20f;
            }

            return y + 4f;
        }

        private void BuildGatherBank(Transform side, float y)
        {
            Economy.IdleSettlementService.EnsureLoaded();
            int total = Economy.IdleSettlementService.GatherBankTotal;
            bool full = Economy.IdleSettlementService.IsGatherBankFull;

            NexusUiFactory.CreateBox(
                side, "GatherBank", new Vector2(20f, y), new Vector2(420f, 64f),
                NexusTheme.SurfaceRaised, NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                side, "GatherBankTitle", UiText.GatherBankTitle,
                new Vector2(36f, y + 8f), new Vector2(200f, 22f), 12f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            string body = total <= 0
                ? UiText.GatherBankEmpty
                : full ? UiText.GatherBankFull : UiText.GatherBankHint;
            var bodyText = NexusUiFactory.CreateText(
                side, "GatherBankBody", body,
                new Vector2(36f, y + 32f), new Vector2(250f, 22f), 11f,
                full ? NexusTheme.Red : NexusTheme.MutedText);
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            if (total <= 0)
                return;

            NexusUiFactory.CreateButton(
                side, "CollectGather", UiText.CollectGather(total),
                new Vector2(280f, y + 14f), new Vector2(144f, 36f),
                CollectGather,
                NexusTheme.WithAlpha(NexusTheme.Green, 0.2f), NexusTheme.Green, 12f);
        }

        private float BuildUniverseSectorDetail(Transform side, SectorNodeDef sector)
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
                return 350f;
            }

            return 284f;
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
                Rebuild();
                return;
            }

            selectedSectorId = sectorId;
            if (WorldService.State != null)
                WorldService.State.currentSectorId = sectorId;
            mapRig?.OpenSectorView(ExploreMapRig.SectorEnterZoom, Vector2.zero);
            Rebuild();
        }

        private void SelectBody(string bodyId)
        {
            selectedBodyId = bodyId ?? "";
            var body = SectorMapCatalog.Get(selectedBodyId);
            string regionId = PrimaryRegion(body);
            if (!string.IsNullOrEmpty(regionId))
            {
                PlayerPrefs.SetString("nexus_last_region_id", regionId);
                PlayerPrefs.Save();
            }

            Rebuild();
        }

        private void ProbeBody(string bodyId)
        {
            var result = GridService.TryProbe(bodyId);
            if (result.Success)
            {
                selectedBodyId = bodyId;
                BridgeEventLog.Push(
                    BridgeLogCategory.Explore,
                    "Probe locked a fogged node on the inner grid.",
                    "探测锁定了域内航网上一处熵雾节点。");
            }
            else
                NexusSnackbar.Show(result.Message);

            Rebuild();
        }

        private void StabilizeBody(string bodyId)
        {
            var result = GridService.TryStabilize(bodyId);
            if (result.Success)
            {
                BridgeEventLog.Push(
                    BridgeLogCategory.Explore,
                    "Beacon repaired. A lane is now stable.",
                    "航标已修复。一段航线进入稳定通航。");
            }
            else
                NexusSnackbar.Show(result.Message);

            Rebuild();
        }

        private void BuyChart()
        {
            var result = GridService.TryBuyChart();
            if (result.Success)
            {
                BridgeEventLog.Push(
                    BridgeLogCategory.Explore,
                    "Sector chart purchased from a merchant. The whole sector is revealed.",
                    "从商人处购入星域海图，整个星域的节点均已揭示。");
            }
            else
                NexusSnackbar.Show(result.Message);

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
                NexusSnackbar.Show(result.Message);
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
            if (LiveBattleSession.CanResume)
            {
                if (!string.IsNullOrEmpty(LiveBattleSession.RegionId)
                    && LiveBattleSession.RegionId != regionId)
                {
                    NexusSnackbar.Show(UiText.SnackbarFleetBusy);
                    return;
                }

                BattleController.PendingResumeLiveSession = true;
                BattleController.PendingReturnScreen = AppScreen.Battle;
                LoadingOverlay.LoadScene(nameof(SceneLoader.SceneName.BattleScene));
                return;
            }

            if (DeckService.IsActiveCombatAutoCombatRunning())
            {
                NexusSnackbar.Show(UiText.SnackbarChallengeWhileAutoCombat);
                return;
            }

            if (!WorldService.CanEnter(regionId, out var view) || view?.Config == null)
            {
                NexusSnackbar.Show(UiText.SnackbarRegionUnavailable);
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
                NexusSnackbar.Show(UiText.SnackbarEmptyDeck);
                openFormation?.Invoke();
                return;
            }

            var start = DeckService.TryStart(
                deck.deckId, DeckActionType.MainCombat, regionId, CardListManager.Instance?.cardEntities);
            if (!start.Success)
            {
                NexusSnackbar.Show(start);
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
                NexusSnackbar.Show(UiText.SnackbarFarmLocked);
                return;
            }

            var deck = DeckService.GetActiveCombatDeck();
            if (deck == null || deck.MemberCount < 1)
            {
                NexusSnackbar.Show(UiText.SnackbarEmptyDeck);
                openFormation?.Invoke();
                return;
            }

            var start = DeckService.TryStart(
                deck.deckId, DeckActionType.AutoCombat, regionId, CardListManager.Instance?.cardEntities);
            if (!start.Success)
            {
                NexusSnackbar.Show(start);
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

        private static void DrawInnerLanes(Transform map)
        {
            foreach (var edge in SectorMapCatalog.Edges)
            {
                if (edge == null) continue;
                var a = SectorMapCatalog.Get(edge.a);
                var b = SectorMapCatalog.Get(edge.b);
                if (a == null || b == null) continue;
                if (!IsNodeDisplayable(edge.a) || !IsNodeDisplayable(edge.b))
                    continue;

                var from = SectorToLocal(a.x, a.y);
                var to = SectorToLocal(b.x, b.y);
                var tier = GridService.GetEdgeTier(edge.edgeId);
                Color color = LaneColor(edge, tier);
                float thickness = LaneThickness(edge, tier);
                bool dashed = tier == GridEdgeTier.Provisional && !edge.rift;
                DrawLane(map, "Lane_" + edge.edgeId, from, to, color, thickness, dashed);
            }

            // Lanes attaching points of interest — drawn once their grid anchor is on the map.
            foreach (var edge in SectorMapCatalog.PoiEdges)
            {
                if (edge == null) continue;
                var a = SectorMapCatalog.Get(edge.a);
                var b = SectorMapCatalog.Get(edge.b);
                if (a == null || b == null) continue;
                if (!IsNodeDisplayable(edge.a) || !IsNodeDisplayable(edge.b))
                    continue;

                var from = SectorToLocal(a.x, a.y);
                var to = SectorToLocal(b.x, b.y);
                Color color = edge.rift
                    ? NexusTheme.WithAlpha(NexusTheme.Purple, 0.5f)
                    : NexusTheme.WithAlpha(NexusTheme.MutedText, 0.32f);
                DrawLane(map, "PoiLane_" + edge.edgeId, from, to, color, 2f, dashed: !edge.rift);
            }
        }

        private static Color LaneColor(InnerEdgeDef edge, GridEdgeTier tier)
        {
            if (tier >= GridEdgeTier.Stable)
                return NexusTheme.WithAlpha(NexusTheme.Cyan, 0.75f);
            if (tier == GridEdgeTier.Provisional)
                return NexusTheme.WithAlpha(NexusTheme.Gold, 0.55f);
            if (edge.rift)
                return NexusTheme.WithAlpha(NexusTheme.Purple, 0.55f);
            return NexusTheme.WithAlpha(NexusTheme.MutedText, 0.28f);
        }

        private static float LaneThickness(InnerEdgeDef edge, GridEdgeTier tier)
        {
            if (tier >= GridEdgeTier.Stable) return 5f;
            if (edge != null && edge.rift) return 3.5f;
            if (tier == GridEdgeTier.Provisional) return 2f;
            return 2f;
        }

        private static void DrawLane(
            Transform map, string name, Vector2 from, Vector2 to, Color color,
            float thickness = 3f, bool dashed = false)
        {
            float dx = to.x - from.x;
            float dy = to.y - from.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 4f) return;

            float visualDy = from.y - to.y;
            float angle = Mathf.Atan2(visualDy, dx) * Mathf.Rad2Deg;

            if (!dashed)
            {
                SpawnLaneSegment(map, name, from, to, len, thickness, color, angle);
                return;
            }

            const float dash = 10f;
            const float gap = 6f;
            float stride = dash + gap;
            int count = Mathf.Max(1, Mathf.FloorToInt(len / stride));
            for (int i = 0; i < count; i++)
            {
                float t0 = (i * stride) / len;
                float t1 = Mathf.Min(1f, (i * stride + dash) / len);
                var a = Vector2.Lerp(from, to, t0);
                var b = Vector2.Lerp(from, to, t1);
                float segLen = Vector2.Distance(a, b);
                if (segLen < 2f) continue;
                SpawnLaneSegment(map, name + "_" + i, a, b, segLen, thickness, color, angle);
            }
        }

        private static void SpawnLaneSegment(
            Transform map, string name, Vector2 from, Vector2 to, float len, float thickness, Color color, float angle)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(map, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float midX = (from.x + to.x) * 0.5f;
            float midY = (from.y + to.y) * 0.5f;
            rect.anchoredPosition = new Vector2(midX, -midY);
            rect.sizeDelta = new Vector2(len, thickness);
            rect.localEulerAngles = new Vector3(0f, 0f, angle);
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

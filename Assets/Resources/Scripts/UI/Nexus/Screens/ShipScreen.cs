using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Ship bay: hull appearance, mascot card, and module upgrades (P2.2).
    /// </summary>
    internal sealed class ShipScreen
    {
        private const float LeftColumnX = 28f;
        private const float LeftColumnWidth = 520f;
        private const float RightColumnX = 576f;
        private const float RightColumnWidth = 1080f;
        private const float AppearanceHeight = 360f;
        private const float MascotTop = 396f;
        private const float MascotHeight = 500f;

        private readonly Transform root;
        private readonly System.Action onClose;

        private ShipScreen(Transform root, System.Action onClose)
        {
            this.root = root;
            this.onClose = onClose;
        }

        public GameObject Root => root.gameObject;

        public static ShipScreen Build(Transform parent, System.Action onClose)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Ship Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new ShipScreen(panel.transform, onClose);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            CloseMascotPicker();
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            if (DataUtil.Instance != null)
                ShipService.EnsureLoaded(DataUtil.Instance);

            var ship = ShipService.State ?? ShipRules.CreateStarterShip();

            BuildAppearance(ship);
            BuildMascot(ship);
            BuildUpgrades(ship);
        }

        // ---------------------------------------------------------------- appearance

        private void BuildAppearance(ShipEntity ship)
        {
            GameObject box = NexusUiFactory.CreateBox(
                root, "Appearance",
                new Vector2(LeftColumnX, 20f), new Vector2(LeftColumnWidth, AppearanceHeight),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            NexusUiFactory.CreateText(
                box.transform, "Heading", UiText.ShipAppearance,
                new Vector2(16f, 12f), new Vector2(LeftColumnWidth - 32f, 24f), 14f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                box.transform, "HullClass", UiText.ShipHullClass(ship.displayName, ship.level),
                new Vector2(16f, 34f), new Vector2(LeftColumnWidth - 32f, 20f), 12f, NexusTheme.MutedText);

            DrawHull(box.transform, ship);

            var hint = NexusUiFactory.CreateText(
                box.transform, "Hint", UiText.ShipAppearanceHint,
                new Vector2(16f, 322f), new Vector2(LeftColumnWidth - 32f, 30f), 11f, NexusTheme.DimText);
            hint.textWrappingMode = TextWrappingModes.Normal;
        }

        /// <summary>
        /// Draws the hull from primitives rather than art, so the silhouette can grow with the
        /// ship level and fitted modules without needing a sprite per configuration.
        /// </summary>
        private static void DrawHull(Transform box, ShipEntity ship)
        {
            const float viewX = 16f;
            const float viewY = 56f;
            const float viewW = LeftColumnWidth - 32f;
            const float viewH = 258f;
            float cx = viewX + viewW * 0.5f;

            NexusUiFactory.CreateBox(
                box, "HullView", new Vector2(viewX, viewY), new Vector2(viewW, viewH),
                NexusTheme.WithAlpha(NexusTheme.Background, 0.9f), NexusTheme.BorderSoft);

            DrawStarfield(box, ship, viewX, viewY, viewW, viewH);

            Color accent = HullAccent(ship.level);
            Color plating = NexusTheme.SurfaceHover;
            int thrustLevel = ModuleLevel(ship, "mod_propulsion");
            int cargoLevel = ModuleLevel(ship, "mod_cargo");
            int armorLevel = ModuleLevel(ship, "mod_armor");
            int scanLevel = ModuleLevel(ship, "mod_scanner");

            // Wings widen with the cargo bay; the hull thickens with ship level and armour.
            float wingSpan = 100f + Mathf.Clamp(cargoLevel, 0, 6) * 8f;
            float bodyWidth = 44f + Mathf.Clamp(ship.level, 0, 8) * 3f + Mathf.Clamp(armorLevel, 0, 6) * 2f;

            HullPiece(box, "WingLeft", new Vector2(cx - wingSpan * 0.55f, viewY + 150f),
                new Vector2(wingSpan, 22f), plating, 22f);
            HullPiece(box, "WingRight", new Vector2(cx + wingSpan * 0.55f, viewY + 150f),
                new Vector2(wingSpan, 22f), plating, -22f);
            HullPiece(box, "WingTipLeft", new Vector2(cx - wingSpan, viewY + 132f),
                new Vector2(9f, 30f), accent, 22f);
            HullPiece(box, "WingTipRight", new Vector2(cx + wingSpan, viewY + 132f),
                new Vector2(9f, 30f), accent, -22f);

            HullPiece(box, "Fuselage", new Vector2(cx, viewY + 128f),
                new Vector2(bodyWidth, 148f), NexusTheme.SurfaceRaised);
            HullPiece(box, "Spine", new Vector2(cx, viewY + 128f),
                new Vector2(6f, 130f), NexusTheme.WithAlpha(accent, 0.35f));
            HullPiece(box, "Nose", new Vector2(cx, viewY + 48f),
                new Vector2(bodyWidth * 0.55f, 34f), plating);
            HullPiece(box, "Cockpit", new Vector2(cx, viewY + 74f),
                new Vector2(22f, 30f), accent);

            float engineOffset = bodyWidth * 0.5f + 8f;
            HullPiece(box, "EngineLeft", new Vector2(cx - engineOffset, viewY + 196f),
                new Vector2(17f, 40f), plating);
            HullPiece(box, "EngineRight", new Vector2(cx + engineOffset, viewY + 196f),
                new Vector2(17f, 40f), plating);

            float plumeHeight = 12f + Mathf.Clamp(thrustLevel, 0, 6) * 4f;
            HullPiece(box, "PlumeLeft", new Vector2(cx - engineOffset, viewY + 216f + plumeHeight * 0.5f),
                new Vector2(11f, plumeHeight), NexusTheme.WithAlpha(accent, 0.75f));
            HullPiece(box, "PlumeRight", new Vector2(cx + engineOffset, viewY + 216f + plumeHeight * 0.5f),
                new Vector2(11f, plumeHeight), NexusTheme.WithAlpha(accent, 0.75f));

            if (ship.level >= 3)
            {
                HullPiece(box, "Fin", new Vector2(cx, viewY + 190f),
                    new Vector2(12f, 52f), NexusTheme.WithAlpha(plating, 0.9f));
            }

            if (scanLevel > 0)
            {
                HullPiece(box, "Dish", new Vector2(cx, viewY + 34f),
                    new Vector2(30f + scanLevel * 4f, 7f), accent);
            }
        }

        private static void DrawStarfield(
            Transform box, ShipEntity ship, float viewX, float viewY, float viewW, float viewH)
        {
            // Deterministic per ship so the backdrop does not flicker between rebuilds.
            int seed = string.IsNullOrEmpty(ship.shipId) ? 7 : ship.shipId.GetHashCode();
            var random = new System.Random(seed);
            for (var i = 0; i < 22; i++)
            {
                float x = viewX + 6f + (float)random.NextDouble() * (viewW - 12f);
                float y = viewY + 6f + (float)random.NextDouble() * (viewH - 12f);
                float size = random.Next(0, 3) == 0 ? 3f : 2f;
                HullPiece(box, $"Star{i}", new Vector2(x, y), new Vector2(size, size),
                    NexusTheme.WithAlpha(NexusTheme.Text, 0.14f + (float)random.NextDouble() * 0.26f));
            }
        }

        /// <summary>Places a box by its centre inside <paramref name="parent"/>'s top-left space.</summary>
        private static GameObject HullPiece(
            Transform parent, string name, Vector2 center, Vector2 size, Color color, float rotation = 0f)
        {
            GameObject piece = NexusUiFactory.CreateBox(parent, name, Vector2.zero, size, color);
            var rect = piece.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(center.x, -center.y);
            rect.sizeDelta = size;
            if (Mathf.Abs(rotation) > 0.01f)
                rect.localEulerAngles = new Vector3(0f, 0f, rotation);
            return piece;
        }

        private static Color HullAccent(int level) => level switch
        {
            <= 2 => NexusTheme.Cyan,
            <= 4 => NexusTheme.Purple,
            _ => NexusTheme.Gold
        };

        private static int ModuleLevel(ShipEntity ship, string moduleId)
        {
            if (ship?.modules == null) return 0;
            foreach (var module in ship.modules)
            {
                if (module != null && module.moduleId == moduleId)
                    return module.level;
            }

            return 0;
        }

        // -------------------------------------------------------------------- mascot

        private void BuildMascot(ShipEntity ship)
        {
            GameObject box = NexusUiFactory.CreateBox(
                root, "Mascot",
                new Vector2(LeftColumnX, MascotTop), new Vector2(LeftColumnWidth, MascotHeight),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            NexusUiFactory.CreateText(
                box.transform, "Heading", UiText.ShipMascot,
                new Vector2(16f, 12f), new Vector2(LeftColumnWidth - 32f, 24f), 14f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                box.transform, "Hint", UiText.ShipMascotHint,
                new Vector2(16f, 36f), new Vector2(LeftColumnWidth - 32f, 20f), 11f, NexusTheme.MutedText);

            CardEntity mascot = FindMascot(ship);
            float stageW = LeftColumnWidth - 32f;
            NexusUiFactory.CreateBox(
                box.transform, "Stage", new Vector2(16f, 62f), new Vector2(stageW, 288f),
                NexusTheme.WithAlpha(NexusTheme.Background, 0.85f), NexusTheme.BorderSoft);

            if (mascot == null)
            {
                NexusUiFactory.CreateText(
                    box.transform, "Empty", UiText.ShipMascotEmpty,
                    new Vector2(16f, 190f), new Vector2(stageW, 28f), 13f, NexusTheme.DimText,
                    TextAlignmentOptions.Center);
            }
            else
            {
                Sprite frame = NexusCardVisual.TierFrameSprite(mascot);
                if (frame != null)
                {
                    NexusUiFactory.CreateIcon(
                        box.transform, "Frame", frame,
                        new Vector2(166f, 70f), new Vector2(220f, 272f), NexusTheme.WithAlpha(Color.white, 0.7f));
                }

                NexusUiFactory.CreateIcon(
                    box.transform, "Portrait", NexusCardVisual.CharacterSprite(mascot),
                    new Vector2(176f, 76f), new Vector2(200f, 250f), Color.white);

                Sprite badge = NexusCardVisual.TierBadgeSprite(mascot);
                if (badge != null)
                {
                    NexusUiFactory.CreateIcon(
                        box.transform, "Badge", badge,
                        new Vector2(28f, 74f), new Vector2(32f, 32f), Color.white);
                }

                NexusUiFactory.CreateText(
                    box.transform, "Name", DisplayName(mascot),
                    new Vector2(16f, 358f), new Vector2(stageW, 28f), 18f, NexusTheme.Text,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                NexusUiFactory.CreateText(
                    box.transform, "Meta", $"{mascot.CharacterTier}  ·  Lv.{mascot.level}",
                    new Vector2(16f, 388f), new Vector2(stageW, 22f), 12f, NexusTheme.Cyan,
                    TextAlignmentOptions.Center);
            }

            NexusUiFactory.CreateButton(
                box.transform, "PickMascot",
                mascot == null ? UiText.ShipMascotChoose : UiText.ShipMascotChange,
                new Vector2(16f, 424f), new Vector2(mascot == null ? stageW : 300f, 44f),
                OpenMascotPicker,
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 13f);

            if (mascot != null)
            {
                NexusUiFactory.CreateButton(
                    box.transform, "ClearMascot", UiText.ShipMascotClear,
                    new Vector2(326f, 424f), new Vector2(178f, 44f),
                    () =>
                    {
                        ShipService.SetMascot("");
                        Rebuild();
                    },
                    NexusTheme.SurfaceRaised, NexusTheme.MutedText, 12f);
            }
        }

        private static CardEntity FindMascot(ShipEntity ship)
        {
            if (ship == null || string.IsNullOrEmpty(ship.mascotCardId))
                return null;

            var all = CardListManager.Instance?.GetCardEntities();
            if (all == null) return null;
            foreach (var card in all)
            {
                if (card != null && card.id == ship.mascotCardId)
                    return card;
            }

            return null;
        }

        private static string DisplayName(CardEntity card)
        {
            if (card == null) return "";
            return string.IsNullOrEmpty(card.cardName) ? card.characterName.ToString() : card.cardName;
        }

        private void CloseMascotPicker()
        {
            Transform overlay = root.Find("MascotOverlay");
            if (overlay != null)
                Object.DestroyImmediate(overlay.gameObject);
        }

        private void OpenMascotPicker()
        {
            CloseMascotPicker();

            GameObject overlay = NexusUiFactory.CreatePanel(
                root, "MascotOverlay", NexusTheme.WithAlpha(Color.black, 0.6f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            overlay.transform.SetAsLastSibling();
            var dismiss = overlay.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(CloseMascotPicker);

            const float dialogW = 900f;
            const float dialogH = 620f;
            GameObject dialog = NexusUiFactory.CreateBox(
                overlay.transform, "MascotDialog", Vector2.zero, new Vector2(dialogW, dialogH),
                NexusTheme.Surface, NexusTheme.Gold);
            var dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(dialogW, dialogH);
            dialog.GetComponent<Image>().raycastTarget = true;

            NexusUiFactory.CreateText(
                dialog.transform, "Title", UiText.ShipMascotPicker,
                new Vector2(20f, 14f), new Vector2(dialogW - 140f, 28f), 16f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateButton(
                dialog.transform, "Close", UiText.Close,
                new Vector2(dialogW - 100f, 12f), new Vector2(80f, 28f),
                CloseMascotPicker,
                NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);

            DrawMascotGrid(dialog.transform, dialogW, dialogH);
        }

        private void DrawMascotGrid(Transform dialog, float dialogW, float dialogH)
        {
            var owned = CardListManager.Instance?.GetCardEntities() ?? new List<CardEntity>();
            if (owned.Count == 0)
            {
                NexusUiFactory.CreateText(
                    dialog, "Empty", UiText.ShipMascotNoneOwned,
                    new Vector2(24f, 70f), new Vector2(dialogW - 48f, 24f), 13f, NexusTheme.DimText);
                return;
            }

            const int columns = 5;
            const float cardW = 150f;
            const float cardH = 210f;
            const float gap = 12f;
            float gridW = dialogW - 40f;
            float gridH = dialogH - 76f;
            Transform content = CreateScrollArea(dialog, new Vector2(20f, 56f), new Vector2(gridW, gridH));

            string selectedId = ShipService.MascotCardId;
            for (var i = 0; i < owned.Count; i++)
            {
                CardEntity card = owned[i];
                if (card == null) continue;

                int column = i % columns;
                int row = i / columns;
                var position = new Vector2(10f + column * (cardW + gap), 10f + row * (cardH + gap));
                bool selected = card.id == selectedId;

                GameObject cell = NexusCardVisual.CreatePortraitCard(
                    content, $"Mascot {i}", card, position, new Vector2(cardW, cardH),
                    selected ? UiText.ShipMascot : "");

                var image = cell.GetComponent<Image>();
                image.raycastTarget = true;
                image.color = selected
                    ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.2f)
                    : NexusTheme.SurfaceRaised;

                string captured = card.id;
                var button = cell.AddComponent<Button>();
                var colors = button.colors;
                colors.normalColor = image.color;
                colors.highlightedColor = NexusTheme.SurfaceHover;
                colors.pressedColor = NexusTheme.WithAlpha(NexusTheme.Gold, 0.45f);
                button.colors = colors;
                button.onClick.AddListener(() =>
                {
                    ShipService.SetMascot(captured);
                    Rebuild();
                });
            }

            int rows = (owned.Count + columns - 1) / columns;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0f, Mathf.Max(gridH, 20f + rows * (cardH + gap)));
        }

        private static Transform CreateScrollArea(Transform parent, Vector2 position, Vector2 size)
        {
            var viewport = new GameObject(
                "Mascot Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);

            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 1f);
            viewportRect.anchorMax = new Vector2(0f, 1f);
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.anchoredPosition = new Vector2(position.x, -position.y);
            viewportRect.sizeDelta = size;

            var image = viewport.GetComponent<Image>();
            image.color = NexusTheme.WithAlpha(NexusTheme.Background, 0.55f);
            image.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, size.y);

            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            return content.transform;
        }

        // ------------------------------------------------------------------ upgrades

        private void BuildUpgrades(ShipEntity ship)
        {
            var stats = ShipRules.GetEffectiveStats(ship);
            int credits = DataUtil.Instance?.currentPlayer?.creditPoints ?? 0;

            NexusUiFactory.CreateText(
                root, "Title", UiText.ShipBayTitle,
                new Vector2(RightColumnX, 20f), new Vector2(600f, 36f), 22f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                root, "Subtitle", UiText.ShipSummary(ship.level, credits),
                new Vector2(RightColumnX, 58f), new Vector2(700f, 24f), 13f, NexusTheme.MutedText);

            NexusUiFactory.CreateBox(
                root, "StatsBox", new Vector2(RightColumnX, 90f), new Vector2(RightColumnWidth, 84f),
                NexusTheme.Surface, NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                root, "Stats",
                $"{UiText.ShipStatRange} {stats.range}   {UiText.ShipStatEnergy} {stats.energy}   " +
                $"{UiText.ShipStatHull} {stats.hull}   {UiText.ShipStatEntropy} {stats.entropyResist}\n" +
                $"{UiText.ShipStatCargo} {stats.cargo}   {UiText.ShipStatScan} {stats.scan}   " +
                $"{UiText.ShipStatLife} {stats.lifeSupport}",
                new Vector2(RightColumnX + 20f, 108f), new Vector2(RightColumnWidth - 40f, 56f), 14f, NexusTheme.Cyan);

            NexusUiFactory.CreateButton(
                root, "UpgradeLevel",
                UiText.UpgradeShipLevel(
                    ShipRules.ScrapCostForShipLevel(ship.level + 1),
                    ShipRules.CreditCostForShipLevel(ship.level + 1)),
                new Vector2(RightColumnX, 190f), new Vector2(460f, 44f),
                () =>
                {
                    var r = ShipService.TryUpgradeShipLevel();
                    Debug.Log("[SHIP] level upgrade: " + (r.Success ? "ok" : r.Message));
                    Rebuild();
                },
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 13f);

            NexusUiFactory.CreateText(
                root, "ModulesHeading", UiText.ShipModules,
                new Vector2(RightColumnX, 252f), new Vector2(RightColumnWidth, 24f), 14f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);

            float y = 284f;
            foreach (var def in ShipModuleCatalog.All)
            {
                int level = ModuleLevel(ship, def.moduleId);
                string label =
                    $"{UiText.T(def.displayNameEn, def.displayNameZh)} Lv.{level}  (+{def.primaryStat})  " +
                    $"[{ShipRules.ScrapCostForModule(def.moduleId, level + 1)} scrap / {ShipRules.CreditCostForModule(def.moduleId, level + 1)}₵]";

                string captured = def.moduleId;
                NexusUiFactory.CreateButton(
                    root, "Mod " + def.moduleId, label,
                    new Vector2(RightColumnX, y), new Vector2(RightColumnWidth, 40f),
                    () =>
                    {
                        var r = ShipService.TryUpgradeModule(captured);
                        Debug.Log("[SHIP] module " + captured + ": " + (r.Success ? "ok" : r.Message));
                        Rebuild();
                    },
                    NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);
                y += 48f;
            }

            NexusUiFactory.CreateButton(
                root, "Back", UiText.Back,
                new Vector2(RightColumnX, y + 20f), new Vector2(200f, 44f),
                () => onClose?.Invoke(),
                NexusTheme.SurfaceRaised, NexusTheme.MutedText, 14f);
        }
    }
}

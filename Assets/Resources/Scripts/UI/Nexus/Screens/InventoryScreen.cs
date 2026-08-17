using System.Collections.Generic;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Native warehouse UI on the Nexus content canvas (legacy panel is World Space and hidden).</summary>
    internal sealed class InventoryScreen
    {
        private readonly Transform root;
        private int typeTab;
        private int qualityTab;

        private InventoryScreen(Transform root)
        {
            this.root = root;
        }

        public GameObject Root => root.gameObject;

        public static InventoryScreen Build(Transform parent)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Inventory Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new InventoryScreen(panel.transform);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            IdleSettlementService.EnsureLoaded();
            List<RewardPopup.Line> claimedLines = null;
            if (IdleSettlementService.PendingCount > 0)
            {
                IdleSettlementService.ClaimAllPending(out var claimed);
                claimedLines = RewardPopup.FromPending(claimed);
            }

            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            NexusUiFactory.CreateText(
                root, "Title", UiText.InventoryTitle,
                new Vector2(28f, 16f), new Vector2(640f, 32f), 22f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                root, "Hint", UiText.InventoryHint,
                new Vector2(28f, 48f), new Vector2(1200f, 24f), 12f, NexusTheme.MutedText);

            DrawTypeTabs();
            DrawQualityTabs();
            DrawGrid(VisibleItems());

            if (claimedLines != null && claimedLines.Count > 0)
                RewardPopup.Show(UiText.RewardTitle, claimedLines);
        }

        private void DrawTypeTabs()
        {
            Tab(28f, 80f, "type-all", typeTab == 0, UiText.InventoryTabAll, () => SetType(0));
            Tab(168f, 80f, "type-eq", typeTab == 1, UiText.InventoryTabEquipment, () => SetType(1));
            Tab(308f, 80f, "type-mat", typeTab == 2, UiText.InventoryTabMaterial, () => SetType(2));
            Tab(448f, 80f, "type-con", typeTab == 3, UiText.InventoryTabConsumable, () => SetType(3));
        }

        private void DrawQualityTabs()
        {
            string[] labels =
            {
                UiText.InventoryQualityAll, "Q1", "Q2", "Q3", "Q4", "Q5"
            };
            for (var q = 0; q < labels.Length; q++)
            {
                var captured = q;
                Tab(28f + q * 88f, 122f, "q-" + q, qualityTab == q, labels[q], () => SetQuality(captured), 80f);
            }
        }

        private void SetType(int tab)
        {
            typeTab = tab;
            Rebuild();
        }

        private void SetQuality(int tab)
        {
            qualityTab = tab;
            Rebuild();
        }

        private void Tab(
            float x, float y, string id, bool selected, string label, UnityEngine.Events.UnityAction onClick, float width = 132f)
        {
            NexusUiFactory.CreateButton(
                root, "Tab " + id, label,
                new Vector2(x, y), new Vector2(width, 34f),
                onClick,
                selected ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f) : NexusTheme.SurfaceRaised,
                selected ? NexusTheme.Gold : NexusTheme.Text,
                12f);
        }

        private void DrawGrid(List<ItemEntity> visible)
        {
            var content = CreateScrollContent(root, new Vector2(28f, 168f), new Vector2(1480f, 680f));
            if (visible.Count == 0)
            {
                NexusUiFactory.CreateText(
                    content, "Empty", UiText.InventoryEmpty,
                    new Vector2(8f, 8f), new Vector2(800f, 32f), 14f, NexusTheme.MutedText);
                return;
            }

            const int cols = 2;
            const float rowH = 72f;
            for (var i = 0; i < visible.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                DrawItemCard(content, visible[i], 8f + col * 720f, 8f + row * rowH);
            }

            var contentRect = content.GetComponent<RectTransform>();
            int rows = (visible.Count + cols - 1) / cols;
            contentRect.sizeDelta = new Vector2(0f, Mathf.Max(80f, 16f + rows * rowH));
        }

        private static void DrawItemCard(Transform parent, ItemEntity item, float x, float y)
        {
            if (item == null) return;
            ItemFactory.NormalizeLegacy(item);

            var def = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
            var name = def != null ? UiText.T(def.displayNameEn, def.displayNameZh) : item.itemName;
            var typeLabel = item.itemType switch
            {
                ItemType.Equipment => UiText.InventoryTabEquipment,
                ItemType.Food => UiText.InventoryTabConsumable,
                _ => UiText.InventoryTabMaterial
            };
            var quality = item.quality > 0 ? item.quality : 2;
            var iconName = ItemFactory.ResolveIcon(
                string.IsNullOrEmpty(item.itemIcon) ? def?.icon : item.itemIcon);

            NexusUiFactory.CreateBox(
                parent, "Item " + (item.itemDefId ?? item.itemName),
                new Vector2(x, y), new Vector2(700f, 64f),
                NexusTheme.SurfaceRaised, NexusTheme.BorderSoft);

            var sprite = ImageUtil.GetSpriteByName(ImageUtil.itemImagePath, iconName ?? "Steel");
            NexusUiFactory.CreateIcon(
                parent, "Icon " + name, sprite,
                new Vector2(x + 8f, y + 6f), new Vector2(52f, 52f), Color.white);

            NexusUiFactory.CreateText(
                parent, "Name " + name, name ?? "",
                new Vector2(x + 72f, y + 8f), new Vector2(400f, 24f), 15f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                parent, "Meta " + name,
                $"{typeLabel}  ·  {UiText.InventoryQty(quality, item.quantity)}",
                new Vector2(x + 72f, y + 34f), new Vector2(500f, 22f), 12f, NexusTheme.MutedText);
        }

        private List<ItemEntity> VisibleItems()
        {
            var source = ProductionService.GetLocalItems() ?? new List<ItemEntity>();
            var list = new List<ItemEntity>();
            foreach (var item in source)
            {
                if (item == null) continue;
                ItemFactory.NormalizeLegacy(item);
                var category = ItemCatalog.Get(ItemFactory.ResolveDefId(item))?.category;
                if (typeTab == 1 && item.itemType != ItemType.Equipment
                    && category != ItemCategory.Equipment && category != ItemCategory.ShipModule)
                    continue;
                if (typeTab == 2 && item.itemType != ItemType.Material
                    && category != ItemCategory.Material && category != ItemCategory.Intermediate)
                    continue;
                if (typeTab == 3 && item.itemType != ItemType.Food
                    && category != ItemCategory.Consumable)
                    continue;
                if (qualityTab >= 1 && item.quality != qualityTab) continue;
                list.Add(item);
            }

            return list;
        }

        private static Transform CreateScrollContent(Transform parent, Vector2 position, Vector2 size)
        {
            var viewport = new GameObject("Inventory Scroll", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(parent, false);

            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 1f);
            viewportRect.anchorMax = new Vector2(0f, 1f);
            viewportRect.pivot = new Vector2(0f, 1f);
            viewportRect.anchoredPosition = new Vector2(position.x, -position.y);
            viewportRect.sizeDelta = size;

            var image = viewport.GetComponent<Image>();
            image.color = NexusTheme.WithAlpha(NexusTheme.Surface, 0.35f);
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
            scroll.scrollSensitivity = 24f;
            return content.transform;
        }
    }
}

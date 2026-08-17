using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Shared hover tip for warehouse / upgrade cost icons: lore, quality, and clickable acquire links.
    /// </summary>
    internal static class ItemTooltip
    {
        private const float TipWidth = 360f;
        private const int SortingOrder = 520;

        private static GameObject current;
        private static ItemTooltipAnchor owner;
        private static float hideAt = -1f;

        public static void Show(ItemTooltipAnchor anchor, ItemDef def, ItemEntity entity = null)
        {
            if (anchor == null || def == null) return;
            hideAt = -1f;
            owner = anchor;
            Build(def, entity, anchor.transform as RectTransform);
        }

        public static void ScheduleHide(ItemTooltipAnchor anchor)
        {
            if (owner != anchor) return;
            hideAt = Time.unscaledTime + 0.12f;
        }

        public static void CancelHide()
        {
            hideAt = -1f;
        }

        public static void Hide()
        {
            hideAt = -1f;
            owner = null;
            if (current != null)
                Object.Destroy(current);
            current = null;
        }

        public static void Tick()
        {
            if (hideAt > 0f && Time.unscaledTime >= hideAt)
                Hide();
        }

        private static void Build(ItemDef def, ItemEntity entity, RectTransform anchor)
        {
            if (current != null)
                Object.Destroy(current);

            Canvas canvas = NexusUiFactory.CreateCanvas("Item Tooltip", SortingOrder, true);
            if (AppShell.Instance != null)
                canvas.transform.SetParent(AppShell.Instance.transform, false);
            current = canvas.gameObject;
            current.AddComponent<ItemTooltipTicker>();

            var tip = NexusUiFactory.CreateBox(
                canvas.transform, "Tip", Vector2.zero, new Vector2(TipWidth, 220f),
                NexusTheme.SurfaceRaised, NexusTheme.Gold);
            tip.GetComponent<Image>().raycastTarget = true;
            var tipRect = tip.GetComponent<RectTransform>();
            tipRect.anchorMin = tipRect.anchorMax = tipRect.pivot = new Vector2(0f, 1f);

            tip.AddComponent<ItemTooltipPanel>();
            PositionNear(tipRect, anchor);

            float y = 12f;
            NexusUiFactory.CreateText(
                tip.transform, "Name", UiText.ItemName(def),
                new Vector2(14f, y), new Vector2(TipWidth - 28f, 24f), 15f, NexusTheme.Gold);
            y += 26f;

            string meta = UiText.ItemCategoryLabel(def.category);
            if (entity != null)
            {
                int q = entity.quality > 0 ? entity.quality : EconomyConstants.DefaultQuality;
                meta += $"  ·  {UiText.ItemQualityLabel(q)}";
                if (entity.maxDurability > 0)
                    meta += $"  ·  {UiText.GearDurability(entity.durability, entity.maxDurability)}";
                if (entity.quantity > 1)
                    meta += $"  ·  ×{entity.quantity}";
            }

            NexusUiFactory.CreateText(
                tip.transform, "Meta", meta,
                new Vector2(14f, y), new Vector2(TipWidth - 28f, 18f), 11f, NexusTheme.Cyan);
            y += 22f;

            NexusUiFactory.CreateText(
                tip.transform, "Desc", UiText.ItemDescription(def),
                new Vector2(14f, y), new Vector2(TipWidth - 28f, 56f), 12f, NexusTheme.MutedText);
            y += 60f;

            NexusUiFactory.CreateText(
                tip.transform, "SourcesHead", UiText.ItemAcquireHeading,
                new Vector2(14f, y), new Vector2(TipWidth - 28f, 18f), 12f, NexusTheme.Gold);
            y += 22f;

            var sources = ItemAcquireCatalog.ForItem(def.itemDefId);
            if (sources.Count == 0)
            {
                NexusUiFactory.CreateText(
                    tip.transform, "NoSource", UiText.ItemAcquireNone,
                    new Vector2(14f, y), new Vector2(TipWidth - 28f, 20f), 11f, NexusTheme.DimText);
                y += 24f;
            }
            else
            {
                foreach (var src in sources)
                {
                    if (src == null) continue;
                    bool unlocked = IsUnlocked(src);
                    string label = UiText.T(src.labelEn, src.labelZh);
                    if (!unlocked)
                        label = $"{label}  ·  {UiText.ItemAcquireLocked}";

                    var captured = src;
                    var canJump = unlocked;
                    var btn = NexusUiFactory.CreateButton(
                        tip.transform, "Src " + src.sourceId, label,
                        new Vector2(14f, y), new Vector2(TipWidth - 28f, 28f),
                        () =>
                        {
                            if (!canJump) return;
                            Jump(captured);
                        },
                        unlocked
                            ? NexusTheme.WithAlpha(NexusTheme.Cyan, 0.14f)
                            : NexusTheme.WithAlpha(NexusTheme.Surface, 0.8f),
                        unlocked ? NexusTheme.Cyan : NexusTheme.DimText,
                        11f);
                    if (!unlocked)
                        btn.interactable = false;

                    y += 32f;
                }
            }

            tipRect.sizeDelta = new Vector2(TipWidth, y + 12f);
            PositionNear(tipRect, anchor);
        }

        private static void PositionNear(RectTransform tip, RectTransform anchor)
        {
            if (tip == null) return;
            Vector2 screen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (anchor != null)
            {
                var corners = new Vector3[4];
                anchor.GetWorldCorners(corners);
                screen = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            }

            float x = Mathf.Clamp(screen.x + 12f, 16f, Screen.width - TipWidth - 16f);
            float yFromTop = Screen.height - screen.y + 8f;
            tip.anchoredPosition = new Vector2(x, -Mathf.Clamp(yFromTop, 16f, Screen.height - 40f));
        }

        public static bool IsUnlocked(ItemAcquireSourceDef src)
        {
            if (src == null) return false;
            switch (src.kind)
            {
                case ItemAcquireKind.Gather:
                    if (string.IsNullOrEmpty(src.regionId)) return true;
                    if (WorldService.IsFarmUnlocked(src.regionId)) return true;
                    var gatherView = WorldService.GetRegionView(src.regionId);
                    return gatherView != null &&
                           (gatherView.Progress == RegionProgressState.Cleared
                            || gatherView.Progress == RegionProgressState.BossDefeated);
                case ItemAcquireKind.Combat:
                    if (string.IsNullOrEmpty(src.regionId)) return true;
                    return IsRegionReachable(src.regionId);
                case ItemAcquireKind.Craft:
                    // Crafting hub is always reachable; recipe/facility gates live on CraftingScreen.
                    return true;
                case ItemAcquireKind.Market:
                case ItemAcquireKind.Recruit:
                case ItemAcquireKind.Mission:
                    return true;
                default:
                    return true;
            }
        }

        private static bool IsRegionReachable(string regionId)
        {
            var view = WorldService.GetRegionView(regionId);
            return view != null && view.Progress != RegionProgressState.Locked;
        }

        public static void Jump(ItemAcquireSourceDef src)
        {
            if (src == null || !IsUnlocked(src)) return;
            Hide();
            var screen = MapScreen(src.targetScreen);
            if (AppShell.Instance != null)
                AppShell.Instance.Navigate(screen);
            else
                AppShell.RequestScreen(screen);
        }

        public static AppScreen MapScreen(string target) => target switch
        {
            "Crafting" => AppScreen.Crafting,
            "Market" => AppScreen.Market,
            "Recruit" => AppScreen.Recruit,
            "Missions" => AppScreen.Missions,
            "Inventory" => AppScreen.Inventory,
            "Ship" => AppScreen.Ship,
            "Formation" => AppScreen.Formation,
            _ => AppScreen.Battle
        };
    }

    /// <summary>Attach to an item icon/card to open <see cref="ItemTooltip"/> on hover.</summary>
    internal sealed class ItemTooltipAnchor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public ItemDef Def;
        public ItemEntity Entity;

        public void OnPointerEnter(PointerEventData eventData) =>
            ItemTooltip.Show(this, Def, Entity);

        public void OnPointerExit(PointerEventData eventData) =>
            ItemTooltip.ScheduleHide(this);
    }

    internal sealed class ItemTooltipPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData) => ItemTooltip.CancelHide();

        public void OnPointerExit(PointerEventData eventData) => ItemTooltip.Hide();
    }

    internal sealed class ItemTooltipTicker : MonoBehaviour
    {
        private void Update() => ItemTooltip.Tick();

        private void OnDestroy()
        {
            // no-op; Hide clears statics from callers
        }
    }
}

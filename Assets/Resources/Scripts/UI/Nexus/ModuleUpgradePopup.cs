using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Modal confirming a ship-module upgrade and listing scrap / credit costs.
    /// </summary>
    internal static class ModuleUpgradePopup
    {
        private const float DialogWidth = 520f;
        private static GameObject current;

        public static void Close()
        {
            ItemTooltip.Hide();
            if (current != null)
                Object.Destroy(current);
            current = null;
        }

        public static void Show(ShipModuleDef def, int currentLevel, System.Action onUpgraded)
        {
            if (def == null) return;
            Close();

            int next = currentLevel + 1;
            int scrapNeed = ShipRules.ScrapCostForModule(def.moduleId, next);
            int creditNeed = ShipRules.CreditCostForModule(def.moduleId, next);
            int scrapHave = CountScrap();
            int creditHave = DataUtil.Instance?.currentPlayer?.creditPoints ?? 0;
            bool canAfford = ShipService.CanAfford(scrapNeed, creditNeed);

            Canvas canvas = NexusUiFactory.CreateCanvas("Module Upgrade Popup", 510, true);
            if (AppShell.Instance != null)
                canvas.transform.SetParent(AppShell.Instance.transform, false);
            current = canvas.gameObject;

            GameObject scrim = NexusUiFactory.CreatePanel(
                canvas.transform, "Scrim",
                NexusTheme.WithAlpha(Color.black, 0.66f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            var dismiss = scrim.AddComponent<Button>();
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(Close);

            const float dialogH = 380f;
            GameObject dialog = NexusUiFactory.CreateBox(
                scrim.transform, "Dialog", Vector2.zero, new Vector2(DialogWidth, dialogH),
                NexusTheme.Surface, NexusTheme.Gold);
            dialog.GetComponent<Image>().raycastTarget = true;
            var dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;

            // Absorb clicks on empty dialog chrome without closing the modal.
            var block = dialog.AddComponent<Button>();
            block.transition = Selectable.Transition.None;

            string title = UiText.T(def.displayNameEn, def.displayNameZh);
            NexusUiFactory.CreateText(
                dialog.transform, "Title", UiText.ShipModuleUpgradeTitle(title),
                new Vector2(24f, 18f), new Vector2(DialogWidth - 48f, 28f), 18f, NexusTheme.Gold);

            NexusUiFactory.CreateText(
                dialog.transform, "Level", UiText.ShipModuleLevelNext(currentLevel, next),
                new Vector2(24f, 50f), new Vector2(DialogWidth - 48f, 22f), 13f, NexusTheme.Cyan);

            NexusUiFactory.CreateText(
                dialog.transform, "CostsHead", UiText.ShipModuleUpgradeCosts,
                new Vector2(24f, 88f), new Vector2(DialogWidth - 48f, 22f), 13f, NexusTheme.Text);

            DrawCostRow(dialog.transform, 118f, EconomyConstants.ScrapDefId, scrapNeed, scrapHave);
            DrawCreditRow(dialog.transform, 178f, creditNeed, creditHave);

            var status = NexusUiFactory.CreateText(
                dialog.transform, "Status",
                canAfford ? "" : UiText.ShipModuleCannotAfford,
                new Vector2(24f, 236f), new Vector2(DialogWidth - 48f, 22f), 12f, NexusTheme.Red);

            string captured = def.moduleId;
            var confirm = NexusUiFactory.CreateButton(
                dialog.transform, "Confirm",
                canAfford ? UiText.ShipModuleConfirmUpgrade : UiText.ShipModuleCannotAfford,
                new Vector2(24f, 270f), new Vector2(220f, 44f),
                () =>
                {
                    if (!ShipService.CanAffordModuleUpgrade(captured))
                    {
                        status.text = UiText.ShipModuleCannotAfford;
                        return;
                    }

                    var result = ShipService.TryUpgradeModule(captured);
                    Debug.Log("[SHIP] module upgrade popup: " + (result.Success ? "ok" : result.Message));
                    if (!result.Success)
                    {
                        status.text = string.IsNullOrEmpty(result.Message)
                            ? UiText.ShipModuleCannotAfford
                            : result.Message;
                        return;
                    }

                    Close();
                    onUpgraded?.Invoke();
                },
                canAfford
                    ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f)
                    : NexusTheme.SurfaceRaised,
                canAfford ? NexusTheme.Gold : NexusTheme.DimText,
                14f);
            confirm.interactable = canAfford;

            NexusUiFactory.CreateButton(
                dialog.transform, "Cancel", UiText.Close,
                new Vector2(260f, 270f), new Vector2(160f, 44f),
                Close,
                NexusTheme.SurfaceRaised, NexusTheme.MutedText, 14f);
        }

        private static void DrawCostRow(Transform parent, float y, string itemDefId, int need, int have)
        {
            var def = ItemCatalog.Get(itemDefId);
            var box = NexusUiFactory.CreateBox(
                parent, "Cost " + itemDefId,
                new Vector2(24f, y), new Vector2(DialogWidth - 48f, 52f),
                NexusTheme.SurfaceRaised, NexusTheme.BorderSoft);
            box.GetComponent<Image>().raycastTarget = true;

            var iconName = ItemFactory.ResolveIcon(def?.icon);
            var sprite = ImageUtil.GetSpriteByName(ImageUtil.itemImagePath, iconName ?? "Steel");
            NexusUiFactory.CreateIcon(
                box.transform, "Icon", sprite,
                new Vector2(10f, 6f), new Vector2(40f, 40f), Color.white);

            string name = def != null ? UiText.ItemName(def) : itemDefId;
            Color qtyColor = have >= need ? NexusTheme.Text : NexusTheme.Red;
            NexusUiFactory.CreateText(
                box.transform, "Label",
                $"{name}   {have}/{need}",
                new Vector2(60f, 14f), new Vector2(380f, 24f), 14f, qtyColor);

            var anchor = box.AddComponent<ItemTooltipAnchor>();
            anchor.Def = def;
        }

        private static void DrawCreditRow(Transform parent, float y, int need, int have)
        {
            var box = NexusUiFactory.CreateBox(
                parent, "Cost Credits",
                new Vector2(24f, y), new Vector2(DialogWidth - 48f, 52f),
                NexusTheme.SurfaceRaised, NexusTheme.BorderSoft);
            NexusUiFactory.CreateText(
                box.transform, "Label",
                UiText.ShipModuleCreditCost(have, need),
                new Vector2(16f, 14f), new Vector2(440f, 24f), 14f,
                have >= need ? NexusTheme.Text : NexusTheme.Red);
        }

        private static int CountScrap()
        {
            var items = ProductionService.GetLocalItems();
            foreach (var item in items)
                ItemFactory.NormalizeLegacy(item);
            return InventoryRules.CountOf(ItemFactory.ToStacks(items), EconomyConstants.ScrapDefId, 1);
        }
    }
}

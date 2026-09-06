using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Onboarding.Domain;
using Assets.Resources.Scripts.UI.Nexus.Tutorial;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Manual crafting: unlocked item list with filters, one in-flight batch, no fleet.</summary>
    internal sealed class CraftingScreen
    {
        private readonly Transform root;
        private Transform listHost;
        private Transform detailHost;
        private string selectedRecipeId = "";
        private string searchQuery = "";
        private int categoryFilter = -1;
        private int qualityFilter;
        private int levelFilter;
        private string lastStatus = "";

        private CraftingScreen(Transform root)
        {
            this.root = root;
        }

        public GameObject Root => root.gameObject;

        public static CraftingScreen Build(Transform parent)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Crafting Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new CraftingScreen(panel.transform);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            TutorialGuideService.UnregisterAnchor("crafting_start");
            var staleWatcher = root.GetComponent<CraftingJobWatcher>();
            if (staleWatcher != null)
                UnityEngine.Object.DestroyImmediate(staleWatcher);
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);

            IdleSettlementService.EnsureLoaded();
            ShipService.EnsureReady();
            ProductionService.SettleManualOnline();

            if (string.IsNullOrEmpty(selectedRecipeId)
                && OnboardingService.ActiveStep?.stepId == OnboardingCatalog.StepCraft)
            {
                foreach (var recipe in UnlockedRecipes())
                {
                    selectedRecipeId = recipe.recipeId;
                    break;
                }
            }

            NexusUiFactory.CreateText(
                root, "Title", UiText.CraftingTitle,
                new Vector2(28f, 16f), new Vector2(600f, 32f), 22f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                root, "Hint", UiText.CraftingHint,
                new Vector2(28f, 48f), new Vector2(1600f, 22f), 12f, NexusTheme.MutedText);
            OnboardingBanner.TryDraw(root, AppScreen.Crafting, new Vector2(28f, 70f));

            BuildFilters();
            listHost = CreateScrollContent(root, new Vector2(28f, 232f), new Vector2(520f, 560f));
            detailHost = NexusUiFactory.CreateBox(
                root, "Detail", new Vector2(568f, 88f), new Vector2(1120f, 704f),
                NexusTheme.Surface, NexusTheme.BorderSoft).transform;

            RefreshList();
            RefreshDetail();

            var watcher = root.gameObject.AddComponent<CraftingJobWatcher>();
            watcher.Bind(RefreshAfterJobChange);
        }

        private void RefreshAfterJobChange()
        {
            if (root == null) return;
            RefreshList();
            RefreshDetail();
        }

        private void BuildFilters()
        {
            var search = NexusUiFactory.CreateInputField(
                root, "Search", UiText.CraftingSearch,
                new Vector2(28f, 88f), new Vector2(520f, 36f), 13);
            search.text = searchQuery ?? "";
            search.onValueChanged.AddListener(value =>
            {
                searchQuery = value ?? "";
                RefreshList();
            });

            DrawFilterChip(28f, 132f, 80f, categoryFilter < 0, UiText.CraftingFilterAll, () => SetCategory(-1));
            var cats = new[]
            {
                ItemCategory.Material, ItemCategory.Intermediate, ItemCategory.Consumable,
                ItemCategory.Equipment, ItemCategory.ShipModule
            };
            for (var i = 0; i < cats.Length; i++)
            {
                var captured = (int)cats[i];
                DrawFilterChip(
                    112f + i * 88f, 132f, 84f, categoryFilter == captured,
                    UiText.ItemCategoryLabel(cats[i]),
                    () => SetCategory(captured));
            }

            DrawFilterChip(28f, 170f, 80f, qualityFilter == 0, UiText.InventoryQualityAll, () => SetQuality(0));
            var grades = new[] { 1, 2, 3, 4, 5 };
            for (var i = 0; i < grades.Length; i++)
            {
                var q = grades[i];
                DrawFilterChip(
                    112f + i * 88f, 170f, 84f, qualityFilter == q,
                    UiText.QualityGradeLetter(q),
                    () => SetQuality(q),
                    NexusTheme.QualityGradeColor(q));
            }

            DrawFilterChip(28f, 208f, 80f, levelFilter == 0, UiText.CraftingLevelAll, () => SetLevel(0));
            for (var lv = 1; lv <= 3; lv++)
            {
                var captured = lv;
                DrawFilterChip(
                    112f + (lv - 1) * 88f, 208f, 84f, levelFilter == captured,
                    UiText.CraftingLevel(captured),
                    () => SetLevel(captured));
            }
        }

        private void SetCategory(int value)
        {
            categoryFilter = value;
            Rebuild();
        }

        private void SetQuality(int value)
        {
            qualityFilter = value;
            Rebuild();
        }

        private void SetLevel(int value)
        {
            levelFilter = value;
            Rebuild();
        }

        private void DrawFilterChip(
            float x, float y, float width, bool selected, string label, UnityEngine.Events.UnityAction onClick,
            Color? labelColor = null)
        {
            NexusUiFactory.CreateButton(
                root, "Filter " + label + x + y, label,
                new Vector2(x, y), new Vector2(width, 30f),
                onClick,
                selected ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f) : NexusTheme.SurfaceRaised,
                selected ? NexusTheme.Gold : labelColor ?? NexusTheme.Text,
                11f);
        }

        private void RefreshList()
        {
            if (listHost == null) return;
            for (int i = listHost.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(listHost.GetChild(i).gameObject);

            var visible = VisibleRecipes();
            if (visible.Count == 0)
            {
                NexusUiFactory.CreateText(
                    listHost, "Empty", UiText.CraftingSelectRecipe,
                    new Vector2(8f, 8f), new Vector2(480f, 28f), 13f, NexusTheme.MutedText);
                var emptyRect = listHost.GetComponent<RectTransform>();
                emptyRect.sizeDelta = new Vector2(0f, 48f);
                return;
            }

            if (string.IsNullOrEmpty(selectedRecipeId))
                selectedRecipeId = visible[0].recipeId;

            const float rowH = 64f;
            for (var i = 0; i < visible.Count; i++)
                DrawListItem(visible[i], 6f + i * rowH);

            var contentRect = listHost.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0f, 12f + visible.Count * rowH);
        }

        private void DrawListItem(RecipeDef recipe, float y)
        {
            var def = ItemCatalog.Get(recipe.outputDefId);
            var name = def != null ? UiText.ItemName(def) : UiText.T(recipe.displayNameEn, recipe.displayNameZh);
            var quality = ProductionService.PreviewQualityFloor(recipe.recipeId);
            var selected = recipe.recipeId == selectedRecipeId;
            var crafting = ProductionService.ActiveManualJob?.recipeId == recipe.recipeId;
            var captured = recipe.recipeId;

            var box = NexusUiFactory.CreateBox(
                listHost, "Item " + captured,
                new Vector2(8f, y), new Vector2(492f, 58f),
                selected ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f) : NexusTheme.SurfaceRaised,
                selected ? NexusTheme.Gold : crafting ? NexusTheme.Cyan : NexusTheme.BorderSoft);
            var image = box.GetComponent<Image>();
            image.raycastTarget = true;
            var button = box.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                selectedRecipeId = captured;
                RefreshList();
                RefreshDetail();
            });

            var iconName = ItemFactory.ResolveIcon(def?.icon);
            var sprite = ImageUtil.GetSpriteByName(ImageUtil.itemImagePath, iconName ?? "Steel");
            NexusUiFactory.CreateIcon(
                box.transform, "Icon", sprite,
                new Vector2(8f, 9f), new Vector2(40f, 40f), Color.white);

            NexusUiFactory.CreateText(
                box.transform, "Name", name,
                new Vector2(56f, 8f), new Vector2(300f, 24f), 15f,
                NexusTheme.QualityGradeColor(quality),
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                box.transform, "Level",
                UiText.CraftingLevel(Mathf.Max(1, recipe.requiredLineTier)),
                new Vector2(56f, 32f), new Vector2(200f, 18f), 12f, NexusTheme.MutedText);

            NexusUiFactory.CreateText(
                box.transform, "Grade",
                UiText.QualityGradeLetter(quality),
                new Vector2(400f, 16f), new Vector2(80f, 24f), 16f,
                NexusTheme.QualityGradeColor(quality),
                TextAlignmentOptions.Right, FontStyles.Bold);
        }

        private void RefreshDetail()
        {
            if (detailHost == null) return;
            TutorialGuideService.UnregisterAnchor("crafting_start");
            for (int i = detailHost.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(detailHost.GetChild(i).gameObject);

            var recipe = RecipeCatalog.Get(selectedRecipeId);
            if (recipe == null)
            {
                NexusUiFactory.CreateText(
                    detailHost, "Empty", UiText.CraftingSelectRecipe,
                    new Vector2(24f, 24f), new Vector2(800f, 40f), 14f, NexusTheme.MutedText);
                return;
            }

            var outDef = ItemCatalog.Get(recipe.outputDefId);
            var outputName = outDef != null
                ? UiText.ItemName(outDef)
                : UiText.T(recipe.displayNameEn, recipe.displayNameZh);
            var quality = ProductionService.PreviewQualityFloor(recipe.recipeId);

            var iconName = ItemFactory.ResolveIcon(outDef?.icon);
            var sprite = ImageUtil.GetSpriteByName(ImageUtil.itemImagePath, iconName ?? "Steel");
            NexusUiFactory.CreateIcon(
                detailHost, "Icon", sprite,
                new Vector2(24f, 20f), new Vector2(56f, 56f), Color.white);

            NexusUiFactory.CreateText(
                detailHost, "Name", outputName,
                new Vector2(96f, 20f), new Vector2(700f, 32f), 20f,
                NexusTheme.QualityGradeColor(quality),
                TextAlignmentOptions.Left, FontStyles.Bold);
            NexusUiFactory.CreateText(
                detailHost, "Meta",
                $"{UiText.ItemCategoryLabel(outDef?.category ?? ItemCategory.Material)}  ·  " +
                $"{UiText.CraftingLevel(Mathf.Max(1, recipe.requiredLineTier))}  ·  " +
                UiText.QualityGradeLetter(quality),
                new Vector2(96f, 54f), new Vector2(700f, 22f), 13f, NexusTheme.MutedText);

            var body = new System.Text.StringBuilder();
            body.AppendLine(UiText.CraftingInputs);
            var inputIndex = 0;
            foreach (var input in recipe.inputs)
            {
                if (input == null) continue;
                var have = CountLocal(input.itemDefId, input.minQuality);
                var inDef = ItemCatalog.Get(input.itemDefId);
                var inputName = inDef != null ? UiText.ItemName(inDef) : input.itemDefId;
                body.AppendLine($"  · {inputName} x{input.quantity} (Q≥{input.minQuality})  {have}");
                if (have < input.quantity)
                    ShortageJump.DrawItem(detailHost, "Gap" + input.itemDefId, 100f + inputIndex * 22f, inDef, input.quantity, have);
                inputIndex++;
            }

            body.AppendLine();
            body.AppendLine(UiText.CraftingOutputs);
            body.AppendLine($"  · {outputName} x{recipe.outputQty}");
            body.AppendLine(UiText.CraftingExpectedQuality(ProductionService.PreviewQuality(recipe.recipeId)));
            var cycle = Mathf.Max(1, Mathf.RoundToInt(ProductionService.ResolveManualCycleSeconds(recipe)));
            body.AppendLine(UiText.T(
                $"Cycle: ~{cycle}s per batch (base {recipe.cycleSeconds}s — higher tiers take longer)",
                $"制造周期：约 {cycle} 秒/批（基础 {recipe.cycleSeconds} 秒，越高级耗时越久）"));

            var text = NexusUiFactory.CreateText(
                detailHost, "Body", body.ToString(),
                new Vector2(24f, 96f), new Vector2(1070f, 280f), 13f, NexusTheme.MutedText);
            text.textWrappingMode = TextWrappingModes.Normal;

            if (!string.IsNullOrEmpty(lastStatus))
            {
                NexusUiFactory.CreateText(
                    detailHost, "Status", lastStatus,
                    new Vector2(24f, 390f), new Vector2(1070f, 22f), 12f, NexusTheme.Cyan);
            }

            var job = ProductionService.ActiveManualJob;
            var busy = job != null;
            var viewingActive = busy && job.recipeId == recipe.recipeId;
            var startEnabled = !busy;
            var stopEnabled = viewingActive;

            var start = NexusUiFactory.CreateRoleButton(
                detailHost, "Start", UiText.CraftingStart,
                new Vector2(24f, 430f), new Vector2(220f, 48f),
                () =>
                {
                    if (!startEnabled) return;
                    var r = ProductionService.TryStartRecipe(recipe.recipeId);
                    lastStatus = r.Success
                        ? UiText.T(
                            $"Crafting started — {recipe.outputQty}× {outputName}.",
                            $"已开始制造 — {recipe.outputQty}× {outputName}。")
                        : r.Message;
                    if (!r.Success)
                        NexusSnackbar.Show(r.Message);
                    RefreshList();
                    RefreshDetail();
                },
                startEnabled ? NexusButtonRole.Primary : NexusButtonRole.Disabled,
                14f);
            TutorialGuideService.RegisterAnchor(
                "crafting_start",
                start.GetComponent<RectTransform>(),
                start);

            NexusUiFactory.CreateRoleButton(
                detailHost, "Stop", UiText.StopAction,
                new Vector2(260f, 430f), new Vector2(220f, 48f),
                () =>
                {
                    if (!stopEnabled) return;
                    ShowStopConfirm();
                },
                stopEnabled ? NexusButtonRole.Danger : NexusButtonRole.Disabled,
                14f);

            NexusTimedBar.Attach(
                detailHost, "CraftBar", new Vector2(24f, 494f), 1070f, 28f,
                ProductionService.ManualProgressRatio,
                ProgressLabel);

            NexusUiFactory.CreateButton(
                detailHost, "AutoRepair", UiText.CraftingAutoRepair,
                new Vector2(24f, 540f), new Vector2(220f, 40f),
                () =>
                {
                    DurabilityService.TryAutoRepairAll();
                    RefreshDetail();
                },
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan, 12f);
            NexusUiFactory.CreateButton(
                detailHost, "RepairFirst", UiText.CraftingRepairFirst,
                new Vector2(260f, 540f), new Vector2(220f, 40f),
                () =>
                {
                    foreach (var item in ProductionService.GetLocalItems())
                    {
                        if (item == null || string.IsNullOrEmpty(item.itemInstanceId)) continue;
                        if (item.durability >= item.maxDurability) continue;
                        if (DurabilityService.TryRepairInstance(item.itemInstanceId))
                            break;
                    }

                    RefreshDetail();
                },
                NexusTheme.SurfaceRaised, NexusTheme.Text, 12f);
        }

        private static string ProgressLabel()
        {
            var job = ProductionService.ActiveManualJob;
            if (job == null)
                return UiText.CraftingProgressIdle;
            if (job.state == ManualCraftState.BlockedFull)
                return UiText.CraftingBlockedFull;

            var recipe = RecipeCatalog.Get(job.recipeId);
            var cycle = ProductionService.ResolveManualCycleSeconds(recipe);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var last = job.lastSettledAtUtc > 0 ? job.lastSettledAtUtc : now;
            var remain = Mathf.Max(0, Mathf.CeilToInt(cycle - (now - last)));
            var def = recipe != null ? ItemCatalog.Get(recipe.outputDefId) : null;
            var name = def != null
                ? UiText.ItemName(def)
                : recipe != null ? UiText.T(recipe.displayNameEn, recipe.displayNameZh) : "";
            return string.IsNullOrEmpty(name)
                ? UiText.CraftingProgressEta(remain)
                : $"{name}  ·  {UiText.CraftingProgressEta(remain)}";
        }

        private void ShowStopConfirm()
        {
            var overlay = NexusUiFactory.CreatePanel(
                root, "StopCraftOverlay", NexusTheme.WithAlpha(Color.black, 0.62f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            overlay.transform.SetAsLastSibling();

            const float w = 520f;
            const float h = 260f;
            var dialog = NexusUiFactory.CreateBox(
                overlay.transform, "Dialog", Vector2.zero, new Vector2(w, h),
                NexusTheme.Surface, NexusTheme.Gold);
            var rect = dialog.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(w, h);
            dialog.GetComponent<Image>().raycastTarget = true;

            NexusUiFactory.CreateText(
                dialog.transform, "Title", UiText.CraftingStopConfirmTitle,
                new Vector2(24f, 18f), new Vector2(w - 48f, 28f), 16f, NexusTheme.Gold,
                TextAlignmentOptions.Left, FontStyles.Bold);
            var body = NexusUiFactory.CreateText(
                dialog.transform, "Body", UiText.CraftingStopConfirmBody,
                new Vector2(24f, 56f), new Vector2(w - 48f, 90f), 13f, NexusTheme.MutedText);
            body.textWrappingMode = TextWrappingModes.Normal;

            NexusUiFactory.CreateButton(
                dialog.transform, "Cancel", UiText.Close,
                new Vector2(24f, 188f), new Vector2(200f, 44f),
                () => UnityEngine.Object.Destroy(overlay),
                NexusTheme.SurfaceRaised, NexusTheme.Text, 14f);
            NexusUiFactory.CreateButton(
                dialog.transform, "Confirm", UiText.CraftingConfirmStop,
                new Vector2(296f, 188f), new Vector2(200f, 44f),
                () =>
                {
                    var r = ProductionService.TryStopManualCraft();
                    UnityEngine.Object.Destroy(overlay);
                    lastStatus = r.Success
                        ? UiText.T("Crafting stopped.", "已停止制造。")
                        : r.Message;
                    RefreshList();
                    RefreshDetail();
                },
                NexusTheme.WithAlpha(NexusTheme.Red, 0.22f), NexusTheme.Red, 14f);
        }

        private List<RecipeDef> VisibleRecipes()
        {
            var list = new List<RecipeDef>();
            var query = (searchQuery ?? "").Trim();
            foreach (var recipe in UnlockedRecipes())
            {
                var def = ItemCatalog.Get(recipe.outputDefId);
                if (categoryFilter >= 0 && (int)(def?.category ?? ItemCategory.Material) != categoryFilter)
                    continue;
                if (qualityFilter > 0 && ProductionService.PreviewQualityFloor(recipe.recipeId) != qualityFilter)
                    continue;
                if (levelFilter > 0 && Mathf.Max(1, recipe.requiredLineTier) != levelFilter)
                    continue;
                if (!MatchesSearch(recipe, def, query))
                    continue;
                list.Add(recipe);
            }

            return list;
        }

        private static IEnumerable<RecipeDef> UnlockedRecipes()
        {
            foreach (var recipe in RecipeCatalog.All)
            {
                if (recipe != null && ProductionService.IsManualRecipeUnlocked(recipe))
                    yield return recipe;
            }
        }

        private static bool MatchesSearch(RecipeDef recipe, ItemDef def, string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            return Contains(recipe.displayNameEn, query)
                   || Contains(recipe.displayNameZh, query)
                   || Contains(def?.displayNameEn, query)
                   || Contains(def?.displayNameZh, query)
                   || Contains(recipe.outputDefId, query);
        }

        private static bool Contains(string source, string query) =>
            !string.IsNullOrEmpty(source)
            && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        private static int CountLocal(string defId, int minQ) =>
            InventoryRules.CountOf(ItemFactory.ToStacks(ProductionService.GetLocalItems()), defId, minQ);

        private static Transform CreateScrollContent(Transform parent, Vector2 position, Vector2 size)
        {
            var viewport = new GameObject(
                "Craft List", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
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

        private sealed class CraftingJobWatcher : MonoBehaviour
        {
            private string lastSig = "";
            private Action onChanged;

            public void Bind(Action callback)
            {
                onChanged = callback;
                lastSig = Signature();
            }

            private void Update()
            {
                var sig = Signature();
                if (sig == lastSig) return;
                lastSig = sig;
                onChanged?.Invoke();
            }

            private static string Signature()
            {
                var job = ProductionService.ActiveManualJob;
                return job == null ? "" : job.recipeId + "/" + (int)job.state + "/" + job.lastSettledAtUtc;
            }
        }
    }
}

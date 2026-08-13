using System.Collections.Generic;

namespace Assets.Resources.Scripts.Onboarding.Domain
{
    /// <summary>Static onboarding_v1 step table (systems/10).</summary>
    public static class OnboardingCatalog
    {
        public const string ChainId = "onboarding_v1";
        public const string StepFormation = "ob_formation";
        public const string StepFirstBattle = "ob_first_battle";
        public const string StepGather = "ob_gather";
        public const string StepCraft = "ob_craft";
        public const string StepNpcShop = "ob_npc_shop";

        private static readonly List<OnboardingStepDef> Steps = new List<OnboardingStepDef>
        {
            new OnboardingStepDef
            {
                stepId = StepFormation,
                order = 1,
                titleEn = "Form a Deck",
                titleZh = "编成首支队伍",
                hintEn = "Open Formation and put at least one card into an unlocked deck.",
                hintZh = "打开编队，向任意已解锁卡组放入至少 1 名角色。",
                targetScreen = "Formation",
                rewards = new[]
                {
                    new OnboardingRewardGrant { kind = "credits", credits = 50 }
                }
            },
            new OnboardingStepDef
            {
                stepId = StepFirstBattle,
                order = 2,
                titleEn = "First Battle",
                titleZh = "完成首场区域战",
                hintEn = "From Explore, challenge a region and win (AFK farm does not count).",
                hintZh = "从探索发起区域挑战并获胜（挂机刷取不算）。",
                targetScreen = "Battle",
                rewards = new[]
                {
                    new OnboardingRewardGrant
                    {
                        kind = "item", itemDefId = "mat_scrap", quantity = 10, quality = 2
                    }
                }
            },
            new OnboardingStepDef
            {
                stepId = StepGather,
                order = 3,
                titleEn = "Start Gathering",
                titleZh = "开始采集",
                hintEn = "Start a Gather action from Explore, or obtain gather loot.",
                hintZh = "在探索开始采集行动，或获得任意采集产物。",
                targetScreen = "Battle",
                rewards = new[]
                {
                    new OnboardingRewardGrant
                    {
                        kind = "item", itemDefId = "mat_iron_ore", quantity = 6, quality = 2
                    }
                }
            },
            new OnboardingStepDef
            {
                stepId = StepCraft,
                order = 4,
                titleEn = "Craft Once",
                titleZh = "完成一次制造",
                hintEn = "Open Crafting and successfully start a recipe (output goes to Inventory).",
                hintZh = "打开制造并成功开始一次配方（产出会进入仓库）。",
                targetScreen = "Crafting",
                rewards = new[]
                {
                    new OnboardingRewardGrant
                    {
                        kind = "item", itemDefId = "con_repair_kit", quantity = 1, quality = 2
                    }
                }
            },
            new OnboardingStepDef
            {
                stepId = StepNpcShop,
                order = 5,
                titleEn = "Visit Starport",
                titleZh = "星港购或售",
                hintEn = "Buy or sell once at the Starport NPC shop.",
                hintZh = "在星港 NPC 商店完成一次购买或出售。",
                targetScreen = "Market",
                rewards = new[]
                {
                    new OnboardingRewardGrant { kind = "credits", credits = 100 }
                }
            }
        };

        public static IReadOnlyList<OnboardingStepDef> All => Steps;

        public static OnboardingStepDef Get(string stepId)
        {
            if (string.IsNullOrEmpty(stepId)) return null;
            foreach (var s in Steps)
            {
                if (s != null && s.stepId == stepId)
                    return s;
            }

            return null;
        }

        public static OnboardingStepDef NextAfter(string stepId)
        {
            var current = Get(stepId);
            if (current == null) return null;
            foreach (var s in Steps)
            {
                if (s != null && s.order == current.order + 1)
                    return s;
            }

            return null;
        }
    }
}

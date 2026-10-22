using System.Collections.Generic;

namespace Assets.Resources.Scripts.ChapterQuest.Domain
{
    public static class ChapterQuestCatalog
    {
        public const string ChapterId = "chapter_v1";
        public const string StepPrologue = "ch1_prologue";
        public const string StepRecruitCole = "ch1_recruit_cole";
        public const string StepFormation = "ch1_formation";
        public const string StepOuterCleanup = "ch1_outer_cleanup";
        public const string StepScanSignal = "ch1_scan_signal";
        public const string StepMiningSpur = "ch1_mining_spur";
        public const string StepIdleGather = "ch1_idle_gather";
        public const string StepHullLeak = "ch1_hull_leak";
        public const string StepTwoRoutes = "ch1_two_routes";
        public const string StepExpeditionPrep = "ch1_expedition_prep";
        public const string StepOpenTheLock = "ch1_open_the_lock";

        private static readonly List<ChapterQuestStepDef> Steps = new List<ChapterQuestStepDef>
        {
            new ChapterQuestStepDef
            {
                stepId = StepPrologue,
                order = 0,
                titleEn = "Runaway Return",
                titleZh = "失控的返航",
                hintEn = "Survive the sweep drone attack and dock at Outer Haven.",
                hintZh = "击退清扫无人机，在外缘港临时停靠。",
                targetScreen = "Battle",
                dialogueIdOnStart = "ch1_wake",
                dialogueIdOnClaim = "ch1_prologue_clear",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 120 },
                    new ChapterQuestRewardGrant { kind = "item", itemDefId = "con_recruit_ticket", quantity = 1, quality = 1 }
                }
            },
            new ChapterQuestStepDef
            {
                stepId = StepRecruitCole,
                order = 1,
                titleEn = "Failed Personnel Beacon",
                titleZh = "失效的人员信标",
                hintEn = "Use emergency recruit — Cole is guaranteed on your first pull.",
                hintZh = "使用紧急招募，首次必得科尔。",
                targetScreen = "Recruit",
                dialogueIdOnStart = "ch1_mita_recruit",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 80 }
                }
            },
            new ChapterQuestStepDef
            {
                stepId = StepFormation,
                order = 2,
                titleEn = "Patchwork Fleet",
                titleZh = "拼凑出的舰队",
                hintEn = "Fight once with Asra in the back, then move Cole to the front row.",
                hintZh = "阿斯拉先站后排打一场，再把科尔调到前排。",
                targetScreen = "Formation",
                dialogueIdOnStart = "ch1_asra_formation",
                linkedOnboardingStepId = "ob_formation",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 100 }
                }
            },
            new ChapterQuestStepDef
            {
                stepId = StepOuterCleanup,
                order = 3,
                titleEn = "Outer Haven Cleanup Order",
                titleZh = "外缘港清理令",
                hintEn = "Win the Outer Belt battle with your adjusted formation.",
                hintZh = "用调整后的编队赢得外缘带战斗。",
                targetScreen = "Battle",
                linkedOnboardingStepId = "ob_first_battle",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 150 },
                    new ChapterQuestRewardGrant { kind = "item", itemDefId = "mat_scrap", quantity = 15, quality = 2 }
                }
            },
            new ChapterQuestStepDef
            {
                stepId = StepScanSignal,
                order = 4,
                titleEn = "Periodic Signal in the Fog",
                titleZh = "熵雾中的周期信号",
                hintEn = "Probe the mining spur signal on Explore.",
                hintZh = "在探索界面探测矿脉支线信号。",
                targetScreen = "Battle",
                dialogueIdOnStart = "ch1_sernia_signal",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 80 }
                }
            },
            new ChapterQuestStepDef
            {
                stepId = StepMiningSpur,
                order = 5,
                titleEn = "Abandoned Mine",
                titleZh = "无人矿场",
                hintEn = "Clear Mining Spur — Magki's armor break shines here.",
                hintZh = "清剿矿脉支线，发挥玛吉破甲优势。",
                targetScreen = "Battle",
                dialogueIdOnStart = "ch1_magki_armor",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 200 },
                    new ChapterQuestRewardGrant { kind = "item", itemDefId = "mat_iron", quantity = 20, quality = 2 }
                }
            },
            new ChapterQuestStepDef
            {
                stepId = StepIdleGather,
                order = 6,
                titleEn = "Let Machines Work",
                titleZh = "让机器替我们工作",
                hintEn = "Complete at least one gather cycle on an unlocked node.",
                hintZh = "在已解锁采集的节点完成至少一个采集周期。",
                targetScreen = "Battle",
                linkedOnboardingStepId = "ob_gather",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 80 },
                    new ChapterQuestRewardGrant { kind = "item", itemDefId = "mat_iron_ore", quantity = 8, quality = 2 }
                }
            },
            new ChapterQuestStepDef
            {
                stepId = StepHullLeak,
                order = 7,
                titleEn = "Hull Still Leaking",
                titleZh = "舰体仍在漏气",
                hintEn = "Craft a repair kit so one reaches the warehouse.",
                hintZh = "制造 1 个维修包并使其进入仓库。",
                targetScreen = "Crafting",
                linkedOnboardingStepId = "ob_craft",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 100 },
                    new ChapterQuestRewardGrant { kind = "item", itemDefId = "con_repair_kit", quantity = 1, quality = 2 }
                }
            },
            new ChapterQuestStepDef
            {
                stepId = StepTwoRoutes,
                order = 8,
                titleEn = "Two Routes",
                titleZh = "两条航路",
                hintEn = "Probe Convoy Lane and Quantum Rift, then choose which to emphasize.",
                hintZh = "探测护航航道与量子裂隙，并选择先强调的一条。",
                targetScreen = "Battle",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 120 }
                }
            },
            new ChapterQuestStepDef
            {
                stepId = StepExpeditionPrep,
                order = 9,
                titleEn = "Prep the Push",
                titleZh = "为远征准备补给",
                hintEn = "Unlock a second deck or meet the next ship gate, and claim pending loot once.",
                hintZh = "解锁第二卡组或满足下一区舰船门，并领取一次挂机待领。",
                targetScreen = "Formation",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 150 },
                    new ChapterQuestRewardGrant { kind = "item", itemDefId = "mat_scrap", quantity = 20, quality = 2 }
                }
            },
            new ChapterQuestStepDef
            {
                stepId = StepOpenTheLock,
                order = 10,
                titleEn = "You Opened a Lock",
                titleZh = "你打开了一道锁",
                hintEn = "Defeat the Frontier Anchor boss and restore the beacon.",
                hintZh = "击败边境锚点 Boss，恢复航标。",
                targetScreen = "Battle",
                rewards = new[]
                {
                    new ChapterQuestRewardGrant { kind = "credits", credits = 400 },
                    new ChapterQuestRewardGrant { kind = "item", itemDefId = "con_recruit_ticket", quantity = 2, quality = 1 }
                }
            }
        };

        public static IReadOnlyList<ChapterQuestStepDef> All => Steps;

        public static ChapterQuestStepDef Get(string stepId)
        {
            if (string.IsNullOrEmpty(stepId)) return null;
            foreach (var s in Steps)
            {
                if (s != null && s.stepId == stepId)
                    return s;
            }

            return null;
        }

        public static ChapterQuestStepDef NextAfter(string stepId)
        {
            ChapterQuestStepDef found = null;
            foreach (var s in Steps)
            {
                if (s == null) continue;
                if (found != null)
                    return s;
                if (s.stepId == stepId)
                    found = s;
            }

            return null;
        }
    }
}

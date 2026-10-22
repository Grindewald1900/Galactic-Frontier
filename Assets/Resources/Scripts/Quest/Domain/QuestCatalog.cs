using System.Collections.Generic;
using Assets.Resources.Scripts.World.Domain;

namespace Assets.Resources.Scripts.Quest.Domain
{
    public static class QuestCatalog
    {
        private static readonly List<QuestDef> Steps = Build();

        public static IReadOnlyList<QuestDef> All => Steps;

        public static QuestDef Get(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            foreach (var def in Steps)
            {
                if (def != null && def.questId == questId)
                    return def;
            }

            return null;
        }

        public static IEnumerable<QuestDef> OfType(QuestType type)
        {
            foreach (var def in Steps)
            {
                if (def != null && def.type == type)
                    yield return def;
            }
        }

        private static List<QuestDef> Build()
        {
            var list = new List<QuestDef>(51);
            list.Add(Ob(QuestIds.ObFormation, "OB-01", 1, "Form a Deck", "编成首支队伍",
                "Open Formation and put at least one card into an unlocked deck.",
                "打开编队，向任意已解锁卡组放入至少 1 名角色。",
                "Formation", null, Credit(50)));
            list.Add(Ob(QuestIds.ObFirstBattle, "OB-02", 2, "First Battle", "完成首场区域战",
                "From Explore, challenge a region and win (AFK farm does not count).",
                "从探索发起区域挑战并获胜（挂机刷取不算）。",
                "Battle", new[] { QuestIds.ObFormation }, Item("mat_scrap", 10)));
            list.Add(Ob(QuestIds.ObGather, "OB-03", 3, "Start Gathering", "开始采集",
                "Start a Gather action from Explore.",
                "在探索开始一次采集行动。",
                "Battle", new[] { QuestIds.ObFirstBattle }, Item("mat_iron_ore", 6)));
            list.Add(Ob(QuestIds.ObCraft, "OB-04", 4, "Craft Once", "完成一次制造",
                "Open Crafting and successfully start a recipe.",
                "打开制造并成功开始一次配方。",
                "Crafting", new[] { QuestIds.ObGather }, Item("con_repair_kit", 1)));
            list.Add(Ob(QuestIds.ObNpcShop, "OB-05", 5, "Visit Starport", "星港购或售",
                "Buy or sell once at the Starport NPC shop.",
                "在星港 NPC 商店完成一次购买或出售。",
                "Market", new[] { QuestIds.ObCraft }, Credit(100)));

            list.Add(Story(QuestIds.ChPrologue, "CH-00", 0, "Runaway Return", "失控的返航",
                "Survive the sweep drone attack and dock at Outer Haven.",
                "击退清扫无人机，在外缘港临时停靠。",
                "Battle", null, "", "ch1_wake", "ch1_prologue_clear",
                Credit(120), Item("con_recruit_ticket", 1, 1)));
            list.Add(Story(QuestIds.ChRecruitCole, "CH-01", 1, "Failed Personnel Beacon", "失效的人员信标",
                "Use emergency recruit — Cole is guaranteed on your first pull.",
                "使用紧急招募，首次必得科尔。",
                "Recruit", new[] { QuestIds.ChPrologue }, "", "ch1_mita_recruit", "",
                Credit(80)));
            list.Add(Story(QuestIds.ChFormation, "CH-02", 2, "Patchwork Fleet", "拼凑出的舰队",
                "Fight once with Asra in the back, then move Cole to the front row.",
                "阿斯拉先站后排打一场，再把科尔调到前排。",
                "Formation", new[] { QuestIds.ChRecruitCole }, QuestIds.ObFormation, "ch1_asra_formation", "",
                Credit(100)));
            list.Add(Story(QuestIds.ChOuterCleanup, "CH-03", 3, "Outer Haven Cleanup Order", "外缘港清理令",
                "Win the Outer Belt battle with your adjusted formation.",
                "用调整后的编队赢得外缘带战斗。",
                "Battle", new[] { QuestIds.ChFormation }, QuestIds.ObFirstBattle, "", "",
                Credit(150), Item("mat_scrap", 15)));
            list.Add(Story(QuestIds.ChScanSignal, "CH-04", 4, "Periodic Signal in the Fog", "熵雾中的周期信号",
                "Probe the mining spur signal on Explore.",
                "在探索界面探测矿脉支线信号。",
                "Battle", new[] { QuestIds.ChOuterCleanup }, "", "ch1_sernia_signal", "",
                Credit(80)));
            list.Add(Story(QuestIds.ChMiningSpur, "CH-05", 5, "Abandoned Mine", "无人矿场",
                "Clear Mining Spur — Magki's armor break shines here.",
                "清剿矿脉支线，发挥玛吉破甲优势。",
                "Battle", new[] { QuestIds.ChScanSignal }, "", "ch1_magki_armor", "",
                Credit(200), Item("mat_iron", 20)));
            list.Add(Story(QuestIds.ChIdleGather, "CH-06", 6, "Let Machines Work", "让机器替我们工作",
                "Complete at least one gather cycle on an unlocked node.",
                "在已解锁采集的节点完成至少一个采集周期。",
                "Battle", new[] { QuestIds.ChMiningSpur }, QuestIds.ObGather, "", "",
                Credit(80), Item("mat_iron_ore", 8)));
            list.Add(Story(QuestIds.ChHullLeak, "CH-07", 7, "Hull Still Leaking", "舰体仍在漏气",
                "Craft a repair kit and keep one in cargo.",
                "制造 1 个维修包并使其进入仓库。",
                "Crafting", new[] { QuestIds.ChIdleGather }, QuestIds.ObCraft, "", "",
                Credit(100), Item("con_repair_kit", 1)));
            list.Add(Story(QuestIds.ChTwoRoutes, "CH-08", 8, "Two Routes", "两条航路",
                "Chart Convoy Lane and Quantum Rift, then choose which to emphasize.",
                "在星图将护航航道与量子裂隙探至轮廓，并选择先强调的一条。",
                "Battle", new[] { QuestIds.ChHullLeak }, "", "", "",
                Credit(120)));
            list.Add(Story(QuestIds.ChExpeditionPrep, "CH-09", 9, "Prep the Push", "为远征准备补给",
                "Unlock a second deck or meet the next ship gate, and claim pending loot once.",
                "解锁第二卡组或满足下一区舰船门，并领取一次挂机待领。",
                "Formation", new[] { QuestIds.ChTwoRoutes }, "", "", "",
                Credit(150), Item("mat_scrap", 20)));
            list.Add(Story(QuestIds.ChOpenTheLock, "CH-10", 10, "You Opened a Lock", "你打开了一道锁",
                "Defeat the Frontier Anchor boss and restore the beacon.",
                "击败边境锚点 Boss，恢复航标。",
                "Battle", new[] { QuestIds.ChExpeditionPrep, QuestIds.ExConvoyR3 }, "", "", "",
                Credit(400), Item("con_recruit_ticket", 2, 1)));

            AddExpedition(list, "outer", WorldConstants.OuterBeltId, QuestIds.BodyOuter, 3,
                new[] { "EX-outer-R1", "EX-outer-R2", "EX-outer-R3" },
                new[] { QuestIds.ExOuterR1, QuestIds.ExOuterR2, QuestIds.ExOuterR3 },
                new[] { "Wreck Salvage Rights", "Lane Garrison", "Restore Outer Beacon" },
                new[] { "残骸中的采集权", "航道驻留", "修复外缘航标" },
                new[] { new[] { Credit(40), Item("mat_iron_ore", 8) }, new[] { Credit(50), Item("mat_scrap", 12) }, new[] { Credit(120), Item("con_recruit_ticket", 1, 1) } },
                new[] { QuestIds.ChOuterCleanup });
            AddExpedition(list, "mining", WorldConstants.MiningSpurId, QuestIds.BodyMining, 6,
                new[] { "EX-mining-R1", "EX-mining-R2", "EX-mining-R3" },
                new[] { QuestIds.ExMiningR1, QuestIds.ExMiningR2, QuestIds.ExMiningR3 },
                new[] { "Spur Claim", "Shaft Garrison", "Restore Mining Beacon" },
                new[] { "矿口采集权", "矿道驻留", "修复矿脉航标" },
                new[] { new[] { Credit(50), Item("mat_iron", 10) }, new[] { Credit(60), Item("mat_scrap", 16) }, new[] { Credit(160), Item("mat_energy_cell", 12) } },
                new[] { QuestIds.ChMiningSpur });
            AddExpedition(list, "rift", WorldConstants.QuantumRiftId, QuestIds.BodyRift, 9,
                new[] { "EX-rift-R1", "EX-rift-R2", "EX-rift-R3" },
                new[] { QuestIds.ExRiftR1, QuestIds.ExRiftR2, QuestIds.ExRiftR3 },
                new[] { "Fog Gather Rights", "Rift Garrison", "Restore Rift Beacon" },
                new[] { "雾中采集权", "裂隙驻留", "修复裂隙航标" },
                new[] { new[] { Credit(70), Item("mat_energy_cell", 6) }, new[] { Credit(80), Item("mat_energy_cell", 8) }, new[] { Credit(200), Item("mat_energy_cell", 8) } },
                new[] { QuestIds.ExMiningR3 });
            AddExpedition(list, "abyss", WorldConstants.AbyssalEdgeId, QuestIds.BodyAbyss, 12,
                new[] { "EX-abyss-R1", "EX-abyss-R2", "EX-abyss-R3" },
                new[] { QuestIds.ExAbyssR1, QuestIds.ExAbyssR2, QuestIds.ExAbyssR3 },
                new[] { "Edge Scrap Rights", "Abyss Garrison", "Restore Edge Beacon" },
                new[] { "边界废料权", "深渊驻留", "修复深渊航标" },
                new[] { new[] { Credit(80), Item("mat_scrap", 20) }, new[] { Credit(90), Item("con_repair_kit", 2) }, new[] { Credit(240), Item("mat_scrap", 8) } },
                new[] { QuestIds.ExRiftR3 });
            AddExpedition(list, "convoy", WorldConstants.ConvoyLaneId, QuestIds.BodyConvoy, 15,
                new[] { "EX-convoy-R1", "EX-convoy-R2", "EX-convoy-R3" },
                new[] { QuestIds.ExConvoyR1, QuestIds.ExConvoyR2, QuestIds.ExConvoyR3 },
                new[] { "Lane Gather Rights", "Convoy Garrison", "Restore Convoy Beacon" },
                new[] { "航道采集权", "护航驻留", "修复护航航标" },
                new[] { new[] { Credit(90), Item("mat_biofiber", 8) }, new[] { Credit(140) }, new[] { Credit(280), Item("con_recruit_ticket", 1, 1) } },
                new[] { QuestIds.ExAbyssR3 });
            AddExpedition(list, "anchor", WorldConstants.FrontierBossId, QuestIds.BodyAnchor, 18,
                new[] { "EX-anchor-R1", "EX-anchor-R2", "EX-anchor-R3" },
                new[] { QuestIds.ExAnchorR1, QuestIds.ExAnchorR2, QuestIds.ExAnchorR3 },
                new[] { "Anchor Screen", "Anchor Garrison", "Restart Frontier Anchor" },
                new[] { "锚点外围", "锚点驻留", "重启边境锚点" },
                new[] { new[] { Credit(100), Item("mat_scrap", 10), Item("mat_iron_ore", 6), Item("mat_energy_cell", 4) }, new[] { Credit(120), Item("con_repair_kit", 3) }, new[] { Credit(200), Item("mat_scrap", 30) } },
                new[] { QuestIds.ExConvoyR3 });

            list.Add(Ship(QuestIds.ShRangeCargo, "SH-01", 1, "Pin the Range", "把航程钉住",
                "Upgrade range and cargo to enter Mining Spur.",
                "升级航程与货舱，进入矿脉支线。",
                WorldConstants.MiningSpurId, QuestIds.BodyMining, new[] { QuestIds.ChScanSignal }, Credit(40)));
            list.Add(Ship(QuestIds.ShRiftGate, "SH-02", 2, "Hold the Phase", "抗住相位干扰",
                "Meet the Quantum Rift ship gate.",
                "为量子裂隙补齐能源、熵抗与扫描等门槛。",
                WorldConstants.QuantumRiftId, QuestIds.BodyRift, new[] { QuestIds.ExMiningR3 }, Credit(80), Item("mat_energy_cell", 4)));
            list.Add(Ship(QuestIds.ShAbyssGate, "SH-03", 3, "Brace the Hull", "加固船体",
                "Meet the Abyssal Edge ship gate.",
                "为深渊边界补齐船体、熵抗与货舱。",
                WorldConstants.AbyssalEdgeId, QuestIds.BodyAbyss, new[] { QuestIds.ExRiftR3 }, Credit(100), Item("con_repair_kit", 2)));
            list.Add(Ship(QuestIds.ShAnchorGate, "SH-04", 4, "Full Spectrum", "全谱适航",
                "Meet every Frontier Anchor ship gate.",
                "满足边境锚点全部舰船门槛。",
                WorldConstants.FrontierBossId, QuestIds.BodyAnchor, new[] { QuestIds.ExConvoyR3 }, Credit(150)));

            list.Add(Loop(QuestIds.LpFarmOuter, "LP-farm-outer", QuestLoopKind.Farm, "outer", 3, "",
                "Sweep Debris", "清扫残骸", Credit(30), Item("mat_scrap", 8)));
            list.Add(Loop(QuestIds.LpGatherOuter, "LP-gather-outer", QuestLoopKind.Gather, "outer", 3, "node_outer_iron",
                "Outer Iron", "外缘铁矿", Item("mat_iron_ore", 9)));
            list.Add(Loop(QuestIds.LpFarmMining, "LP-farm-mining", QuestLoopKind.Farm, "mining", 3, "",
                "Spur Sweep", "矿脉清扫", Credit(40), Item("mat_scrap", 10)));
            list.Add(Loop(QuestIds.LpGatherMining, "LP-gather-mining", QuestLoopKind.Gather, "mining", 3, "",
                "Dual Gather Shift", "双采值班", Item("mat_crystal_sand", 9)));
            list.Add(Loop(QuestIds.LpFarmRift, "LP-farm-rift", QuestLoopKind.Farm, "rift", 3, "",
                "Rift Echoes", "裂隙残响", Credit(55), Item("mat_energy_cell", 4)));
            list.Add(Loop(QuestIds.LpGatherRift, "LP-gather-rift", QuestLoopKind.Gather, "rift", 3, "node_rift_energy",
                "Fog Harvest", "雾中取能", Item("mat_energy_cell", 6)));
            list.Add(Loop(QuestIds.LpFarmAbyss, "LP-farm-abyss", QuestLoopKind.Farm, "abyss", 3, "",
                "Abyssal Drift", "深渊漂流", Credit(70), Item("mat_scrap", 14)));
            list.Add(Loop(QuestIds.LpGatherAbyss, "LP-gather-abyss", QuestLoopKind.Gather, "abyss", 3, "node_abyss_scrap",
                "High-Pressure Scrap", "高压废料", Item("mat_scrap", 15)));
            list.Add(Loop(QuestIds.LpFarmConvoy, "LP-farm-convoy", QuestLoopKind.Farm, "convoy", 3, "",
                "Convoy Sweep", "护航清扫", Credit(100)));
            list.Add(Loop(QuestIds.LpGatherConvoy, "LP-gather-convoy", QuestLoopKind.Gather, "convoy", 3, "node_convoy_bio",
                "Biofiber Run", "生物纤维", Item("mat_biofiber", 8)));
            list.Add(Loop(QuestIds.LpFarmAnchor, "LP-farm-anchor", QuestLoopKind.Farm, "anchor", 3, "",
                "Anchor Debris", "锚点残骸带", Credit(90), Item("mat_scrap", 16)));
            list.Add(Loop(QuestIds.LpShop, "LP-shop-restock", QuestLoopKind.Shop, "", 1, "",
                "Starport Resupply", "星港补给", Credit(25)));
            list.Add(Loop(QuestIds.LpCraftRepair, "LP-craft-repair", QuestLoopKind.Craft, "", 1, "",
                "Stock Repair Kits", "补齐维修包", Item("mat_scrap", 6)));
            return list;
        }

        private static void AddExpedition(
            List<QuestDef> list, string node, string regionId, string bodyId, int orderBase,
            string[] display, string[] ids, string[] en, string[] zh, QuestRewardGrant[][] rewards, string[] r1Depends)
        {
            for (int i = 0; i < 3; i++)
            {
                string[] depends = i == 0 ? r1Depends : new[] { ids[i - 1] };
                list.Add(new QuestDef
                {
                    questId = ids[i],
                    displayId = display[i],
                    type = QuestType.Expedition,
                    order = orderBase + i,
                    titleEn = en[i],
                    titleZh = zh[i],
                    hintEn = en[i],
                    hintZh = zh[i],
                    targetScreen = "Battle",
                    dependsOn = depends,
                    regionId = regionId,
                    bodyId = bodyId,
                    ring = i + 1,
                    rewards = rewards[i]
                });
            }
        }

        private static QuestDef Ob(string id, string display, int order, string en, string zh,
            string hintEn, string hintZh, string screen, string[] depends, params QuestRewardGrant[] rewards) =>
            new QuestDef
            {
                questId = id, displayId = display, type = QuestType.Onboarding, order = order,
                titleEn = en, titleZh = zh, hintEn = hintEn, hintZh = hintZh, targetScreen = screen,
                dependsOn = depends, rewards = rewards
            };

        private static QuestDef Story(string id, string display, int order, string en, string zh,
            string hintEn, string hintZh, string screen, string[] depends, string linked, string startDlg, string claimDlg,
            params QuestRewardGrant[] rewards) =>
            new QuestDef
            {
                questId = id, displayId = display, type = QuestType.Story, order = order,
                titleEn = en, titleZh = zh, hintEn = hintEn, hintZh = hintZh, targetScreen = screen,
                dependsOn = depends, linkedOnboardingStepId = linked,
                dialogueIdOnStart = startDlg, dialogueIdOnClaim = claimDlg, rewards = rewards
            };

        private static QuestDef Ship(string id, string display, int order, string en, string zh,
            string hintEn, string hintZh, string regionId, string bodyId, string[] depends, params QuestRewardGrant[] rewards) =>
            new QuestDef
            {
                questId = id, displayId = display, type = QuestType.Ship, order = order,
                titleEn = en, titleZh = zh, hintEn = hintEn, hintZh = hintZh, targetScreen = "Ship",
                dependsOn = depends, regionId = regionId, bodyId = bodyId, rewards = rewards
            };

        private static QuestDef Loop(string id, string display, QuestLoopKind kind, string node, int target, string gatherNode,
            string en, string zh, params QuestRewardGrant[] rewards) =>
            new QuestDef
            {
                questId = id, displayId = display, type = QuestType.Loop, order = 100,
                titleEn = en, titleZh = zh, hintEn = en, hintZh = zh,
                targetScreen = kind == QuestLoopKind.Shop ? "Market" : kind == QuestLoopKind.Craft ? "Crafting" : "Battle",
                loopKind = kind, loopTarget = target, gatherNodeId = gatherNode,
                regionId = string.IsNullOrEmpty(node) ? "" : QuestIds.RegionForNode(node),
                rewards = rewards
            };

        private static QuestRewardGrant Credit(int n) =>
            new QuestRewardGrant { kind = "credits", credits = n };

        private static QuestRewardGrant Item(string defId, int qty, int quality = 2) =>
            new QuestRewardGrant { kind = "item", itemDefId = defId, quantity = qty, quality = quality };
    }
}

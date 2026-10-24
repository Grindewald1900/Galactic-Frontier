using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    /// <summary>Expedition stages per combat region. POIs have no campaign.</summary>
    public static class NodeCampaignCatalog
    {
        private static Dictionary<string, NodeCampaignDef> cached;

        public static NodeCampaignDef Get(string regionId)
        {
            Ensure();
            if (string.IsNullOrEmpty(regionId)) return null;
            if (cached.TryGetValue(regionId, out var def))
                return def;
            return LegacySingleStage(RegionCatalog.Get(regionId));
        }

        public static BattleStageDef CurrentStage(RegionRuntimeState runtime, string regionId)
        {
            var campaign = Get(regionId);
            if (campaign == null || campaign.StageCount <= 0) return null;
            var index = NextStageIndex(runtime, campaign);
            if (index < 1 || index > campaign.StageCount) return campaign.stages[campaign.StageCount - 1];
            return campaign.stages[index - 1];
        }

        /// <summary>1-based next fight. After beacon restore this equals <see cref="NodeCampaignDef.StageCount"/>.</summary>
        public static int NextStageIndex(RegionRuntimeState runtime, NodeCampaignDef campaign)
        {
            if (campaign == null || campaign.StageCount <= 0) return 1;
            var cleared = runtime?.highestClearedStage ?? 0;
            if (cleared >= campaign.StageCount) return campaign.StageCount;
            return cleared + 1;
        }

        public static int CombatIdleMinStage(NodeCampaignDef campaign) =>
            campaign != null && campaign.StageCount > 1 ? 3 : campaign?.StageCount ?? 1;

        public static int GatherIdleMinStage(NodeCampaignDef campaign) =>
            campaign != null && campaign.StageCount > 1 ? 2 : campaign?.StageCount ?? 1;

        private static void Ensure()
        {
            if (cached != null) return;
            cached = new Dictionary<string, NodeCampaignDef>();
            Add(BuildOuterHaven());
            Add(BuildMiningSpur());
            Add(BuildConvoyLane());
            Add(BuildQuantumRift());
            Add(BuildAbyssalEdge());
            Add(BuildFrontierAnchor());
        }

        private static void Add(NodeCampaignDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.regionId)) return;
            if (def.stages != null)
            {
                foreach (var stage in def.stages)
                {
                    if (stage == null || string.IsNullOrEmpty(stage.encounterId)) continue;
                    var power = EnemyCatalog.RecommendedPower(stage.encounterId);
                    if (power > 0)
                        stage.recommendedPower = power;
                }
            }

            cached[def.regionId] = def;
        }

        private static bool regionPowerSynced;

        /// <summary>
        /// Copies stage-1 enemy power onto the region so voyage / formation hints use the card-power scale.
        /// The per-stage number on the node panel stays on <see cref="BattleStageDef.recommendedPower"/>.
        /// </summary>
        public static void SyncRegionPower()
        {
            if (regionPowerSynced) return;
            regionPowerSynced = true;
            Ensure();
            foreach (var region in RegionCatalog.CachedOrBuild())
            {
                if (region == null || string.IsNullOrEmpty(region.regionId)) continue;
                if (!cached.TryGetValue(region.regionId, out var campaign)) continue;
                if (campaign?.stages == null || campaign.stages.Length == 0) continue;
                region.recommendedPower = campaign.stages[0].recommendedPower;
            }
        }

        private static NodeCampaignDef BuildOuterHaven()
        {
            return new NodeCampaignDef
            {
                regionId = WorldConstants.OuterBeltId,
                stages = new[]
                {
                    Stage("outer_contact", BattleStageType.Normal, "enc_outer_main", 80,
                        "reward_outer_s1", CampaignIdleUnlock.None,
                        "Lane Contact", "航道接触",
                        "Basic raider boats", "普通劫掠艇",
                        "Learn the local enemy mix", "展示区域敌人和环境机制"),
                    Stage("outer_wreck", BattleStageType.Normal, "enc_outer_s2", 90,
                        "reward_outer_s2", CampaignIdleUnlock.Gather,
                        "Wreck Search", "残骸搜索",
                        "Enemies strike the cargo hold", "敌人开始攻击货舱",
                        "More raiders among the debris", "敌人数量增加"),
                    Stage("outer_escort", BattleStageType.Normal, "enc_outer_s3", 100,
                        "reward_outer_s3", CampaignIdleUnlock.Combat,
                        "Escort Encounter", "护航遭遇",
                        "Fast boats bypass the front row", "高速艇绕过前排",
                        "Protect the rear line", "引导调整站位或战术"),
                    Stage("outer_ambush", BattleStageType.Normal, "enc_outer_s4", 115,
                        "reward_outer_s4", CampaignIdleUnlock.None,
                        "Entropy Ambush", "熵雾伏击",
                        "Entropy fog lowers accuracy", "熵雾降低命中",
                        "Exposes hull or formation gaps", "暴露舰船或阵容短板"),
                    Stage("outer_elite", BattleStageType.Elite, "enc_outer_s5", 135,
                        "reward_outer_s5", CampaignIdleUnlock.RareDrop,
                        "Elite Blockade", "精英封锁",
                        "Command ship shields allies", "精英指挥舰为友军提供护盾",
                        "Near-boss check", "接近 Boss 机制"),
                    Stage("outer_boss", BattleStageType.Boss, "enc_outer_boss", 160,
                        "reward_outer_first", CampaignIdleUnlock.Full,
                        "Raid Flagship", "劫掠旗舰",
                        "Rear strike, shields, and fog", "后排突袭、护盾与熵雾干扰",
                        "Combined raid exam", "完成节点主线目标")
                }
            };
        }

        private static NodeCampaignDef BuildMiningSpur()
        {
            return new NodeCampaignDef
            {
                regionId = WorldConstants.MiningSpurId,
                stages = new[]
                {
                    Stage("mining_claim", BattleStageType.Normal, "enc_mining_main", 120,
                        "reward_mining_s1", CampaignIdleUnlock.None,
                        "Claim Jumpers", "夺矿先遣",
                        "Raiders focus the hull", "劫掠者集火船体",
                        "Armor check at the spur mouth", "矿脉入口的护甲检验"),
                    Stage("mining_drill", BattleStageType.Normal, "enc_mining_s2", 135,
                        "reward_mining_s2", CampaignIdleUnlock.Gather,
                        "Drill Intercept", "钻机拦截",
                        "Burst damage vs cargo holds", "爆发伤害直击货舱",
                        "Gather teams can start after this fight", "此战后可启动采集"),
                    Stage("mining_collapse", BattleStageType.Normal, "enc_mining_s3", 150,
                        "reward_mining_s3", CampaignIdleUnlock.Combat,
                        "Shaft Collapse", "矿道塌方",
                        "Front row is forced to tank debris", "前排必须吃下塌方冲击",
                        "Combat idle unlocks", "解锁战斗挂机"),
                    Stage("mining_overload", BattleStageType.Normal, "enc_mining_s4", 170,
                        "reward_mining_s4", CampaignIdleUnlock.None,
                        "Core Overload", "矿核过载",
                        "Armor shred stacks each round", "破甲效果逐回合叠加",
                        "Exposes thin plating", "暴露护甲短板"),
                    Stage("mining_elite", BattleStageType.Elite, "enc_mining_s5", 195,
                        "reward_mining_s5", CampaignIdleUnlock.RareDrop,
                        "Foreman Guard", "工头卫队",
                        "Elite drill barge shields the pack", "精英钻艇为友军提供护盾",
                        "Near-boss check", "接近 Boss 机制"),
                    Stage("mining_boss", BattleStageType.Boss, "enc_mining_boss", 230,
                        "reward_mining_first", CampaignIdleUnlock.Full,
                        "Ore Leviathan", "矿核巨兽",
                        "Armor break plus cargo strikes", "破甲与货舱打击",
                        "Restore the spur beacon", "恢复矿脉航标")
                }
            };
        }

        private static NodeCampaignDef BuildConvoyLane()
        {
            return new NodeCampaignDef
            {
                regionId = WorldConstants.ConvoyLaneId,
                stages = new[]
                {
                    Stage("convoy_scout", BattleStageType.Normal, "enc_convoy_main", 300,
                        "reward_convoy_s1", CampaignIdleUnlock.None,
                        "Lane Scouts", "航道斥候",
                        "Fast interceptors hunt the rear", "高速拦截艇猎杀后排",
                        "Escort the first hauler", "护送首艘货船"),
                    Stage("convoy_pickets", BattleStageType.Normal, "enc_convoy_s2", 320,
                        "reward_convoy_s2", CampaignIdleUnlock.Gather,
                        "Picket Screen", "警戒幕",
                        "Must keep a civilian slot alive", "必须保住民用舰位",
                        "Gather unlocks on the lane", "航道采集解锁"),
                    Stage("convoy_cross", BattleStageType.Normal, "enc_convoy_s3", 345,
                        "reward_convoy_s3", CampaignIdleUnlock.Combat,
                        "Crossfire", "交叉火力",
                        "Two flanks fire past the front", "两侧火力越过前排",
                        "Combat idle unlocks", "解锁战斗挂机"),
                    Stage("convoy_jam", BattleStageType.Normal, "enc_convoy_s4", 370,
                        "reward_convoy_s4", CampaignIdleUnlock.None,
                        "Signal Jam", "信号干扰",
                        "Accuracy drops while jammers live", "干扰舰存活时命中下降",
                        "Kill the jammer first", "优先击破干扰舰"),
                    Stage("convoy_board", BattleStageType.Normal, "enc_convoy_s5", 400,
                        "reward_convoy_s5", CampaignIdleUnlock.None,
                        "Boarding Wave", "登舰波次",
                        "Boarders skip to the back row", "登舰队跳过后排",
                        "Protect support cards", "保护辅助单位"),
                    Stage("convoy_elite", BattleStageType.Elite, "enc_convoy_s6", 430,
                        "reward_convoy_s6", CampaignIdleUnlock.RareDrop,
                        "Escort Breaker", "护航撕裂者",
                        "Elite cruiser marks one ally", "精英巡洋锁定一名友军",
                        "Near-boss check", "接近 Boss 机制"),
                    Stage("convoy_boss", BattleStageType.Boss, "enc_convoy_boss", 470,
                        "reward_convoy_first", CampaignIdleUnlock.Full,
                        "Privateer Flag", "私掠旗舰",
                        "Focus fire plus boarding", "集火与登舰并用",
                        "Restore the convoy beacon", "恢复护航航标")
                }
            };
        }

        private static NodeCampaignDef BuildQuantumRift()
        {
            return new NodeCampaignDef
            {
                regionId = WorldConstants.QuantumRiftId,
                stages = new[]
                {
                    Stage("rift_echo", BattleStageType.Normal, "enc_rift_main", 200,
                        "reward_rift_s1", CampaignIdleUnlock.None,
                        "Echo Wardens", "残响看守",
                        "Position swaps after two turns", "两回合后站位互换",
                        "Learn the rift offset", "熟悉裂隙偏移"),
                    Stage("rift_shear", BattleStageType.Normal, "enc_rift_s2", 220,
                        "reward_rift_s2", CampaignIdleUnlock.Gather,
                        "Shear Band", "剪切带",
                        "Side lanes take extra damage", "边路承受额外伤害",
                        "Gather unlocks in the fog", "雾中采集解锁"),
                    Stage("rift_mirror", BattleStageType.Normal, "enc_rift_s3", 245,
                        "reward_rift_s3", CampaignIdleUnlock.Combat,
                        "Mirror Pack", "镜像群",
                        "Copies the last card you played", "复制上一张打出的卡",
                        "Combat idle unlocks", "解锁战斗挂机"),
                    Stage("rift_drift", BattleStageType.Normal, "enc_rift_s4", 270,
                        "reward_rift_s4", CampaignIdleUnlock.None,
                        "Phase Drift", "相位漂移",
                        "Accuracy falls each round", "命中逐回合下降",
                        "Bring scan or burst", "需要扫描或爆发"),
                    Stage("rift_spike", BattleStageType.Normal, "enc_rift_s5", 295,
                        "reward_rift_s5", CampaignIdleUnlock.None,
                        "Entropy Spike", "熵能尖刺",
                        "Random slot is silenced one turn", "随机一格沉默一回合",
                        "Formation flexibility check", "检验阵容灵活度"),
                    Stage("rift_fold", BattleStageType.Normal, "enc_rift_s6", 320,
                        "reward_rift_s6", CampaignIdleUnlock.None,
                        "Fold Corridor", "折叠廊",
                        "Back row is pulled to the front", "后排被拉到前排",
                        "Protect fragile damage", "保护脆皮输出"),
                    Stage("rift_elite", BattleStageType.Elite, "enc_rift_s7", 350,
                        "reward_rift_s7", CampaignIdleUnlock.RareDrop,
                        "Warden Prime", "看守长",
                        "Elite shifts the whole board", "精英移动整板站位",
                        "Near-boss check", "接近 Boss 机制"),
                    Stage("rift_boss", BattleStageType.Boss, "enc_rift_boss", 390,
                        "reward_rift_first", CampaignIdleUnlock.Full,
                        "Rift Heart", "裂隙之心",
                        "Offset, silence, and fog", "偏移、沉默与熵雾",
                        "Restore the rift beacon", "恢复裂隙航标")
                }
            };
        }

        private static NodeCampaignDef BuildAbyssalEdge()
        {
            return new NodeCampaignDef
            {
                regionId = WorldConstants.AbyssalEdgeId,
                stages = new[]
                {
                    Stage("abyss_drift", BattleStageType.Normal, "enc_abyss_main", 260,
                        "reward_abyss_s1", CampaignIdleUnlock.None,
                        "Drift Host", "漂流宿主",
                        "Entropy chips hull each turn", "熵蚀每回合削船体",
                        "Repair kits start to matter", "维修包开始成为刚需"),
                    Stage("abyss_leech", BattleStageType.Normal, "enc_abyss_s2", 285,
                        "reward_abyss_s2", CampaignIdleUnlock.Gather,
                        "Leech Tide", "噬能潮",
                        "Healing is halved", "治疗减半",
                        "Gather unlocks on the edge", "边界采集解锁"),
                    Stage("abyss_pressure", BattleStageType.Normal, "enc_abyss_s3", 310,
                        "reward_abyss_s3", CampaignIdleUnlock.Combat,
                        "Pressure Wall", "高压墙",
                        "Front row loses shield first", "前排先失护盾",
                        "Combat idle unlocks", "解锁战斗挂机"),
                    Stage("abyss_bloom", BattleStageType.Normal, "enc_abyss_s4", 340,
                        "reward_abyss_s4", CampaignIdleUnlock.None,
                        "Entropy Bloom", "熵华绽放",
                        "DoT spreads to adjacent slots", "持续伤害扩散到邻格",
                        "Bring cleanse or burst", "需要净化或爆发"),
                    Stage("abyss_choir", BattleStageType.Normal, "enc_abyss_s5", 370,
                        "reward_abyss_s5", CampaignIdleUnlock.None,
                        "Deep Choir", "深渊合唱",
                        "All enemies gain a small shield", "敌方全体获得薄护盾",
                        "AoE check", "检验群体伤害"),
                    Stage("abyss_rift", BattleStageType.Normal, "enc_abyss_s6", 400,
                        "reward_abyss_s6", CampaignIdleUnlock.None,
                        "Black Current", "黑潮",
                        "Lowest-hp ally is focused", "最低生命友军被集火",
                        "Protect the wounded", "保护伤员"),
                    Stage("abyss_elite", BattleStageType.Elite, "enc_abyss_s7", 435,
                        "reward_abyss_s7", CampaignIdleUnlock.RareDrop,
                        "Host Shepherd", "宿主牧者",
                        "Elite refreshes entropy DoT", "精英刷新熵蚀",
                        "Near-boss check", "接近 Boss 机制"),
                    Stage("abyss_boss", BattleStageType.Boss, "enc_abyss_boss", 480,
                        "reward_abyss_first", CampaignIdleUnlock.Full,
                        "Abyssal Crown", "深渊冠冕",
                        "Entropy, leech, and focus fire", "熵蚀、噬能与集火",
                        "Restore the edge beacon", "恢复深渊航标")
                }
            };
        }

        private static NodeCampaignDef BuildFrontierAnchor()
        {
            return new NodeCampaignDef
            {
                regionId = WorldConstants.FrontierBossId,
                stages = new[]
                {
                    Stage("anchor_screen", BattleStageType.Normal, "enc_frontier_s1", 400,
                        "reward_frontier_s1", CampaignIdleUnlock.None,
                        "Outer Screen", "外层幕墙",
                        "Mixed raid plus rift boats", "混编劫掠与裂隙艇",
                        "Composite opener", "复合开场"),
                    Stage("anchor_battery", BattleStageType.Normal, "enc_frontier_s2", 425,
                        "reward_frontier_s2", CampaignIdleUnlock.Gather,
                        "Battery Ring", "炮阵环",
                        "Artillery strikes the back row", "炮击直打后排",
                        "Gather unlocks at the hub", "锚点采集解锁"),
                    Stage("anchor_guard", BattleStageType.Normal, "enc_frontier_s3", 455,
                        "reward_frontier_s3", CampaignIdleUnlock.Combat,
                        "Honor Guard", "仪仗卫队",
                        "Guards taunt the highest threat", "卫队嘲讽最高威胁",
                        "Combat idle unlocks", "解锁战斗挂机"),
                    Stage("anchor_fault", BattleStageType.Normal, "enc_frontier_s4", 485,
                        "reward_frontier_s4", CampaignIdleUnlock.None,
                        "Fault Line", "断层线",
                        "Rift offset plus armor shred", "裂隙偏移加破甲",
                        "Two regional mechanics at once", "同时出现两种区域机制"),
                    Stage("anchor_veil", BattleStageType.Normal, "enc_frontier_s5", 515,
                        "reward_frontier_s5", CampaignIdleUnlock.None,
                        "Entropy Veil", "熵幕",
                        "Accuracy and healing both drop", "命中与治疗同时下降",
                        "Sustain check", "检验续航"),
                    Stage("anchor_elite", BattleStageType.Elite, "enc_frontier_s6", 550,
                        "reward_frontier_s6", CampaignIdleUnlock.RareDrop,
                        "Anchor Warden", "锚点看守",
                        "Elite shields then marks", "精英先护盾再标记",
                        "Phase-one exam", "第一阶段考核"),
                    Stage("anchor_gate", BattleStageType.Elite, "enc_frontier_s7", 590,
                        "reward_frontier_s7", CampaignIdleUnlock.None,
                        "Gatebreaker", "破门者",
                        "Second elite: boarding plus fog", "第二精英：登舰与熵雾",
                        "Phase-two exam", "第二阶段考核"),
                    Stage("anchor_boss", BattleStageType.Boss, "enc_frontier_boss", 650,
                        "reward_boss_first", CampaignIdleUnlock.Full,
                        "Frontier Sovereign", "边境主权舰",
                        "Combined raid, rift, and entropy", "劫掠、裂隙与熵蚀合体",
                        "Clear Frontier VII", "完成第七前沿")
                }
            };
        }

        private static NodeCampaignDef LegacySingleStage(RegionConfig cfg)
        {
            if (cfg == null) return null;
            var type = cfg.bossRegion ? BattleStageType.Boss : BattleStageType.Normal;
            return new NodeCampaignDef
            {
                regionId = cfg.regionId,
                stages = new[]
                {
                    Stage(
                        cfg.regionId + "_main",
                        type,
                        cfg.mainEncounterId,
                        cfg.recommendedPower,
                        cfg.firstClearRewardId,
                        CampaignIdleUnlock.Full,
                        cfg.displayNameEn,
                        cfg.displayNameZh,
                        cfg.blurbEn,
                        cfg.blurbZh,
                        "",
                        "")
                }
            };
        }

        private static BattleStageDef Stage(
            string id,
            BattleStageType type,
            string encounterId,
            int power,
            string rewardId,
            CampaignIdleUnlock idle,
            string nameEn,
            string nameZh,
            string traitEn,
            string traitZh,
            string envEn,
            string envZh) =>
            new BattleStageDef
            {
                stageId = id,
                stageType = type,
                encounterId = encounterId,
                recommendedPower = power,
                firstClearRewardId = rewardId ?? "",
                idleUnlock = idle,
                displayNameEn = nameEn,
                displayNameZh = nameZh,
                traitEn = traitEn,
                traitZh = traitZh,
                environmentEn = envEn,
                environmentZh = envZh
            };
    }
}

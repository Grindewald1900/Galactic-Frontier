using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    public static class EncounterCatalog
    {
        private static Dictionary<string, EncounterConfig> cached;

        public static EncounterConfig Get(string encounterId)
        {
            Ensure();
            if (string.IsNullOrEmpty(encounterId)) return null;
            return cached.TryGetValue(encounterId, out var enc) ? enc : null;
        }

        public static void ReplaceAll(IEnumerable<EncounterConfig> encounters)
        {
            cached = new Dictionary<string, EncounterConfig>();
            if (encounters == null) return;
            foreach (var e in encounters)
            {
                if (e != null && !string.IsNullOrEmpty(e.encounterId))
                    cached[e.encounterId] = e;
            }
        }

        public static IEnumerable<EncounterConfig> All
        {
            get
            {
                Ensure();
                return cached.Values;
            }
        }

        private static void Ensure()
        {
            if (cached != null) return;
            cached = new Dictionary<string, EncounterConfig>();
            foreach (var e in BuildDefaults())
                cached[e.encounterId] = e;
        }

        public static List<EncounterConfig> BuildDefaults()
        {
            return new List<EncounterConfig>
            {
                Enc("enc_prologue_sweep", "Sweep Drones", "清扫无人机", FactionTags.FrontierGuard, false, 0, true,
                    Prof("drone_sweeper")),
                Enc("enc_outer_main", "Outer Belt Patrol", "外缘巡逻队", FactionTags.FrontierGuard, false, 2,
                    Prof("outer_lane_raider"), Prof("outer_lane_scout")),
                Enc("enc_outer_s2", "Wreck Search", "残骸搜索", FactionTags.FrontierGuard, false, 2,
                    Prof("outer_wreck_bruiser"), Prof("outer_salvage_guard"), Prof("outer_hold_piercer")),
                Enc("enc_outer_s3", "Escort Encounter", "护航遭遇", FactionTags.FrontierGuard, false, 3,
                    Prof("outer_escort_guard"), Gap(), Prof("outer_fast_boat")),
                Enc("enc_outer_s4", "Entropy Ambush", "熵雾伏击", FactionTags.RiftSyndicate, false, 3,
                    Prof("outer_fog_striker"), Gap(), Prof("outer_fog_caster")),
                Enc("enc_outer_s5", "Elite Blockade", "精英封锁", FactionTags.FrontierGuard, false, 4,
                    Prof("outer_elite_command"), Prof("outer_blockade"), Prof("outer_elite_rear")),
                Enc("enc_outer_boss", "Raid Flagship", "劫掠旗舰", FactionTags.FrontierGuard, true, 8,
                    Prof("outer_flagship")),
                Enc("enc_outer_farm", "Outer Belt Scraps", "外缘残骸清扫", FactionTags.FrontierGuard, false, 1,
                    Prof("outer_scrap_drone")),
                Enc("enc_mining_main", "Mining Spur Raiders", "矿脉劫掠者", FactionTags.FrontierGuard, false, 3,
                    Slot("Magki", 2), Slot("Sernia", 2)),
                Enc("enc_mining_s2", "Drill Intercept", "钻机拦截", FactionTags.FrontierGuard, false, 3,
                    Slot("Magki", 2), Slot("Sernia", 2), Slot("Asra", 2)),
                Enc("enc_mining_s3", "Shaft Collapse", "矿道塌方", FactionTags.FrontierGuard, false, 4,
                    Slot("Magki", 3), Slot("Sernia", 2)),
                Enc("enc_mining_s4", "Core Overload", "矿核过载", FactionTags.FrontierGuard, false, 4,
                    Slot("Magki", 3), Slot("Sernia", 3), Slot("Asra", 2)),
                Enc("enc_mining_s5", "Foreman Guard", "工头卫队", FactionTags.FrontierGuard, false, 5,
                    Slot("Magki", 4), Slot("Sernia", 3), Slot("Asra", 3)),
                Enc("enc_mining_boss", "Ore Leviathan", "矿核巨兽", FactionTags.FrontierGuard, true, 8,
                    Prof("mining_leviathan")),
                Enc("enc_mining_farm", "Mining Spur Sweep", "矿脉清扫", FactionTags.FrontierGuard, false, 2,
                    Slot("Magki", 2)),
                Enc("enc_rift_main", "Quantum Rift Wardens", "裂隙看守", FactionTags.RiftSyndicate, false, 4,
                    Slot("Asra", 3), Slot("Magki", 3), Slot("Sernia", 3)),
                Enc("enc_rift_s2", "Shear Band", "剪切带", FactionTags.RiftSyndicate, false, 4,
                    Slot("Asra", 3), Slot("Sernia", 3)),
                Enc("enc_rift_s3", "Mirror Pack", "镜像群", FactionTags.RiftSyndicate, false, 5,
                    Slot("Asra", 4), Slot("Magki", 3), Slot("Sernia", 3)),
                Enc("enc_rift_s4", "Phase Drift", "相位漂移", FactionTags.RiftSyndicate, false, 5,
                    Slot("Asra", 4), Slot("Magki", 4)),
                Enc("enc_rift_s5", "Entropy Spike", "熵能尖刺", FactionTags.RiftSyndicate, false, 6,
                    Slot("Sernia", 4), Slot("Asra", 4), Slot("Magki", 3)),
                Enc("enc_rift_s6", "Fold Corridor", "折叠廊", FactionTags.RiftSyndicate, false, 6,
                    Slot("Asra", 5), Slot("Sernia", 4), Slot("Magki", 4)),
                Enc("enc_rift_s7", "Warden Prime", "看守长", FactionTags.RiftSyndicate, false, 7,
                    Slot("Sernia", 5), Slot("Asra", 5), Slot("Magki", 4)),
                Enc("enc_rift_boss", "Rift Heart", "裂隙之心", FactionTags.RiftSyndicate, true, 10,
                    Slot("Sernia", 6), Slot("Asra", 5), Slot("Magki", 5)),
                Enc("enc_rift_farm", "Rift Echoes", "裂隙残响", FactionTags.RiftSyndicate, false, 3,
                    Slot("Asra", 3), Slot("Magki", 2)),
                Enc("enc_abyss_main", "Abyssal Edge Host", "深渊宿主", FactionTags.RiftSyndicate, false, 5,
                    Slot("Sernia", 4), Slot("Asra", 4), Slot("Magki", 4)),
                Enc("enc_abyss_s2", "Leech Tide", "噬能潮", FactionTags.RiftSyndicate, false, 5,
                    Slot("Sernia", 4), Slot("Asra", 4)),
                Enc("enc_abyss_s3", "Pressure Wall", "高压墙", FactionTags.RiftSyndicate, false, 6,
                    Slot("Sernia", 5), Slot("Magki", 4), Slot("Asra", 4)),
                Enc("enc_abyss_s4", "Entropy Bloom", "熵华绽放", FactionTags.RiftSyndicate, false, 6,
                    Slot("Sernia", 5), Slot("Asra", 5)),
                Enc("enc_abyss_s5", "Deep Choir", "深渊合唱", FactionTags.RiftSyndicate, false, 7,
                    Slot("Sernia", 5), Slot("Asra", 5), Slot("Magki", 5)),
                Enc("enc_abyss_s6", "Black Current", "黑潮", FactionTags.RiftSyndicate, false, 7,
                    Slot("Sernia", 6), Slot("Asra", 5), Slot("Magki", 5)),
                Enc("enc_abyss_s7", "Host Shepherd", "宿主牧者", FactionTags.RiftSyndicate, false, 8,
                    Slot("Sernia", 6), Slot("Asra", 6), Slot("Magki", 5)),
                Enc("enc_abyss_boss", "Abyssal Crown", "深渊冠冕", FactionTags.RiftSyndicate, true, 11,
                    Slot("Sernia", 7), Slot("Asra", 6), Slot("Magki", 6)),
                Enc("enc_abyss_farm", "Abyssal Drift", "深渊漂流体", FactionTags.RiftSyndicate, false, 4,
                    Slot("Sernia", 4), Slot("Asra", 3)),
                Enc("enc_convoy_main", "Convoy Ambush", "护航伏击", FactionTags.FrontierGuard, false, 5,
                    Slot("Magki", 5), Slot("Sernia", 5), Slot("Asra", 4)),
                Enc("enc_convoy_s2", "Picket Screen", "警戒幕", FactionTags.FrontierGuard, false, 5,
                    Slot("Magki", 5), Slot("Asra", 5)),
                Enc("enc_convoy_s3", "Crossfire", "交叉火力", FactionTags.FrontierGuard, false, 6,
                    Slot("Magki", 5), Slot("Sernia", 5), Slot("Asra", 5)),
                Enc("enc_convoy_s4", "Signal Jam", "信号干扰", FactionTags.FrontierGuard, false, 6,
                    Slot("Sernia", 5), Slot("Asra", 5), Slot("Magki", 5)),
                Enc("enc_convoy_s5", "Boarding Wave", "登舰波次", FactionTags.FrontierGuard, false, 7,
                    Slot("Magki", 6), Slot("Asra", 5), Slot("Sernia", 5)),
                Enc("enc_convoy_s6", "Escort Breaker", "护航撕裂者", FactionTags.FrontierGuard, false, 8,
                    Slot("Magki", 6), Slot("Sernia", 6), Slot("Asra", 5)),
                Enc("enc_convoy_boss", "Privateer Flag", "私掠旗舰", FactionTags.FrontierGuard, true, 11,
                    Slot("Magki", 7), Slot("Sernia", 6), Slot("Asra", 6)),
                Enc("enc_convoy_farm", "Convoy Escort Sweep", "护航清扫", FactionTags.FrontierGuard, false, 4,
                    Slot("Magki", 4), Slot("Asra", 4)),
                Enc("enc_frontier_s1", "Outer Screen", "外层幕墙", FactionTags.RiftSyndicate, false, 7,
                    Slot("Sernia", 5), Slot("Asra", 5), Slot("Magki", 5)),
                Enc("enc_frontier_s2", "Battery Ring", "炮阵环", FactionTags.RiftSyndicate, false, 7,
                    Slot("Magki", 6), Slot("Asra", 5), Slot("Sernia", 5)),
                Enc("enc_frontier_s3", "Honor Guard", "仪仗卫队", FactionTags.RiftSyndicate, false, 8,
                    Slot("Sernia", 6), Slot("Magki", 6), Slot("Asra", 5)),
                Enc("enc_frontier_s4", "Fault Line", "断层线", FactionTags.RiftSyndicate, false, 8,
                    Slot("Asra", 6), Slot("Sernia", 6), Slot("Magki", 6)),
                Enc("enc_frontier_s5", "Entropy Veil", "熵幕", FactionTags.RiftSyndicate, false, 9,
                    Slot("Sernia", 6), Slot("Asra", 6), Slot("Magki", 6)),
                Enc("enc_frontier_s6", "Anchor Warden", "锚点看守", FactionTags.RiftSyndicate, false, 10,
                    Slot("Sernia", 7), Slot("Asra", 6), Slot("Magki", 6)),
                Enc("enc_frontier_s7", "Gatebreaker", "破门者", FactionTags.RiftSyndicate, false, 11,
                    Slot("Sernia", 7), Slot("Asra", 7), Slot("Magki", 6), Slot("Sernia", 6)),
                Enc("enc_frontier_boss", "Frontier Anchor Boss", "边境锚点首领", FactionTags.RiftSyndicate, true, 12,
                    Slot("Sernia", 6), Slot("Asra", 6), Slot("Magki", 6), Slot("Sernia", 5)),
                Enc("enc_frontier_farm", "Anchor Debris Field", "锚点残骸带", FactionTags.RiftSyndicate, false, 6,
                    Slot("Asra", 5), Slot("Magki", 5))
            };
        }

        private static EncounterConfig Enc(
            string id, string nameEn, string nameZh, string faction, bool boss, int loot,
            bool isTutorial,
            params EncounterEnemySlot[] enemies) =>
            new EncounterConfig
            {
                encounterId = id,
                displayName = nameEn,
                displayNameZh = nameZh,
                factionTag = faction ?? "",
                isBoss = boss,
                isTutorial = isTutorial,
                lootScrap = loot,
                enemies = enemies
            };

        private static EncounterConfig Enc(
            string id, string nameEn, string nameZh, string faction, bool boss, int loot,
            params EncounterEnemySlot[] enemies) =>
            Enc(id, nameEn, nameZh, faction, boss, loot, false, enemies);

        private static EncounterEnemySlot Slot(string key, int level) =>
            new EncounterEnemySlot { characterKey = key, level = level, weight = 1 };

        private static EncounterEnemySlot Prof(string profileId) =>
            new EncounterEnemySlot { profileId = profileId, characterKey = "", level = 1, weight = 1 };

        /// <summary>Vacant board slot so the next enemy keeps its row (index 2+ is the back row).</summary>
        private static EncounterEnemySlot Gap() =>
            new EncounterEnemySlot { profileId = "", characterKey = "", level = 0, weight = 0 };
    }
}

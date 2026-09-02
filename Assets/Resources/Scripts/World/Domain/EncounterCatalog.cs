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
                    Slot("Asra", 1)),
                Enc("enc_outer_main", "Outer Belt Patrol", "外缘巡逻队", FactionTags.FrontierGuard, false, 2,
                    Slot("Asra", 1), Slot("Magki", 1)),
                Enc("enc_outer_farm", "Outer Belt Scraps", "外缘残骸清扫", FactionTags.FrontierGuard, false, 1,
                    Slot("Asra", 1)),
                Enc("enc_mining_main", "Mining Spur Raiders", "矿脉劫掠者", FactionTags.FrontierGuard, false, 3,
                    Slot("Magki", 2), Slot("Sernia", 2)),
                Enc("enc_mining_farm", "Mining Spur Sweep", "矿脉清扫", FactionTags.FrontierGuard, false, 2,
                    Slot("Magki", 2)),
                Enc("enc_rift_main", "Quantum Rift Wardens", "裂隙看守", FactionTags.RiftSyndicate, false, 4,
                    Slot("Asra", 3), Slot("Magki", 3), Slot("Sernia", 3)),
                Enc("enc_rift_farm", "Rift Echoes", "裂隙残响", FactionTags.RiftSyndicate, false, 3,
                    Slot("Asra", 3), Slot("Magki", 2)),
                Enc("enc_abyss_main", "Abyssal Edge Host", "深渊宿主", FactionTags.RiftSyndicate, false, 5,
                    Slot("Sernia", 4), Slot("Asra", 4), Slot("Magki", 4)),
                Enc("enc_abyss_farm", "Abyssal Drift", "深渊漂流体", FactionTags.RiftSyndicate, false, 4,
                    Slot("Sernia", 4), Slot("Asra", 3)),
                Enc("enc_convoy_main", "Convoy Ambush", "护航伏击", FactionTags.FrontierGuard, false, 5,
                    Slot("Magki", 5), Slot("Sernia", 5), Slot("Asra", 4)),
                Enc("enc_convoy_farm", "Convoy Escort Sweep", "护航清扫", FactionTags.FrontierGuard, false, 4,
                    Slot("Magki", 4), Slot("Asra", 4)),
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
    }
}

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
                Enc("enc_outer_main", "Outer Belt Patrol", false, 2, Slot("Asra", 1), Slot("Magki", 1)),
                Enc("enc_outer_farm", "Outer Belt Scraps", false, 1, Slot("Asra", 1)),
                Enc("enc_mining_main", "Mining Spur Raiders", false, 3, Slot("Magki", 2), Slot("Sernia", 2)),
                Enc("enc_mining_farm", "Mining Spur Sweep", false, 2, Slot("Magki", 2)),
                Enc("enc_rift_main", "Quantum Rift Wardens", false, 4, Slot("Asra", 3), Slot("Magki", 3), Slot("Sernia", 3)),
                Enc("enc_rift_farm", "Rift Echoes", false, 3, Slot("Asra", 3), Slot("Magki", 2)),
                Enc("enc_abyss_main", "Abyssal Edge Host", false, 5, Slot("Sernia", 4), Slot("Asra", 4), Slot("Magki", 4)),
                Enc("enc_abyss_farm", "Abyssal Drift", false, 4, Slot("Sernia", 4), Slot("Asra", 3)),
                Enc("enc_convoy_main", "Convoy Ambush", false, 5, Slot("Magki", 5), Slot("Sernia", 5), Slot("Asra", 4)),
                Enc("enc_convoy_farm", "Convoy Escort Sweep", false, 4, Slot("Magki", 4), Slot("Asra", 4)),
                Enc("enc_frontier_boss", "Frontier Anchor Boss", true, 12,
                    Slot("Sernia", 6), Slot("Asra", 6), Slot("Magki", 6), Slot("Sernia", 5)),
                Enc("enc_frontier_farm", "Anchor Debris Field", false, 6, Slot("Asra", 5), Slot("Magki", 5))
            };
        }

        private static EncounterConfig Enc(
            string id, string name, bool boss, int loot, params EncounterEnemySlot[] enemies) =>
            new EncounterConfig
            {
                encounterId = id,
                displayName = name,
                isBoss = boss,
                lootScrap = loot,
                enemies = enemies
            };

        private static EncounterEnemySlot Slot(string key, int level) =>
            new EncounterEnemySlot { characterKey = key, level = level, weight = 1 };
    }
}

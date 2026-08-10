using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    public static class ShipModuleCatalog
    {
        private static List<ShipModuleDef> cached;

        public static IReadOnlyList<ShipModuleDef> All
        {
            get
            {
                cached ??= BuildDefaults();
                return cached;
            }
        }

        public static ShipModuleDef Get(string moduleId)
        {
            foreach (var m in All)
            {
                if (m.moduleId == moduleId)
                    return m;
            }

            return null;
        }

        public static List<ShipModuleDef> BuildDefaults() => new List<ShipModuleDef>
        {
            Mod("mod_propulsion", "Propulsion", "推进", "range", 5, 10),
            Mod("mod_reactor", "Reactor Core", "能源核心", "energy", 5, 10),
            Mod("mod_armor", "Armor Forge", "装甲工坊", "hull", 6, 12),
            Mod("mod_entropy", "Entropy Shield", "熵雾屏障", "entropyResist", 6, 12),
            Mod("mod_cargo", "Cargo Bay", "货舱扩展", "cargo", 4, 8),
            Mod("mod_scanner", "Scan Array", "扫描阵列", "scan", 4, 8),
            Mod("mod_life_support", "Life Support", "生命维持", "lifeSupport", 7, 14),
            Mod("mod_automation", "Automation Core", "自动化核心", "energy", 8, 16),
            Mod("mod_command", "Command Hub", "调度中枢", "scan", 8, 16)
        };

        private static ShipModuleDef Mod(
            string id, string en, string zh, string stat, int scrap, int credit) =>
            new ShipModuleDef
            {
                moduleId = id,
                displayNameEn = en,
                displayNameZh = zh,
                primaryStat = stat,
                scrapCostPerLevel = scrap,
                creditCostPerLevel = credit,
                statPerLevel = 1
            };
    }
}

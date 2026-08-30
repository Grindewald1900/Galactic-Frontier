using System.Collections.Generic;

namespace Assets.Resources.Scripts.UI.Nexus
{
    internal enum BridgeLogCategory
    {
        Explore = 1,
        Combat = 2,
        Production = 3,
        Trade = 4,
        System = 5
    }

    internal sealed class BridgeLogEntry
    {
        public string Id;
        public BridgeLogCategory Category;
        public string En;
        public string Zh;
        public bool Live;
    }

    /// <summary>In-memory situation log for the Bridge panel. Not persisted (MVP).</summary>
    internal static class BridgeEventLog
    {
        private static readonly List<BridgeLogEntry> Entries = new List<BridgeLogEntry>();
        private static bool seeded;

        public static void Ensure()
        {
            if (seeded) return;
            seeded = true;
            Add("seed_explore", BridgeLogCategory.Explore,
                "Radar ping: uncharted body on the outer belt.",
                "雷达回波：外带发现未标明天体。");
            Add("seed_combat", BridgeLogCategory.Combat,
                "Auto-battle rules active — no manual turns required.",
                "全自动战斗规则生效 — 无需手动回合。");
            Add("seed_prod", BridgeLogCategory.Production,
                "Workshop cycle queued. Gather bank will settle on claim.",
                "工坊周期已排队。采集仓可在领取时结算。");
            Add("seed_trade", BridgeLogCategory.Trade,
                "NPC stall restocked copper and water.",
                "NPC 货摊补货：铜材与水源。");
            Add("seed_sys", BridgeLogCategory.System,
                "Command license issued. Profile and frames are in Settings.",
                "开拓许可证已签发。资料与头像框见设置。");
        }

        public static void UpsertLive(string id, BridgeLogCategory category, string en, string zh)
        {
            Ensure();
            if (string.IsNullOrEmpty(id)) return;
            for (var i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].Id == id)
                {
                    Entries[i].Category = category;
                    Entries[i].En = en;
                    Entries[i].Zh = zh;
                    Entries[i].Live = true;
                    return;
                }
            }

            Entries.Insert(0, new BridgeLogEntry
            {
                Id = id,
                Category = category,
                En = en,
                Zh = zh,
                Live = true
            });
        }

        public static void Push(BridgeLogCategory category, string en, string zh = null)
        {
            Ensure();
            if (string.IsNullOrWhiteSpace(en) && string.IsNullOrWhiteSpace(zh)) return;
            Entries.Insert(0, new BridgeLogEntry
            {
                Id = "evt_" + Entries.Count,
                Category = category,
                En = en ?? zh,
                Zh = string.IsNullOrEmpty(zh) ? en : zh
            });
            const int cap = 40;
            while (Entries.Count > cap)
                Entries.RemoveAt(Entries.Count - 1);
        }

        public static List<BridgeLogEntry> Query(BridgeLogCategory? filter)
        {
            Ensure();
            var list = new List<BridgeLogEntry>();
            foreach (var entry in Entries)
            {
                if (entry == null) continue;
                if (filter.HasValue && entry.Category != filter.Value) continue;
                list.Add(entry);
            }

            return list;
        }

        private static void Add(string id, BridgeLogCategory category, string en, string zh)
        {
            Entries.Add(new BridgeLogEntry
            {
                Id = id,
                Category = category,
                En = en,
                Zh = zh
            });
        }
    }
}

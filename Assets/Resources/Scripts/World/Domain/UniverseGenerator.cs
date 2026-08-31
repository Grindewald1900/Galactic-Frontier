using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    /// <summary>Deterministic universe-layer sector graph (doc 23).</summary>
    public static class UniverseGenerator
    {
        private const float MapMin = 4f;
        private const float MapMax = 96f;

        private readonly struct RingBand
        {
            public readonly UniverseRingId Id;
            public readonly string En;
            public readonly string Zh;
            public readonly float MinDist;
            public readonly float MaxDist;
            public readonly int DiffMin;
            public readonly int DiffMax;

            public RingBand(
                UniverseRingId id, string en, string zh,
                float minDist, float maxDist, int diffMin, int diffMax)
            {
                Id = id;
                En = en;
                Zh = zh;
                MinDist = minDist;
                MaxDist = maxDist;
                DiffMin = diffMin;
                DiffMax = diffMax;
            }
        }

        private static readonly RingBand[] FullRings =
        {
            new(UniverseRingId.OuterRim, "Outer Rim", "外缘带", 0.80f, 1.00f, 1, 15),
            new(UniverseRingId.Pioneer, "Pioneer", "开拓带", 0.60f, 0.80f, 10, 30),
            new(UniverseRingId.Severance, "Severance", "断航带", 0.40f, 0.60f, 25, 50),
            new(UniverseRingId.Plane, "Plane", "位面带", 0.20f, 0.40f, 45, 75),
            new(UniverseRingId.Inner, "Inner", "内环带", 0.05f, 0.20f, 70, 95),
            new(UniverseRingId.Core, "Core", "中枢区", 0.00f, 0.05f, 90, 99)
        };

        private static readonly RingBand[] PilotRings =
        {
            new(UniverseRingId.OuterRim, "Outer Rim", "外缘带", 0.75f, 1.00f, 1, 15),
            new(UniverseRingId.Pioneer, "Pioneer", "开拓带", 0.50f, 0.75f, 10, 30),
            new(UniverseRingId.Severance, "Severance", "断航带", 0.25f, 0.50f, 25, 50),
            new(UniverseRingId.Core, "Core", "中枢区", 0.00f, 0.25f, 45, 95)
        };

        public static GeneratedUniverse Generate(int seed, UniverseGenOptions options = null)
        {
            options ??= new UniverseGenOptions();
            var rng = new Random(UniverseSeedUtil.MixToInt(seed));
            var universe = new GeneratedUniverse { seed = seed };
            universe.planeModifiers = RollPlaneModifiers(rng);

            float cx = 90f;
            float cy = 10f;
            universe.centerX = cx;
            universe.centerY = cy;

            var rings = options.useFullRings ? FullRings : PilotRings;
            var sectors = new List<GeneratedSector>();

            sectors.Add(CreatePinnedBirth(cx, cy));
            sectors.Add(CreatePinnedNexus(cx, cy));

            int extras = Math.Max(0, options.targetSectorCount - 2);
            var kindQueue = BuildKindQueue(extras, rng);
            int genIndex = 0;
            foreach (var kind in kindQueue)
            {
                var ring = PickRingForKind(kind, rings, rng);
                var pos = SamplePosition(cx, cy, ring, sectors, rng);
                sectors.Add(CreateGeneratedSector(kind, ring, pos.x, pos.y, genIndex++, rng));
            }

            ResolveOverlaps(sectors, cx, cy, rng);
            var routes = BuildRoutes(sectors, cx, cy, rng);
            AssignDifficulties(sectors, routes, cx, cy, rings, rng);
            AssignFactions(sectors, routes, rng);

            universe.sectors = sectors;
            universe.routes = routes;

            universe.validation = UniverseValidator.Validate(universe);
            int repairs = 0;
            while (!universe.validation.isValid && repairs < options.maxRepairPasses)
            {
                UniverseValidator.TryRepair(universe);
                universe.validation = UniverseValidator.Validate(universe);
                repairs++;
            }

            return universe;
        }

        private static PlaneModifiersState RollPlaneModifiers(Random rng)
        {
            var all = (PlaneBonusKind[])Enum.GetValues(typeof(PlaneBonusKind));
            var primary = all[rng.Next(0, all.Length)];
            PlaneBonusKind secondary;
            do secondary = all[rng.Next(0, all.Length)];
            while (secondary == primary);
            PlaneBonusKind gap;
            do gap = all[rng.Next(0, all.Length)];
            while (gap == primary || gap == secondary);

            return new PlaneModifiersState
            {
                primary = primary.ToString(),
                secondary = secondary.ToString(),
                gap = gap.ToString()
            };
        }

        private static GeneratedSector CreatePinnedBirth(float cx, float cy)
        {
            return new GeneratedSector
            {
                sectorId = WorldConstants.SectorId,
                x = 18f,
                y = 82f,
                ring = UniverseRingId.OuterRim,
                kind = UniverseSectorKind.Birth,
                displayNameEn = "Frontier VII",
                displayNameZh = "第七前沿",
                playable = true,
                recommendedExpeditionLv = 1,
                danger = 1,
                difficulty = 5,
                factionTag = FactionTags.FrontierGuard,
                resourceProfile = "baseline",
                spriteIndex = 0,
                mainPathRequired = true
            };
        }

        private static GeneratedSector CreatePinnedNexus(float cx, float cy)
        {
            return new GeneratedSector
            {
                sectorId = WorldConstants.SectorCoreId,
                x = cx,
                y = cy,
                ring = UniverseRingId.Core,
                kind = UniverseSectorKind.Nexus,
                displayNameEn = "Astral Core",
                displayNameZh = "群星中枢",
                playable = false,
                recommendedExpeditionLv = 40,
                danger = 5,
                difficulty = 95,
                factionTag = "",
                resourceProfile = "nexus",
                spriteIndex = 15,
                mainPathRequired = true
            };
        }

        private static List<UniverseSectorKind> BuildKindQueue(int count, Random rng)
        {
            var list = new List<UniverseSectorKind>(count);
            int faction = Math.Max(1, count / 9);
            int resource = Math.Max(1, count / 6);
            int relic = Math.Max(1, count / 9);
            int entropy = Math.Max(1, count / 9);
            int special = Math.Max(1, count / 12);

            for (int i = 0; i < faction; i++) list.Add(UniverseSectorKind.FactionCore);
            for (int i = 0; i < resource; i++) list.Add(UniverseSectorKind.Resource);
            for (int i = 0; i < relic; i++) list.Add(UniverseSectorKind.Relic);
            for (int i = 0; i < entropy; i++) list.Add(UniverseSectorKind.EntropyHazard);
            for (int i = 0; i < special; i++) list.Add(UniverseSectorKind.Special);
            while (list.Count < count)
                list.Add(UniverseSectorKind.Normal);

            Shuffle(list, rng);
            return list;
        }

        private static RingBand PickRingForKind(UniverseSectorKind kind, RingBand[] rings, Random rng)
        {
            return kind switch
            {
                UniverseSectorKind.FactionCore => rings[Math.Min(1, rings.Length - 1)],
                UniverseSectorKind.Resource => rings[Math.Min(rng.Next(0, 2), rings.Length - 1)],
                UniverseSectorKind.Relic => rings[Math.Min(rng.Next(1, rings.Length), rings.Length - 1)],
                UniverseSectorKind.EntropyHazard => rings[Math.Min(rings.Length - 2, rings.Length - 1)],
                UniverseSectorKind.Special => rings[Math.Min(rng.Next(1, rings.Length), rings.Length - 1)],
                _ => rings[rng.Next(0, Math.Max(1, rings.Length - 1))]
            };
        }

        private static (float x, float y) SamplePosition(
            float cx, float cy, RingBand ring, List<GeneratedSector> existing, Random rng)
        {
            for (int attempt = 0; attempt < 48; attempt++)
            {
                float t = ring.MinDist + (float)rng.NextDouble() * (ring.MaxDist - ring.MinDist);
                float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                float radius = t * 42f;
                float x = cx + (float)Math.Cos(angle) * radius;
                float y = cy + (float)Math.Sin(angle) * radius;
                x = Clamp(x, MapMin, MapMax);
                y = Clamp(y, MapMin, MapMax);
                if (!TooClose(x, y, existing, 8f))
                    return (x, y);
            }

            return (cx + rng.Next(-30, 30), cy + rng.Next(20, 40));
        }

        private static void ResolveOverlaps(List<GeneratedSector> sectors, float cx, float cy, Random rng)
        {
            for (int pass = 0; pass < 6; pass++)
            {
                bool moved = false;
                for (int i = 0; i < sectors.Count; i++)
                {
                    var a = sectors[i];
                    if (a.kind is UniverseSectorKind.Birth or UniverseSectorKind.Nexus)
                        continue;
                    for (int j = i + 1; j < sectors.Count; j++)
                    {
                        var b = sectors[j];
                        if (b.kind is UniverseSectorKind.Birth or UniverseSectorKind.Nexus)
                            continue;
                        float dx = a.x - b.x;
                        float dy = a.y - b.y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (dist >= 7f || dist < 0.01f) continue;
                        float push = (7f - dist) * 0.55f;
                        a.x = Clamp(a.x + dx / dist * push, MapMin, MapMax);
                        a.y = Clamp(a.y + dy / dist * push, MapMin, MapMax);
                        moved = true;
                    }
                }

                if (!moved) break;
            }
        }

        private static GeneratedSector CreateGeneratedSector(
            UniverseSectorKind kind, RingBand ring, float x, float y, int index, Random rng)
        {
            var (en, zh) = NameForKind(kind, index, rng);
            return new GeneratedSector
            {
                sectorId = $"sector_gen_{index:D3}",
                x = x,
                y = y,
                ring = ring.Id,
                kind = kind,
                displayNameEn = en,
                displayNameZh = zh,
                playable = false,
                recommendedExpeditionLv = ring.DiffMin + rng.Next(0, 4),
                danger = Math.Min(5, 1 + (int)ring.Id),
                difficulty = ring.DiffMin,
                factionTag = kind == UniverseSectorKind.FactionCore
                    ? (rng.Next(0, 2) == 0 ? FactionTags.FrontierGuard : FactionTags.RiftSyndicate)
                    : "",
                resourceProfile = ResourceProfileForKind(kind),
                spriteIndex = rng.Next(0, 16),
                mainPathRequired = false
            };
        }

        private static string ResourceProfileForKind(UniverseSectorKind kind) => kind switch
        {
            UniverseSectorKind.Resource => "metal|energy|bio",
            UniverseSectorKind.Relic => "relic",
            UniverseSectorKind.EntropyHazard => "entropy",
            UniverseSectorKind.Special => "trade",
            _ => "mixed"
        };

        private static (string en, string zh) NameForKind(UniverseSectorKind kind, int index, Random rng)
        {
            return kind switch
            {
                UniverseSectorKind.FactionCore => ($"Sector Command {index + 1}", $"势力核心 {index + 1}"),
                UniverseSectorKind.Resource => ($"Resource Belt {index + 1}", $"资源带 {index + 1}"),
                UniverseSectorKind.Relic => ($"Relic Reach {index + 1}", $"遗迹星域 {index + 1}"),
                UniverseSectorKind.EntropyHazard => ($"Entropy Fault {index + 1}", $"熵雾断层 {index + 1}"),
                UniverseSectorKind.Special => ($"Relay Hub {index + 1}", $"中继站 {index + 1}"),
                _ => ($"Frontier Sector {index + 1}", $"边境星域 {index + 1}")
            };
        }

        private static void AssignDifficulties(
            List<GeneratedSector> sectors,
            List<GeneratedRoute> routes,
            float cx, float cy,
            RingBand[] rings,
            Random rng)
        {
            var birth = FindSector(sectors, WorldConstants.SectorId);
            var depths = routes == null
                ? new Dictionary<string, int>()
                : ComputeRouteDepths(sectors, routes, WorldConstants.SectorId);

            foreach (var s in sectors)
            {
                if (s == null) continue;
                var band = RingBandOf(s.ring, rings);
                int ringBase = (band.DiffMin + band.DiffMax) / 2;
                int depth = depths.TryGetValue(s.sectorId, out var d) ? d : 0;
                int entropy = s.kind == UniverseSectorKind.EntropyHazard ? 3 : 1;
                int factionMod = s.kind == UniverseSectorKind.FactionCore ? 2 : 0;
                int jitter = rng.Next(-3, 4);
                s.difficulty = Math.Max(1, Math.Min(99, ringBase + depth * 2 + entropy * 3 + factionMod + jitter));
                s.recommendedExpeditionLv = Math.Max(1, Math.Min(50, s.difficulty / 2 + 1));
                s.danger = Math.Max(1, Math.Min(5, 1 + s.difficulty / 20));
            }

            if (birth != null)
            {
                birth.difficulty = Math.Min(birth.difficulty, 15);
                birth.recommendedExpeditionLv = 1;
            }
        }

        private static void AssignFactions(List<GeneratedSector> sectors, List<GeneratedRoute> routes, Random rng)
        {
            var cores = new List<GeneratedSector>();
            foreach (var s in sectors)
            {
                if (s?.kind == UniverseSectorKind.FactionCore)
                    cores.Add(s);
            }

            foreach (var core in cores)
            {
                if (string.IsNullOrEmpty(core.factionTag))
                    core.factionTag = rng.Next(0, 2) == 0
                        ? FactionTags.FrontierGuard
                        : FactionTags.RiftSyndicate;
                ExpandFaction(sectors, routes, core, core.factionTag, hops: 2, rng);
            }
        }

        private static void ExpandFaction(
            List<GeneratedSector> sectors,
            List<GeneratedRoute> routes,
            GeneratedSector origin,
            string tag,
            int hops,
            Random rng)
        {
            if (origin == null || hops <= 0) return;
            var visited = new HashSet<string> { origin.sectorId };
            var frontier = new List<string> { origin.sectorId };
            for (int h = 0; h < hops; h++)
            {
                var next = new List<string>();
                foreach (var id in frontier)
                {
                    foreach (var route in routes)
                    {
                        if (route == null) continue;
                        string other = route.fromId == id ? route.toId
                            : route.toId == id ? route.fromId : null;
                        if (other == null || visited.Contains(other)) continue;
                        var node = FindSector(sectors, other);
                        if (node == null || node.kind == UniverseSectorKind.Nexus) continue;
                        if (string.IsNullOrEmpty(node.factionTag) && rng.Next(0, 100) < 70)
                            node.factionTag = tag;
                        visited.Add(other);
                        next.Add(other);
                    }
                }

                frontier = next;
            }
        }

        private static List<GeneratedRoute> BuildRoutes(List<GeneratedSector> sectors, float cx, float cy, Random rng)
        {
            var routes = new List<GeneratedRoute>();
            var birth = WorldConstants.SectorId;
            var core = WorldConstants.SectorCoreId;

            // 1) Spine toward core
            AddSpineRoutes(sectors, routes, birth, core);

            // 2) MST over remaining nodes
            AddMinimumSpanningRoutes(sectors, routes);

            // 3) Lateral links within similar radius
            AddLateralRoutes(sectors, routes, rng);

            // 4) One risky shortcut
            AddWormholeShortcut(sectors, routes, rng);

            CapNodeDegree(sectors, routes, maxDegree: 4, rng);
            return routes;
        }

        private static void AddSpineRoutes(
            List<GeneratedSector> sectors, List<GeneratedRoute> routes, string fromId, string toId)
        {
            var path = GreedyPath(sectors, fromId, toId);
            for (int i = 0; i < path.Count - 1; i++)
                TryAddRoute(sectors, routes, path[i], path[i + 1], GridRouteTag.Military, 0.22f, rift: false, wormhole: false);
        }

        private static List<string> GreedyPath(List<GeneratedSector> sectors, string fromId, string toId)
        {
            var path = new List<string> { fromId };
            var visited = new HashSet<string> { fromId };
            var target = FindSector(sectors, toId);
            if (target == null) return path;

            string current = fromId;
            for (int step = 0; step < sectors.Count && current != toId; step++)
            {
                GeneratedSector best = null;
                float bestScore = float.MaxValue;
                foreach (var s in sectors)
                {
                    if (s == null || visited.Contains(s.sectorId)) continue;
                    float dToTarget = Dist(s, target);
                    float dFromCurrent = Dist(FindSector(sectors, current), s);
                    float score = dToTarget + dFromCurrent * 0.35f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = s;
                    }
                }

                if (best == null) break;
                path.Add(best.sectorId);
                visited.Add(best.sectorId);
                current = best.sectorId;
            }

            if (current != toId)
                path.Add(toId);
            return path;
        }

        private static void AddMinimumSpanningRoutes(List<GeneratedSector> sectors, List<GeneratedRoute> routes)
        {
            var connected = new HashSet<string>();
            foreach (var r in routes)
            {
                if (r == null) continue;
                connected.Add(r.fromId);
                connected.Add(r.toId);
            }

            if (connected.Count == 0)
                connected.Add(WorldConstants.SectorId);

            while (connected.Count < sectors.Count)
            {
                GeneratedRoute best = null;
                float bestDist = float.MaxValue;
                string bestOuter = null;
                foreach (var id in connected)
                {
                    var a = FindSector(sectors, id);
                    foreach (var b in sectors)
                    {
                        if (b == null || connected.Contains(b.sectorId)) continue;
                        float d = Dist(a, b);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestOuter = b.sectorId;
                            best = new GeneratedRoute { fromId = id, toId = b.sectorId };
                        }
                    }
                }

                if (best == null || bestOuter == null) break;
                TryAddRoute(sectors, routes, best.fromId, best.toId, GridRouteTag.Industry, 0.18f, false, false);
                connected.Add(bestOuter);
            }
        }

        private static void AddLateralRoutes(List<GeneratedSector> sectors, List<GeneratedRoute> routes, Random rng)
        {
            for (int i = 0; i < sectors.Count; i++)
            {
                var a = sectors[i];
                if (a == null) continue;
                int added = 0;
                for (int j = i + 1; j < sectors.Count && added < 1; j++)
                {
                    var b = sectors[j];
                    if (b == null || a.ring != b.ring) continue;
                    if (Dist(a, b) > 22f) continue;
                    if (NodeDegree(routes, a.sectorId) >= 4) continue;
                    if (TryAddRoute(sectors, routes, a.sectorId, b.sectorId, GridRouteTag.Trade, 0.16f, false, false))
                        added++;
                }
            }
        }

        private static void AddWormholeShortcut(List<GeneratedSector> sectors, List<GeneratedRoute> routes, Random rng)
        {
            var birth = FindSector(sectors, WorldConstants.SectorId);
            GeneratedSector far = null;
            float best = 0f;
            foreach (var s in sectors)
            {
                if (s == null || s.kind == UniverseSectorKind.Birth || s.kind == UniverseSectorKind.Nexus)
                    continue;
                float d = Dist(birth, s);
                if (d > best)
                {
                    best = d;
                    far = s;
                }
            }

            if (far != null && best > 35f)
                TryAddRoute(sectors, routes, birth.sectorId, far.sectorId, GridRouteTag.Rift, 0.45f, rift: true, wormhole: true);
        }

        private static void CapNodeDegree(
            List<GeneratedSector> sectors, List<GeneratedRoute> routes, int maxDegree, Random rng)
        {
            for (int guard = 0; guard < 32; guard++)
            {
                bool trimmed = false;
                foreach (var s in sectors)
                {
                    if (s == null) continue;
                    while (NodeDegree(routes, s.sectorId) > maxDegree)
                    {
                        int idx = FindRemovableRouteIndex(routes, s.sectorId);
                        if (idx < 0) break;
                        routes.RemoveAt(idx);
                        trimmed = true;
                    }
                }

                if (!trimmed) break;
            }
        }

        private static int FindRemovableRouteIndex(List<GeneratedRoute> routes, string nodeId)
        {
            for (int i = routes.Count - 1; i >= 0; i--)
            {
                var r = routes[i];
                if (r == null) continue;
                if (r.wormhole) continue;
                if (r.fromId == nodeId || r.toId == nodeId)
                    return i;
            }

            return -1;
        }

        private static bool TryAddRoute(
            List<GeneratedSector> sectors,
            List<GeneratedRoute> routes,
            string aId, string bId,
            GridRouteTag tag, float entropy, bool rift, bool wormhole)
        {
            if (string.IsNullOrEmpty(aId) || string.IsNullOrEmpty(bId) || aId == bId)
                return false;
            foreach (var existing in routes)
            {
                if (existing == null) continue;
                if ((existing.fromId == aId && existing.toId == bId)
                    || (existing.fromId == bId && existing.toId == aId))
                    return false;
            }

            routes.Add(new GeneratedRoute
            {
                edgeId = $"uroute_{aId}_{bId}",
                fromId = aId,
                toId = bId,
                route = tag,
                entropy = entropy,
                rift = rift,
                wormhole = wormhole
            });
            return true;
        }

        private static Dictionary<string, int> ComputeRouteDepths(
            List<GeneratedSector> sectors, List<GeneratedRoute> routes, string birthId)
        {
            var depths = new Dictionary<string, int> { [birthId] = 0 };
            var q = new Queue<string>();
            q.Enqueue(birthId);
            while (q.Count > 0)
            {
                var id = q.Dequeue();
                int d = depths[id];
                foreach (var r in routes)
                {
                    if (r == null) continue;
                    string other = r.fromId == id ? r.toId : r.toId == id ? r.fromId : null;
                    if (other == null || depths.ContainsKey(other)) continue;
                    depths[other] = d + 1;
                    q.Enqueue(other);
                }
            }

            return depths;
        }

        private static int NodeDegree(List<GeneratedRoute> routes, string nodeId)
        {
            int n = 0;
            foreach (var r in routes)
            {
                if (r == null) continue;
                if (r.fromId == nodeId || r.toId == nodeId)
                    n++;
            }

            return n;
        }

        private static GeneratedSector FindSector(List<GeneratedSector> sectors, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in sectors)
            {
                if (s != null && s.sectorId == id)
                    return s;
            }

            return null;
        }

        private static RingBand RingBandOf(UniverseRingId ring, RingBand[] rings)
        {
            foreach (var r in rings)
            {
                if (r.Id == ring) return r;
            }

            return rings[0];
        }

        private static float Dist(GeneratedSector a, GeneratedSector b)
        {
            if (a == null || b == null) return float.MaxValue;
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static bool TooClose(float x, float y, List<GeneratedSector> existing, float minDist)
        {
            foreach (var s in existing)
            {
                if (s == null) continue;
                float dx = x - s.x;
                float dy = y - s.y;
                if (dx * dx + dy * dy < minDist * minDist)
                    return true;
            }

            return false;
        }

        private static float Clamp(float v, float min, float max) =>
            v < min ? min : v > max ? max : v;

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

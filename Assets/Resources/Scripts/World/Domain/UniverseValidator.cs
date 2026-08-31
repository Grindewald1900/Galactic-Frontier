using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    public static class UniverseValidator
    {
        public static UniverseValidationResult Validate(GeneratedUniverse universe)
        {
            var result = new UniverseValidationResult { isValid = true };
            if (universe?.sectors == null || universe.routes == null)
            {
                AddIssue(result, "null_universe", "Universe graph is null.");
                return result;
            }

            var birth = WorldConstants.SectorId;
            var core = WorldConstants.SectorCoreId;
            if (Find(universe.sectors, birth) == null)
                AddIssue(result, "missing_birth", "Birth sector missing.");
            if (Find(universe.sectors, core) == null)
                AddIssue(result, "missing_core", "Core sector missing.");

            int birthDegree = Degree(universe.routes, birth);
            if (birthDegree < 2)
                AddIssue(result, "birth_degree", "Birth sector must have at least two routes.");

            if (!Reachable(universe.routes, birth, core))
                AddIssue(result, "core_unreachable", "Core is not reachable from birth.");

            if (CountReachable(universe.routes, birth) < universe.sectors.Count)
                AddIssue(result, "disconnected", "Some sectors are isolated from birth.");

            if (CountSimplePaths(universe.routes, birth, core) < 2)
                AddIssue(result, "single_path", "Need at least two distinct routes toward the core.");

            foreach (var s in universe.sectors)
            {
                if (s == null) continue;
                if (s.kind == UniverseSectorKind.Birth && s.difficulty > 20)
                    AddIssue(result, "birth_difficulty", "Birth difficulty too high.");
            }

            return result;
        }

        public static bool TryRepair(GeneratedUniverse universe)
        {
            if (universe == null) return false;
            bool changed = false;
            var birth = WorldConstants.SectorId;
            var core = WorldConstants.SectorCoreId;

            if (Degree(universe.routes, birth) < 2)
            {
                var neighbor = NearestUnconnected(universe, birth);
                if (neighbor != null)
                {
                    AddEdge(universe, birth, neighbor.sectorId, GridRouteTag.Trade, 0.2f);
                    changed = true;
                }
            }

            if (!Reachable(universe.routes, birth, core))
            {
                var step = NearestUnconnected(universe, birth);
                if (step != null)
                {
                    AddEdge(universe, birth, step.sectorId, GridRouteTag.Military, 0.25f);
                    changed = true;
                }
            }

            foreach (var s in universe.sectors)
            {
                if (s == null) continue;
                if (!Reachable(universe.routes, birth, s.sectorId))
                {
                    AddEdge(universe, birth, s.sectorId, GridRouteTag.Industry, 0.18f);
                    changed = true;
                }
            }

            if (CountSimplePaths(universe.routes, birth, core) < 2)
            {
                var alt = FindAlternateBridge(universe, birth, core);
                if (alt.a != null && alt.b != null)
                {
                    AddEdge(universe, alt.a, alt.b, GridRouteTag.Trade, 0.2f);
                    changed = true;
                }
            }

            var birthSector = Find(universe.sectors, birth);
            if (birthSector != null && birthSector.difficulty > 20)
            {
                birthSector.difficulty = 8;
                changed = true;
            }

            return changed;
        }

        private static (string a, string b) FindAlternateBridge(GeneratedUniverse u, string birth, string core)
        {
            foreach (var s in u.sectors)
            {
                if (s == null || s.sectorId == birth || s.sectorId == core) continue;
                if (Reachable(u.routes, birth, s.sectorId) && Reachable(u.routes, s.sectorId, core))
                    return (birth, s.sectorId);
            }

            foreach (var s in u.sectors)
            {
                if (s == null || s.sectorId == birth) continue;
                return (birth, s.sectorId);
            }

            return (null, null);
        }

        private static GeneratedSector NearestUnconnected(GeneratedUniverse u, string fromId)
        {
            var from = Find(u.sectors, fromId);
            if (from == null) return null;
            GeneratedSector best = null;
            float bestDist = float.MaxValue;
            foreach (var s in u.sectors)
            {
                if (s == null || s.sectorId == fromId) continue;
                if (HasEdge(u.routes, fromId, s.sectorId)) continue;
                float dx = from.x - s.x;
                float dy = from.y - s.y;
                float d = dx * dx + dy * dy;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }

            return best;
        }

        private static void AddEdge(
            GeneratedUniverse u, string a, string b, GridRouteTag tag, float entropy)
        {
            if (HasEdge(u.routes, a, b)) return;
            u.routes.Add(new GeneratedRoute
            {
                edgeId = $"repair_{a}_{b}",
                fromId = a,
                toId = b,
                route = tag,
                entropy = entropy
            });
        }

        private static bool HasEdge(List<GeneratedRoute> routes, string a, string b)
        {
            foreach (var r in routes)
            {
                if (r == null) continue;
                if ((r.fromId == a && r.toId == b) || (r.fromId == b && r.toId == a))
                    return true;
            }

            return false;
        }

        private static int CountReachable(List<GeneratedRoute> routes, string start)
        {
            var seen = new HashSet<string>();
            var q = new Queue<string>();
            q.Enqueue(start);
            seen.Add(start);
            while (q.Count > 0)
            {
                var id = q.Dequeue();
                foreach (var r in routes)
                {
                    if (r == null) continue;
                    string other = r.fromId == id ? r.toId : r.toId == id ? r.fromId : null;
                    if (other == null || seen.Contains(other)) continue;
                    seen.Add(other);
                    q.Enqueue(other);
                }
            }

            return seen.Count;
        }

        private static bool Reachable(List<GeneratedRoute> routes, string from, string to)
        {
            if (from == to) return true;
            var seen = new HashSet<string> { from };
            var q = new Queue<string>();
            q.Enqueue(from);
            while (q.Count > 0)
            {
                var id = q.Dequeue();
                foreach (var r in routes)
                {
                    if (r == null) continue;
                    string other = r.fromId == id ? r.toId : r.toId == id ? r.fromId : null;
                    if (other == null || seen.Contains(other)) continue;
                    if (other == to) return true;
                    seen.Add(other);
                    q.Enqueue(other);
                }
            }

            return false;
        }

        private static int CountSimplePaths(List<GeneratedRoute> routes, string from, string to)
        {
            int count = 0;
            var path = new List<string>();
            var visited = new HashSet<string>();
            DfsPath(routes, from, to, visited, path, ref count);
            return count;
        }

        private static void DfsPath(
            List<GeneratedRoute> routes,
            string current,
            string target,
            HashSet<string> visited,
            List<string> path,
            ref int count)
        {
            if (count >= 2) return;
            if (current == target)
            {
                count++;
                return;
            }

            visited.Add(current);
            path.Add(current);
            foreach (var r in routes)
            {
                if (r == null) continue;
                string other = r.fromId == current ? r.toId : r.toId == current ? r.fromId : null;
                if (other == null || visited.Contains(other)) continue;
                DfsPath(routes, other, target, visited, path, ref count);
            }

            path.RemoveAt(path.Count - 1);
            visited.Remove(current);
        }

        private static int Degree(List<GeneratedRoute> routes, string nodeId)
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

        private static GeneratedSector Find(List<GeneratedSector> sectors, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in sectors)
            {
                if (s != null && s.sectorId == id)
                    return s;
            }

            return null;
        }

        private static void AddIssue(UniverseValidationResult result, string code, string message)
        {
            result.isValid = false;
            result.issues.Add(new UniverseValidationIssue { code = code, message = message });
        }
    }
}

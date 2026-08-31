using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.World
{
    /// <summary>
    /// In-sector cruise: ship position, radar fog, sail to located nodes. Grid states live in GridService.
    /// </summary>
    public static class NavigationService
    {
        public static float ShipX => WorldService.State?.navX ?? SectorMapCatalog.SpawnX;
        public static float ShipY => WorldService.State?.navY ?? SectorMapCatalog.SpawnY;

        public static void EnsureReady()
        {
            WorldService.EnsureReady();
            var world = WorldService.State;
            if (world == null) return;

            world.knownBodyIds ??= new System.Collections.Generic.List<string>();
            if (world.navX <= 0.01f && world.navY <= 0.01f)
            {
                world.navX = SectorMapCatalog.SpawnX;
                world.navY = SectorMapCatalog.SpawnY;
            }

            GridService.EnsureReady();
            if (GridService.FogBodiesInRadar() > 0)
            {
                GridService.RecalcExplore();
                WorldService.Save();
            }
        }

        public static float GetRadarRange()
        {
            ShipService.EnsureReady();
            int scan = ShipService.GetModuleLevel("mod_scanner");
            return 12f + scan * 6f;
        }

        public static float GetCruiseSpeed()
        {
            ShipService.EnsureReady();
            int propulsion = ShipService.GetModuleLevel("mod_propulsion");
            return 8f + propulsion * 3f;
        }

        public static float DistanceTo(float x, float y)
        {
            float dx = x - ShipX;
            float dy = y - ShipY;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static float DistanceToBody(StellarBodyDef body) =>
            body == null ? float.MaxValue : DistanceTo(body.x, body.y);

        public static bool IsDocked(StellarBodyDef body) =>
            body != null && DistanceToBody(body) <= SectorMapCatalog.DockingRange;

        public static string DockedBodyId()
        {
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body != null && IsDocked(body))
                    return body.bodyId;
            }

            return "";
        }

        public static string NearestLocatedBodyId()
        {
            string best = DockedBodyId();
            if (!string.IsNullOrEmpty(best)) return best;

            float bestDist = float.MaxValue;
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null || !GridService.IsLocatedOrBetter(body.bodyId))
                    continue;
                float d = DistanceToBody(body);
                if (d >= bestDist) continue;
                bestDist = d;
                best = body.bodyId;
            }

            return best ?? "";
        }

        public static bool IsKnown(string bodyId) => GridService.IsLocatedOrBetter(bodyId);

        public static bool IsVisible(StellarBodyDef body)
        {
            if (body == null) return false;
            return GridService.GetState(body.bodyId) >= GridNodeState.Fogged;
        }

        public static void RememberBody(string bodyId) => GridService.EnsureLocated(bodyId);

        public static int RevealRadar()
        {
            int newly = GridService.FogBodiesInRadar();
            if (newly > 0)
                GridService.RecalcExplore();
            return newly;
        }

        public static WorldCommandResult TryCruiseTo(float x, float y)
        {
            EnsureReady();
            x = Mathf.Clamp(x, SectorMapCatalog.MapMin, SectorMapCatalog.MapMax);
            y = Mathf.Clamp(y, SectorMapCatalog.MapMin, SectorMapCatalog.MapMax);

            float dist = DistanceTo(x, y);
            if (dist < 0.5f)
                return WorldCommandResult.Fail("Already at destination.");

            var world = WorldService.State;
            world.navX = x;
            world.navY = y;
            int revealed = RevealRadar();
            GridService.RecalcExplore();
            WorldService.Save();
            return WorldCommandResult.OkMessage(
                revealed > 0
                    ? $"Arrived. Radar painted {revealed} entropy silhouette(s)."
                    : "Arrived.");
        }

        public static WorldCommandResult TryCruiseToBody(string bodyId)
        {
            var body = SectorMapCatalog.Get(bodyId);
            if (body == null)
                return WorldCommandResult.Fail("Unknown body.");
            if (GridService.GetState(bodyId) < GridNodeState.Located)
                return WorldCommandResult.Fail("Target is not located. Probe the fogged signal first.");

            string from = DockedBodyId();
            var result = TryCruiseTo(body.x, body.y);
            if (result.Success)
            {
                GridService.OnArrived(from, bodyId);
                WorldService.Save();
            }

            return result;
        }

        public static WorldCommandResult TryCruiseRandom(float distance)
        {
            EnsureReady();
            distance = Mathf.Clamp(distance, 8f, 35f);
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float x = ShipX + Mathf.Cos(angle) * distance;
            float y = ShipY + Mathf.Sin(angle) * distance;
            return TryCruiseTo(x, y);
        }

        public static float EstimateCruiseSeconds(float distance, StellarBodyDef target = null)
        {
            float speed = Mathf.Max(1f, GetCruiseSpeed());
            float seconds = distance / speed * 60f;
            if (target != null)
                seconds *= GridService.EstimateCruise(target).Multiplier;
            return seconds;
        }
    }
}

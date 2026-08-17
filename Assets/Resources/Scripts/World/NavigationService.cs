using System;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.World
{
    /// <summary>
    /// M1 stellar navigation: ship position, radar reveal, and in-sector cruise (no jump yet).
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

            // Starter: outer haven is always charted so the player has a first docking target.
            RememberBody("body_outer_haven");
            if (RevealRadar() > 0)
                WorldService.Save();
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

        public static bool IsKnown(string bodyId)
        {
            var world = WorldService.State;
            if (world?.knownBodyIds == null || string.IsNullOrEmpty(bodyId)) return false;
            foreach (var id in world.knownBodyIds)
            {
                if (id == bodyId) return true;
            }

            return false;
        }

        public static bool IsVisible(StellarBodyDef body)
        {
            if (body == null) return false;
            if (IsKnown(body.bodyId)) return true;
            return DistanceToBody(body) <= GetRadarRange();
        }

        public static void RememberBody(string bodyId)
        {
            var world = WorldService.State;
            if (world == null || string.IsNullOrEmpty(bodyId)) return;
            world.knownBodyIds ??= new System.Collections.Generic.List<string>();
            if (IsKnown(bodyId)) return;
            world.knownBodyIds.Add(bodyId);
        }

        public static int RevealRadar()
        {
            int newly = 0;
            float range = GetRadarRange();
            foreach (var body in SectorMapCatalog.Bodies)
            {
                if (body == null || IsKnown(body.bodyId)) continue;
                if (DistanceToBody(body) <= range)
                {
                    RememberBody(body.bodyId);
                    newly++;
                }
            }

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
            WorldService.Save();
            return WorldCommandResult.OkMessage(
                revealed > 0
                    ? $"Arrived. Radar locked {revealed} new signal(s)."
                    : "Arrived.");
        }

        public static WorldCommandResult TryCruiseToBody(string bodyId)
        {
            var body = SectorMapCatalog.Get(bodyId);
            if (body == null)
                return WorldCommandResult.Fail("Unknown body.");
            if (!IsVisible(body))
                return WorldCommandResult.Fail("Target is outside radar / chart coverage.");

            var result = TryCruiseTo(body.x, body.y);
            if (result.Success)
                RememberBody(bodyId);
            WorldService.Save();
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

        public static float EstimateCruiseSeconds(float distance)
        {
            float speed = Mathf.Max(1f, GetCruiseSpeed());
            return distance / speed * 60f; // flavor ETA in seconds for UI
        }
    }
}

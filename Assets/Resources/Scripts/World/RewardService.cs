using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.World
{
    /// <summary>Grants region FirstClear / Repeat / Farm loot into warehouse or pending.</summary>
    public static class RewardService
    {
        public sealed class GrantResult
        {
            public bool Success = true;
            public string TableId = "";
            public int CreditsGranted;
            public int ItemsGranted;
            public int ItemsPending;
            public string Message = "";
        }

        public static GrantResult GrantForRegionVictory(string regionId, bool wasFirstClear)
        {
            return GrantForRegionVictory(regionId, new WorldCommandResult { WasFirstClear = wasFirstClear });
        }

        public static GrantResult GrantForRegionVictory(string regionId, WorldCommandResult victory)
        {
            var cfg = RegionCatalog.Get(regionId);
            if (cfg == null)
                return new GrantResult { Success = false, Message = "Unknown region." };

            if (victory != null && victory.WasFirstClear)
                return GrantTable(cfg.firstClearRewardId, preferPending: false);

            if (victory != null && victory.WasStageFirstClear && !string.IsNullOrEmpty(victory.StageRewardTableId))
                return GrantTable(victory.StageRewardTableId, preferPending: false);

            if (WorldService.IsLoaded && WorldRules.IsRegionBeaconRestored(WorldService.State, regionId))
                return GrantTable(cfg.repeatClearRewardId, preferPending: false);

            return new GrantResult { Success = true, Message = "No campaign reward." };
        }

        public static GrantResult GrantForFarmCycle(string regionId)
        {
            var cfg = RegionCatalog.Get(regionId);
            if (cfg == null)
                return new GrantResult { Success = false, Message = "Unknown region." };

            return GrantTable(cfg.farmRewardId, preferPending: false);
        }

        public static GrantResult GrantTable(string tableId, bool preferPending, int? seed = null)
        {
            var result = new GrantResult { TableId = tableId ?? "" };
            var table = RewardCatalog.Get(tableId);
            if (table == null)
            {
                result.Success = false;
                result.Message = "Missing reward table: " + tableId;
                return result;
            }

            var rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random(Environment.TickCount);
            var grants = RewardRules.Resolve(table, rng);
            IdleSettlementService.EnsureLoaded();

            foreach (var grant in grants)
            {
                if (grant == null) continue;
                if (grant.IsCredit)
                {
                    CurrencyService.AddCredits(grant.quantity);
                    result.CreditsGranted += grant.quantity;
                    continue;
                }

                var item = ItemFactory.FromDef(grant.itemDefId, grant.quantity, grant.quality);
                if (preferPending || !ProductionService.TryAddLocal(item))
                {
                    IdleSettlementService.EnqueuePending(
                        grant.itemDefId, grant.quantity, grant.quality, "region_reward");
                    result.ItemsPending++;
                }
                else
                {
                    result.ItemsGranted++;
                }
            }

            IdleSettlementService.Save();
            Debug.Log(
                $"[REWARD] table={tableId} credits={result.CreditsGranted} items={result.ItemsGranted} pending={result.ItemsPending}");
            return result;
        }
    }
}

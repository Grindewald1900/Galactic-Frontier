using System.Collections.Generic;

namespace Assets.Resources.Scripts.Unlock.Domain
{
    /// <summary>Pure unlock evaluation. No Unity / save I/O.</summary>
    public static class FeatureUnlockRules
    {
        public static bool ContainsId(List<string> ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id)) return false;
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id) return true;
            }

            return false;
        }

        public static void AddUnique(List<string> ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id)) return;
            if (!ContainsId(ids, id))
                ids.Add(id);
        }

        public static bool IsConditionMet(FeatureUnlockDef def, FeatureUnlockContext ctx)
        {
            if (def == null) return false;
            if (def.startsUnlocked) return true;
            if (ctx == null) return false;
            if (ctx.ChainCompleted) return true;

            if (!string.IsNullOrEmpty(def.requiredStepId)
                && !ContainsId(ctx.ClaimedStepIds, def.requiredStepId))
                return false;

            if (def.requiredShipLevel > 0 && ctx.ShipLevel < def.requiredShipLevel)
                return false;

            if (!string.IsNullOrEmpty(def.requiredRegionId)
                && !ContainsId(ctx.ClearedRegionIds, def.requiredRegionId))
                return false;

            return true;
        }

        /// <summary>
        /// Ids that are newly met this pass. Mutates <paramref name="state"/> unlocked list.
        /// When <paramref name="seed"/> is true, newly met ids are also marked announced.
        /// </summary>
        public static List<string> ApplyNewlyMet(
            FeatureUnlockState state,
            IList<FeatureUnlockDef> catalog,
            FeatureUnlockContext ctx,
            bool seed)
        {
            var newly = new List<string>();
            if (state == null || catalog == null) return newly;
            state.unlockedFeatureIds ??= new List<string>();
            state.announcedFeatureIds ??= new List<string>();

            foreach (var def in catalog)
            {
                if (def == null || string.IsNullOrEmpty(def.featureId)) continue;
                if (!IsConditionMet(def, ctx)) continue;
                if (ContainsId(state.unlockedFeatureIds, def.featureId)) continue;

                AddUnique(state.unlockedFeatureIds, def.featureId);
                if (seed)
                    AddUnique(state.announcedFeatureIds, def.featureId);
                else
                    newly.Add(def.featureId);
            }

            if (seed)
                state.seeded = true;

            state.count = state.unlockedFeatureIds.Count;
            return newly;
        }

        public static List<string> PendingAnnouncements(FeatureUnlockState state)
        {
            var list = new List<string>();
            if (state?.unlockedFeatureIds == null) return list;
            foreach (var id in state.unlockedFeatureIds)
            {
                if (!ContainsId(state.announcedFeatureIds, id))
                    list.Add(id);
            }

            return list;
        }
    }
}

using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Unlock.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.Unlock
{
    /// <summary>Persists and evaluates gradual feature unlocks (nav grey-out + announcement queue).</summary>
    public static class FeatureUnlockService
    {
        public static FeatureUnlockState State { get; private set; }
        public static bool IsLoaded => State != null;

        public static event Action UnlocksChanged;

        public static void Clear()
        {
            State = null;
            FeatureUnlockCatalog.ClearCache();
        }

        public static void EnsureLoaded(DataUtil dataUtil = null)
        {
            var util = dataUtil ?? DataUtil.Instance;
            if (State != null) return;

            if (util == null)
            {
                State = new FeatureUnlockState();
                FeatureUnlockRules.ApplyNewlyMet(State, CopyCatalog(), BuildContext(), seed: true);
                return;
            }

            State = util.LoadFeatureUnlockState() ?? new FeatureUnlockState();
            bool seed = !State.seeded;
            FeatureUnlockRules.ApplyNewlyMet(State, CopyCatalog(), BuildContext(), seed);
            util.SaveFeatureUnlockState(State, touchMeta: !seed);
        }

        public static void Save(DataUtil dataUtil = null)
        {
            if (State == null) return;
            var util = dataUtil ?? DataUtil.Instance;
            util?.SaveFeatureUnlockState(State, touchMeta: true);
        }

        /// <summary>Re-evaluate catalog against live onboarding / ship / world. Returns newly unlocked ids.</summary>
        public static List<string> Evaluate()
        {
            EnsureLoaded();
            var newly = FeatureUnlockRules.ApplyNewlyMet(State, CopyCatalog(), BuildContext(), seed: false);
            if (newly.Count == 0)
                return newly;

            Save();
            UnlocksChanged?.Invoke();
            return newly;
        }

        public static bool IsFeatureUnlocked(string featureId)
        {
            EnsureLoaded();
            return FeatureUnlockRules.ContainsId(State.unlockedFeatureIds, featureId);
        }

        public static bool IsScreenUnlocked(string navScreen)
        {
            var def = FeatureUnlockCatalog.ForScreen(navScreen);
            if (def == null) return true;
            return IsFeatureUnlocked(def.featureId);
        }

        public static FeatureUnlockDef DefForScreen(string navScreen) =>
            FeatureUnlockCatalog.ForScreen(navScreen);

        public static List<string> PendingAnnouncements()
        {
            EnsureLoaded();
            return FeatureUnlockRules.PendingAnnouncements(State);
        }

        public static void MarkAnnounced(string featureId)
        {
            EnsureLoaded();
            FeatureUnlockRules.AddUnique(State.announcedFeatureIds, featureId);
            Save();
        }

        public static FeatureUnlockContext BuildContext()
        {
            var ctx = new FeatureUnlockContext();
            OnboardingService.EnsureLoaded();
            var ob = OnboardingService.State;
            if (ob != null)
            {
                ctx.ChainCompleted = ob.chainCompleted;
                if (ob.claimedStepIds != null)
                    ctx.ClaimedStepIds.AddRange(ob.claimedStepIds);
            }

            ShipService.EnsureReady();
            ctx.ShipLevel = ShipService.State != null ? Mathf.Max(1, ShipService.State.level) : 1;

            try
            {
                if (WorldService.IsLoaded)
                {
                    foreach (var view in WorldService.GetAllRegionViews())
                    {
                        if (view?.Config == null) continue;
                        var p = view.Progress;
                        if (p == RegionProgressState.Cleared || p == RegionProgressState.BossDefeated)
                            ctx.ClearedRegionIds.Add(view.Config.regionId);
                    }
                }
            }
            catch (Exception)
            {
                // World not ready during early boot; region gates stay unmet.
            }

            return ctx;
        }

        private static List<FeatureUnlockDef> CopyCatalog()
        {
            var list = new List<FeatureUnlockDef>();
            foreach (var def in FeatureUnlockCatalog.All)
            {
                if (def != null) list.Add(def);
            }

            return list;
        }
    }
}

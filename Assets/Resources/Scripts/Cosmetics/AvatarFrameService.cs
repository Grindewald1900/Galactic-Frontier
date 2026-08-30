using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Cosmetics.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.Cosmetics
{
    /// <summary>Equipped avatar frame + unlock set on the current <see cref="PlayerEntity"/>.</summary>
    public static class AvatarFrameService
    {
        public static event Action UnlocksChanged;

        public static PlayerEntity Player => DataUtil.Instance?.currentPlayer;

        public static void EnsurePlayer(PlayerEntity player = null)
        {
            var p = player ?? Player;
            if (p == null) return;
            p.unlockedAvatarFrameIds ??= new List<string>();
            p.announcedAvatarFrameIds ??= new List<string>();
            AvatarFrameRules.AddUnique(p.unlockedAvatarFrameIds, AvatarFrameIds.Default);
            if (string.IsNullOrEmpty(p.avatarFrameId) || AvatarFrameCatalog.TryGet(p.avatarFrameId) == null)
                p.avatarFrameId = AvatarFrameIds.Default;
        }

        /// <summary>
        /// Grant achievement frames from onboarding progress.
        /// First run seeds already-earned frames as announced so old saves are not spammed.
        /// </summary>
        public static List<string> Evaluate()
        {
            var newly = new List<string>();
            EnsurePlayer();
            var p = Player;
            if (p == null) return newly;

            OnboardingService.EnsureLoaded();
            var claimed = OnboardingService.State?.claimedStepIds;
            bool chainDone = OnboardingService.IsChainComplete;
            bool seed = !p.avatarFramesSeeded;
            bool dirty = false;

            foreach (var def in AvatarFrameCatalog.All)
            {
                if (def == null || def.startsUnlocked) continue;
                if (def.source != AvatarFrameSource.Achievement) continue;
                bool met = !string.IsNullOrEmpty(def.requiredStepId)
                    ? AvatarFrameRules.ContainsId(claimed, def.requiredStepId)
                    : chainDone;
                if (!met) continue;
                if (!UnlockInternal(p, def.frameId)) continue;
                dirty = true;
                if (seed)
                    AvatarFrameRules.AddUnique(p.announcedAvatarFrameIds, def.frameId);
                else
                    newly.Add(def.frameId);
            }

            if (seed)
            {
                foreach (var id in p.unlockedAvatarFrameIds)
                    AvatarFrameRules.AddUnique(p.announcedAvatarFrameIds, id);
                p.avatarFramesSeeded = true;
                dirty = true;
            }

            if (dirty)
                Persist(p);
            if (newly.Count > 0)
                UnlocksChanged?.Invoke();
            return newly;
        }

        public static bool IsUnlocked(string frameId)
        {
            EnsurePlayer();
            var def = AvatarFrameCatalog.TryGet(frameId) ?? AvatarFrameCatalog.Get(frameId);
            return AvatarFrameRules.IsUnlocked(def, Player?.unlockedAvatarFrameIds);
        }

        public static AvatarFrameDef Equipped()
        {
            EnsurePlayer();
            return AvatarFrameCatalog.Get(Player?.avatarFrameId);
        }

        public static bool TryEquip(string frameId)
        {
            EnsurePlayer();
            var p = Player;
            if (p == null) return false;
            var def = AvatarFrameCatalog.TryGet(frameId);
            if (def == null || !AvatarFrameRules.CanEquip(def, p.unlockedAvatarFrameIds))
                return false;
            p.avatarFrameId = def.frameId;
            Persist(p);
            return true;
        }

        public static bool Unlock(string frameId)
        {
            EnsurePlayer();
            var p = Player;
            if (p == null) return false;
            if (!UnlockInternal(p, frameId))
                return false;
            p.avatarFrameId = frameId;
            Persist(p);
            UnlocksChanged?.Invoke();
            return true;
        }

        public static List<string> PendingAnnouncements()
        {
            EnsurePlayer();
            var pending = new List<string>();
            var p = Player;
            if (p?.unlockedAvatarFrameIds == null) return pending;
            foreach (var id in p.unlockedAvatarFrameIds)
            {
                if (string.IsNullOrEmpty(id) || id == AvatarFrameIds.Default) continue;
                if (AvatarFrameCatalog.TryGet(id) == null) continue;
                if (AvatarFrameRules.ContainsId(p.announcedAvatarFrameIds, id)) continue;
                pending.Add(id);
            }

            return pending;
        }

        public static void MarkAnnounced(string frameId)
        {
            EnsurePlayer();
            var p = Player;
            if (p == null || string.IsNullOrEmpty(frameId)) return;
            AvatarFrameRules.AddUnique(p.announcedAvatarFrameIds, frameId);
            Persist(p);
        }

        private static bool UnlockInternal(PlayerEntity player, string frameId)
        {
            if (player == null || string.IsNullOrEmpty(frameId)) return false;
            if (AvatarFrameCatalog.TryGet(frameId) == null) return false;
            if (AvatarFrameRules.ContainsId(player.unlockedAvatarFrameIds, frameId))
                return false;
            AvatarFrameRules.AddUnique(player.unlockedAvatarFrameIds, frameId);
            return true;
        }

        private static void Persist(PlayerEntity player)
        {
            DataUtil.Instance?.SavePlayerData(player);
        }
    }
}

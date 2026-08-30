using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.Cosmetics.Domain
{
    public enum AvatarFrameSource
    {
        Starter = 0,
        Achievement = 1,
        GiftCode = 2,
        Event = 3
    }

    [Serializable]
    public class AvatarFrameDef
    {
        public string frameId = "";
        public string titleEn = "";
        public string titleZh = "";
        public string hintEn = "";
        public string hintZh = "";
        public AvatarFrameSource source = AvatarFrameSource.Starter;
        public bool startsUnlocked;
        public string requiredStepId = "";
        public string giftCode = "";
        public string eventId = "";
        public string colorHex = "#64748B";
    }

    /// <summary>Persisted on <c>PlayerEntity</c>; helpers keep lists non-null.</summary>
    public static class AvatarFrameIds
    {
        public const string Default = "frame_default";
        public const string Pioneer = "frame_pioneer";
        public const string FirstVictory = "frame_first_victory";
        public const string Beta = "frame_beta";
        public const string EventRift = "frame_event_rift";
    }

    public static class AvatarFrameRules
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

        public static bool IsUnlocked(AvatarFrameDef def, List<string> unlockedIds)
        {
            if (def == null) return false;
            if (def.startsUnlocked) return true;
            return ContainsId(unlockedIds, def.frameId);
        }

        public static bool CanEquip(AvatarFrameDef def, List<string> unlockedIds) =>
            IsUnlocked(def, unlockedIds);
    }
}

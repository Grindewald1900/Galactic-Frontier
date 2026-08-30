using System.Collections.Generic;

namespace Assets.Resources.Scripts.Cosmetics.Domain
{
    /// <summary>Static avatar-frame catalog. Unlock sources: starter, achievement, gift code, event.</summary>
    public static class AvatarFrameCatalog
    {
        private static readonly List<AvatarFrameDef> Frames = new List<AvatarFrameDef>
        {
            new AvatarFrameDef
            {
                frameId = AvatarFrameIds.Default,
                titleEn = "Frontier Issue",
                titleZh = "开拓制式",
                hintEn = "Issued with your command license.",
                hintZh = "随开拓许可证发放。",
                source = AvatarFrameSource.Starter,
                startsUnlocked = true,
                colorHex = "#64748B"
            },
            new AvatarFrameDef
            {
                frameId = AvatarFrameIds.Pioneer,
                titleEn = "Pioneer Crest",
                titleZh = "开拓纹章",
                hintEn = "Complete the starter mission chain.",
                hintZh = "完成新手航线全部步骤。",
                source = AvatarFrameSource.Achievement,
                colorHex = "#E8A832"
            },
            new AvatarFrameDef
            {
                frameId = AvatarFrameIds.FirstVictory,
                titleEn = "First Clear",
                titleZh = "首胜之环",
                hintEn = "Claim the First Battle mission.",
                hintZh = "领取「首场清剿」任务。",
                source = AvatarFrameSource.Achievement,
                requiredStepId = "ob_first_battle",
                colorHex = "#38BDF8"
            },
            new AvatarFrameDef
            {
                frameId = AvatarFrameIds.Beta,
                titleEn = "Beta Halo",
                titleZh = "内测光环",
                hintEn = "Redeem gift code GF-FRAME.",
                hintZh = "兑换礼品码 GF-FRAME。",
                source = AvatarFrameSource.GiftCode,
                giftCode = "GF-FRAME",
                colorHex = "#A78BFA"
            },
            new AvatarFrameDef
            {
                frameId = AvatarFrameIds.EventRift,
                titleEn = "Rift Festival",
                titleZh = "裂隙庆典",
                hintEn = "Limited event, or gift code EVENT-RIFT.",
                hintZh = "限时活动，或兑换礼品码 EVENT-RIFT。",
                source = AvatarFrameSource.Event,
                eventId = "evt_rift_festival",
                giftCode = "EVENT-RIFT",
                colorHex = "#F87171"
            }
        };

        public static IReadOnlyList<AvatarFrameDef> All => Frames;

        public static AvatarFrameDef TryGet(string frameId)
        {
            if (string.IsNullOrEmpty(frameId)) return null;
            foreach (var def in Frames)
            {
                if (def != null && def.frameId == frameId)
                    return def;
            }

            return null;
        }

        public static AvatarFrameDef Get(string frameId) =>
            TryGet(frameId) ?? TryGet(AvatarFrameIds.Default);
    }
}

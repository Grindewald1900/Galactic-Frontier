using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Gacha;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Unlock;
using Assets.Resources.Scripts.World;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils
{
    /// <summary>
    /// Flushes and clears in-memory per-player services when switching saves.
    /// Without this, static caches (chapter quests, onboarding, etc.) leak across players.
    /// </summary>
    public static class PlayerScopedServices
    {
        public static void FlushAndClear(DataUtil dataUtil)
        {
            if (dataUtil?.currentPlayer != null && !string.IsNullOrWhiteSpace(dataUtil.currentPlayer.playerID))
            {
                try
                {
                    ChapterQuestService.Save(dataUtil);
                    OnboardingService.Save(dataUtil);
                    GachaService.Save(dataUtil);
                    DeckService.Save(dataUtil);
                    WorldService.Save(dataUtil);
                    ShipService.Save(dataUtil);
                    IdleSettlementService.Save(dataUtil);
                    FeatureUnlockService.Save(dataUtil);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[SAVE] Failed flushing player-scoped services: " + ex.Message);
                }
            }

            Clear();
        }

        public static void Clear()
        {
            ChapterQuestService.Clear();
            OnboardingService.Clear();
            GachaService.Clear();
            WorldService.Clear();
            ShipService.Clear();
            IdleSettlementService.Clear();
            FeatureUnlockService.Clear();
        }
    }
}

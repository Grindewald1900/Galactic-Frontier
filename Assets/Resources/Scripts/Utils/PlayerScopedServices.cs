using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.ChapterQuest;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Gacha;
using Assets.Resources.Scripts.Onboarding;
using Assets.Resources.Scripts.Quest;
using Assets.Resources.Scripts.Unlock;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.Utils.Save;
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
                GameSaveService.PushImmediate();
                try
                {
                    QuestService.Save(dataUtil);
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
                finally
                {
                    GameSaveService.PopImmediate();
                }
            }

            Clear();
        }

        public static void Clear()
        {
            QuestService.Clear();
            ChapterQuestService.Clear();
            OnboardingService.Clear();
            GachaService.Clear();
            DeckService.Clear();
            WorldService.Clear();
            ShipService.Clear();
            IdleSettlementService.Clear();
            FeatureUnlockService.Clear();
            LiveBattleSession.Clear();
            if (BackgroundBattleHost.Instance != null)
                BackgroundBattleHost.Instance.ReleasePresentation();
        }
    }
}

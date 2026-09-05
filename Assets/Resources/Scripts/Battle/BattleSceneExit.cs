using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.UI.Nexus;
using UnityEngine;

namespace Assets.Resources.Scripts.Battle
{
    /// <summary>
    /// Leaves BattleScene and restores MainScene at a requested Nexus screen.
    /// Explore maps to <see cref="AppScreen.Battle"/> in the shell navigation.
    /// </summary>
    public static class BattleSceneExit
    {
        /// <summary>Normal post-report path: MainScene → Explore. Stops MainCombat occupation.</summary>
        public static void ReturnToExplore()
        {
            LeaveBattle(AppScreen.Battle, keepLiveSession: false, preferPendingReturn: true);
        }

        /// <summary>
        /// Return to Bridge while the fight continues via <see cref="BackgroundBattleHost"/> coroutine.
        /// </summary>
        public static void ReturnToBridge()
        {
            bool captured = false;
            if (BattleController.Instance != null)
            {
                // Prefer IsBattle, but also snapshot if a live session is already active.
                if (GameStatusManager.Instance == null
                    || GameStatusManager.Instance.IsBattle
                    || LiveBattleSession.Active)
                {
                    BattleController.Instance.CaptureLiveSession();
                    captured = true;
                }
            }

            bool keep = LiveBattleSession.CanResume;
            var host = BackgroundBattleHost.Ensure();
            if (keep)
                host.DetachPresentationAndContinue();
            else
                host.ReleasePresentation();

            Debug.Log(
                $"[BATTLE] ReturnToBridge captured={captured} keep={keep} " +
                $"round={LiveBattleSession.CurrentRound} active={LiveBattleSession.Active}");

            LeaveBattle(AppScreen.Bridge, keepLiveSession: keep, preferPendingReturn: false);
        }

        /// <summary>Abort the fight (and AutoCombat if spectating) then return to Bridge.</summary>
        public static void Flee()
        {
            BackgroundBattleHost.AbortOccupation();
            LeaveBattle(AppScreen.Bridge, keepLiveSession: false, preferPendingReturn: false);
        }

        private static void LeaveBattle(AppScreen screen, bool keepLiveSession, bool preferPendingReturn)
        {
            if (GameStatusManager.Instance != null)
            {
                GameStatusManager.Instance.IsBattle = false;
                GameStatusManager.Instance.CurrentScene = CurrentScene.MAIN_SCENE;
            }

            if (!keepLiveSession)
            {
                DeckService.StopAllMainCombat();
                if (LiveBattleSession.Active || LiveBattleSession.Finished)
                    LiveBattleSession.Clear();
                BackgroundBattleHost.Ensure().ReleasePresentation();
            }

            var returnScreen = BattleController.PendingReturnScreen;
            BattleController.PendingBattleTargetId = null;
            BattleController.PendingEncounterId = null;
            BattleController.PendingIsChapterPrologue = false;
            BattleController.PendingSpectateAutoCombat = false;
            BattleController.PendingResumeLiveSession = false;
            BattleController.PendingReturnScreen = null;

            AppShell.RequestScreen(
                preferPendingReturn && returnScreen.HasValue ? returnScreen.Value : screen);

            LoadingOverlay.LoadScene(nameof(SceneLoader.SceneName.MainScene));

            Debug.Log(
                keepLiveSession
                    ? "[BATTLE] Leaving BattleScene → Bridge (background coroutine continues)."
                    : $"[BATTLE] Leaving BattleScene → MainScene ({screen}).");
        }
    }
}

using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Main;
using Assets.Resources.Scripts.Scene;
using Assets.Resources.Scripts.UI.Nexus;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Resources.Scripts.Battle
{
    /// <summary>
    /// Leaves BattleScene and restores MainScene at a requested Nexus screen.
    /// Explore maps to <see cref="AppScreen.Battle"/> in the shell navigation.
    /// </summary>
    public static class BattleSceneExit
    {
        /// <summary>Normal post-report path: MainScene → Explore.</summary>
        public static void ReturnToExplore()
        {
            LeaveBattle(AppScreen.Battle);
        }

        /// <summary>Abort / Esc path: MainScene → Bridge.</summary>
        public static void ReturnToBridge()
        {
            LeaveBattle(AppScreen.Bridge);
        }

        private static void LeaveBattle(AppScreen screen)
        {
            if (GameStatusManager.Instance != null)
                GameStatusManager.Instance.IsBattle = false;

            // P1.3: MainCombat occupation ends when leaving BattleScene.
            DeckService.StopAllMainCombat();
            BattleController.PendingBattleTargetId = null;

            AppShell.RequestScreen(screen);

            var sceneName = nameof(SceneLoader.SceneName.MainScene);
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);

            Debug.Log($"[BATTLE] Leaving BattleScene → MainScene ({screen}).");
        }
    }
}

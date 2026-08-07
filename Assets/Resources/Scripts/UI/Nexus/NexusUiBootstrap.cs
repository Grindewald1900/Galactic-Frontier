using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Installs the Figma-derived UI theme without requiring scene GUID changes.
    /// </summary>
    internal static class NexusUiBootstrap
    {
        private static bool registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (!registered)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                registered = true;
            }

            Install(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            Install(scene);
        }

        private static void Install(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene.name != "MainMenuScene" &&
                scene.name != "MainScene" &&
                scene.name != "BattleScene")
            {
                return;
            }

            if (Object.FindFirstObjectByType<NexusShell>() != null)
                return;

            var shellObject = new GameObject("Nexus UI Shell");
            shellObject.AddComponent<NexusShell>();
        }
    }
}

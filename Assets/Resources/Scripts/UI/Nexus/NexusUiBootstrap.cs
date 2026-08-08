using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Installs the rewritten AppShell on hub scenes and BattleChrome on BattleScene.
    /// Does not theme or mutate legacy Prefab hierarchies globally.
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
            switch (scene.name)
            {
                case "MainMenuScene":
                case "MainScene":
                    if (Object.FindFirstObjectByType<AppShell>() == null)
                    {
                        var shell = new GameObject("App Shell");
                        shell.AddComponent<AppShell>();
                    }
                    break;
                case "BattleScene":
                    if (Object.FindFirstObjectByType<BattleChrome>() == null)
                    {
                        var chrome = new GameObject("Battle Chrome");
                        chrome.AddComponent<BattleChrome>();
                    }
                    break;
            }
        }
    }
}

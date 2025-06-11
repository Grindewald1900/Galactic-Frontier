using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Resources.Scripts.Scene
{
    public class SceneLoader : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance of SceneLoader.
        /// </summary>
        public static SceneLoader Instance { get; private set; }

        [Header("Optional: Transition animation during scene load")]
        [SerializeField] private Animator transitionAnimator;
        [SerializeField] private float transitionTime = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Loads the specified scene directly.
        /// </summary>
        /// <param name="sceneName">Scene to load.</param>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new System.ArgumentException("Scene name cannot be null or empty.", nameof(sceneName));

            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Loads the specified scene with a transition animation.
        /// </summary>
        /// <param name="sceneName">Scene to load.</param>
        public void LoadSceneWithTransition(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                throw new System.ArgumentException("Scene name cannot be null or empty.", nameof(sceneName));

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            if (transitionAnimator != null)
            {
                transitionAnimator.SetTrigger("Start");
                yield return new WaitForSeconds(transitionTime);
            }
            SceneManager.LoadScene(sceneName);
        }

        public enum SceneName
        {
            // Main menu e.g. "new game", "load game"
            MainMenuScene,
            // Gameplay scene where the actual game happens
            MainScene,
            // Battle scene for combat between two players.
            BattleScene,
        }
    }
}
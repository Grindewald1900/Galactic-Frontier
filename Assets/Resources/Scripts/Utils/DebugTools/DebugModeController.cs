using System;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils.DebugTools
{
    /// <summary>
    /// Process-wide Debug Mode gate. When enabled, AppShell shows the Debug nav entry.
    /// Independent from <see cref="Save.DevDataSettings"/> (sample FakeData) and gift-code rewards.
    /// </summary>
    public sealed class DebugModeController : MonoBehaviour
    {
        public const string EditorPrefsKey = "GalacticFrontier.DebugMode.enabled";
        public const string CommandLineFlag = "-debugMode";

        public static DebugModeController Instance { get; private set; }

        private bool? sessionOverride;
        private bool commandLineParsed;
        private bool commandLineEnabled;

        /// <summary>Raised after <see cref="IsEnabled"/> changes.</summary>
        public event Action<bool> Changed;

        public bool IsEnabled
        {
            get
            {
                if (sessionOverride.HasValue)
                    return sessionOverride.Value;

                EnsureCommandLineParsed();
                if (commandLineEnabled)
                    return true;

#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(EditorPrefsKey, false);
#else
                // Player builds: only session / command line; default off.
                return false;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;

            var go = new GameObject("Debug Mode Controller");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugModeController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCommandLineParsed();
            Debug.Log($"[DEBUG] DebugModeController ready. Enabled={IsEnabled}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Session toggle (not persisted). Use from Settings UI.</summary>
        public void SetEnabled(bool enabled)
        {
            if (sessionOverride == enabled && IsEnabled == enabled)
            {
                // Still force event when turning on from prefs-equivalent state.
            }

            sessionOverride = enabled;
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(EditorPrefsKey, enabled);
#endif
            Debug.Log($"[DEBUG] Debug Mode enabled={enabled}");
            Changed?.Invoke(IsEnabled);
        }

        public void Toggle()
        {
            SetEnabled(!IsEnabled);
        }

        private void EnsureCommandLineParsed()
        {
            if (commandLineParsed)
                return;

            commandLineParsed = true;
            try
            {
                var args = Environment.GetCommandLineArgs();
                foreach (string arg in args)
                {
                    if (string.Equals(arg, CommandLineFlag, StringComparison.OrdinalIgnoreCase))
                    {
                        commandLineEnabled = true;
                        Debug.Log("[DEBUG] Enabled via command line " + CommandLineFlag);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DEBUG] Command line parse failed: " + ex.Message);
            }
        }
    }
}

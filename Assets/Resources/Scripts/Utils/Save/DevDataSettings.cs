using System;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>
    /// Gates prototype FakeData / sample injection. Independent from
    /// <see cref="Props.DefaultProperty.isDebug"/> (which only controls JSON Base64 encoding).
    /// </summary>
    /// <remarks>
    /// Production Mode is the default for Editor Play Mode, Development builds, and Release.
    /// Enable Dev Data Mode via EditorPrefs, <c>-devData</c> command line, or a session override
    /// (e.g. gift code). See Documentation/zh-CN/systems/08-save-and-seed-data.md §4.1.
    /// </remarks>
    public static class DevDataSettings
    {
        public const string EditorPrefsKey = "GalacticFrontier.DevData.enabled";
        public const string CommandLineFlag = "-devData";

        private static bool? sessionOverride;
        private static bool commandLineParsed;
        private static bool commandLineEnabled;

        /// <summary>True when sample / FakeData providers may run.</summary>
        public static bool Enabled
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
                return false;
#endif
            }
        }

        /// <summary>Overrides Enabled for the current process only (not persisted).</summary>
        public static void SetSessionEnabled(bool enabled)
        {
            sessionOverride = enabled;
            Debug.Log($"[DEV-DATA] Session override enabled={enabled}");
        }

        /// <summary>Clears the process override so EditorPrefs / command line apply again.</summary>
        public static void ClearSessionOverride()
        {
            sessionOverride = null;
            Debug.Log("[DEV-DATA] Session override cleared");
        }

        /// <summary>Persists the Editor preference and applies it as the session value.</summary>
        public static void SetEditorEnabled(bool enabled)
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(EditorPrefsKey, enabled);
#endif
            sessionOverride = enabled;
            Debug.Log($"[DEV-DATA] Editor preference enabled={enabled}");
        }

        private static void EnsureCommandLineParsed()
        {
            if (commandLineParsed)
                return;

            commandLineParsed = true;
            try
            {
                var args = Environment.GetCommandLineArgs();
                for (var i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], CommandLineFlag, StringComparison.OrdinalIgnoreCase))
                    {
                        commandLineEnabled = true;
                        Debug.Log("[DEV-DATA] Enabled via command line flag " + CommandLineFlag);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DEV-DATA] Failed to parse command line: " + ex.Message);
            }
        }
    }
}

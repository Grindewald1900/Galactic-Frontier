using System.Collections.Generic;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>
    /// Process-wide access to the active <see cref="IDevDataProvider"/>.
    /// </summary>
    public static class DevData
    {
        private static IDevDataProvider current = new DefaultDevDataProvider();
        private static readonly HashSet<string> skippedSources = new();

        public static IDevDataProvider Current
        {
            get => current;
            set => current = value ?? new DefaultDevDataProvider();
        }

        public static bool IsActive => DevDataSettings.Enabled && Current != null && Current.IsActive;

        /// <summary>Logs once per source when production code skips FakeData because Dev Data Mode is off.</summary>
        public static void LogSkipped(string source)
        {
            if (IsActive || string.IsNullOrEmpty(source) || !skippedSources.Add(source))
                return;
            Debug.Log($"[DEV-DATA] Skipped FakeData in {source} (Dev Data Mode off).");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Assets.Resources.Scripts.Unlock.Domain
{
    /// <summary>Loads <c>Resources/Data/FeatureUnlockCatalog.json</c> with an in-code fallback.</summary>
    public static class FeatureUnlockCatalog
    {
        public const string ResourcePath = "Data/FeatureUnlockCatalog";

        private static List<FeatureUnlockDef> cached;

        public static IReadOnlyList<FeatureUnlockDef> All
        {
            get
            {
                EnsureLoaded();
                return cached;
            }
        }

        public static void ClearCache() => cached = null;

        public static FeatureUnlockDef Get(string featureId)
        {
            if (string.IsNullOrEmpty(featureId)) return null;
            foreach (var def in All)
            {
                if (def != null && def.featureId == featureId)
                    return def;
            }

            return null;
        }

        public static FeatureUnlockDef ForScreen(string navScreen)
        {
            if (string.IsNullOrEmpty(navScreen)) return null;
            foreach (var def in All)
            {
                if (def != null && def.navScreen == navScreen)
                    return def;
            }

            return null;
        }

        private static void EnsureLoaded()
        {
            if (cached != null) return;
            cached = new List<FeatureUnlockDef>();

            var asset = global::UnityEngine.Resources.Load<global::UnityEngine.TextAsset>(ResourcePath);
            if (asset != null && !string.IsNullOrEmpty(asset.text))
            {
                var file = global::UnityEngine.JsonUtility.FromJson<FeatureUnlockCatalogFile>(asset.text);
                if (file?.features != null)
                {
                    foreach (var row in file.features)
                    {
                        if (row != null && !string.IsNullOrEmpty(row.featureId))
                            cached.Add(row);
                    }
                }
            }

            if (cached.Count == 0)
                Debug.LogWarning("[UNLOCK] FeatureUnlockCatalog.json missing or empty.");
        }
    }
}

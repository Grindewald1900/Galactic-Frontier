using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Ensures LiberationSans (or the TMP default) can render Simplified Chinese via a
    /// dynamic Noto Sans SC fallback. No prebaked CJK atlas is checked into the repo.
    /// </summary>
    public static class CjkFontBootstrap
    {
        private const string NotoResourcesPath = "Fonts/NotoSansSC-Regular";
        private static bool applied;
        private static TMP_FontAsset cjkFallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoApply()
        {
            EnsureFallback();
        }

        public static void EnsureFallback()
        {
            if (applied)
                return;

            TMP_FontAsset primary = TMP_Settings.defaultFontAsset;
            if (primary == null)
                return;

            TMP_FontAsset fallback = GetOrCreateCjkFallback();
            if (fallback == null)
                return;

            if (primary.fallbackFontAssetTable == null)
                primary.fallbackFontAssetTable = new List<TMP_FontAsset>();

            if (!primary.fallbackFontAssetTable.Contains(fallback))
                primary.fallbackFontAssetTable.Add(fallback);

            if (TMP_Settings.fallbackFontAssets == null)
                TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();

            if (!TMP_Settings.fallbackFontAssets.Contains(fallback))
                TMP_Settings.fallbackFontAssets.Add(fallback);

            applied = true;
        }

        public static TMP_FontAsset GetRuntimeFont()
        {
            EnsureFallback();
            return TMP_Settings.defaultFontAsset;
        }

        private static TMP_FontAsset GetOrCreateCjkFallback()
        {
            if (cjkFallback != null)
                return cjkFallback;

            Font source = UnityEngine.Resources.Load<Font>(NotoResourcesPath);
            if (source == null)
            {
                // Editor / machines without the bundled OTF: try common OS CJK faces.
                source = Font.CreateDynamicFontFromOSFont(
                    new[]
                    {
                        "Noto Sans SC",
                        "Microsoft YaHei",
                        "Microsoft YaHei UI",
                        "PingFang SC",
                        "Source Han Sans SC",
                        "SimHei"
                    },
                    36);
            }

            if (source == null)
            {
                Debug.LogWarning("[LOC] No CJK font available; Simplified Chinese glyphs may be missing.");
                return null;
            }

            cjkFallback = TMP_FontAsset.CreateFontAsset(
                source,
                36,
                4,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic);

            if (cjkFallback != null)
                cjkFallback.name = "NotoSansSC Dynamic Fallback";

            return cjkFallback;
        }
    }
}

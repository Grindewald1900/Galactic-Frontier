using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Visual tokens aligned with the Figma Make command theme (index.css).
    /// </summary>
    public static class NexusTheme
    {
        public const float NavCollapsedWidth = 64f;
        public const float NavExpandedWidth = 220f;
        public const float TopBarHeight = 80f;
        public const float BreadcrumbHeight = 32f;
        public const float StatusBarHeight = 22f;
        /// <summary>Top marquee is parked; keep 0 so chrome/content offsets stay aligned.</summary>
        public const float NotificationBarHeight = 0f;
        public const bool NotificationBarEnabled = false;
        public static float ChromeHeaderHeight => TopBarHeight + NotificationBarHeight + BreadcrumbHeight;

        public static readonly Color Background = Hex("#07091A");
        public static readonly Color Surface = Hex("#0D1228");
        public static readonly Color SurfaceRaised = Hex("#111830");
        public static readonly Color SurfaceHover = Hex("#182040");
        public static readonly Color Border = Hex("#28324D");
        public static readonly Color BorderSoft = new Color(148f / 255f, 163f / 255f, 184f / 255f, 0.1f);
        public static readonly Color Gold = Hex("#E8A832");
        public static readonly Color Cyan = Hex("#38BDF8");
        public static readonly Color Green = Hex("#34D399");
        public static readonly Color Purple = Hex("#A78BFA");
        public static readonly Color Red = Hex("#F87171");
        public static readonly Color Text = Hex("#E2E8F0");
        public static readonly Color MutedText = Hex("#64748B");
        public static readonly Color DimText = Hex("#94A3B8");

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.white;
        }
    }
}

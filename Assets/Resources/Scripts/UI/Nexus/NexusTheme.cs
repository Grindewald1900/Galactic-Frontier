using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>
    /// Visual tokens adapted from the Figma "PC Card-Based Idle RPG" design.
    /// </summary>
    public static class NexusTheme
    {
        public const float NavigationWidth = 72f;
        public const float TopBarHeight = 56f;
        public const float BreadcrumbHeight = 34f;
        public const float StatusBarHeight = 24f;

        public static readonly Color Background = Hex("#050819");
        public static readonly Color Surface = Hex("#0B1024");
        public static readonly Color SurfaceRaised = Hex("#11172B");
        public static readonly Color SurfaceHover = Hex("#171F37");
        public static readonly Color Border = Hex("#28324D");
        public static readonly Color BorderSoft = Hex("#1A2239");
        public static readonly Color Gold = Hex("#F0B429");
        public static readonly Color Cyan = Hex("#38BDF8");
        public static readonly Color Green = Hex("#34D399");
        public static readonly Color Purple = Hex("#9B7BFF");
        public static readonly Color Red = Hex("#F87171");
        public static readonly Color Text = Hex("#E8ECF7");
        public static readonly Color MutedText = Hex("#8993AC");
        public static readonly Color DimText = Hex("#535E78");

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

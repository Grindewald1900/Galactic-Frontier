namespace Assets.Resources.Scripts.World.Domain
{
    /// <summary>MVP faction tags (P5.2). Labels only — no full faction gameplay.</summary>
    public static class FactionTags
    {
        public const string FrontierGuard = "FactionA";
        public const string RiftSyndicate = "FactionB";

        public static string DisplayEn(string tag) =>
            tag == RiftSyndicate ? "Rift Syndicate" :
            tag == FrontierGuard ? "Frontier Guard" : "";

        public static string DisplayZh(string tag) =>
            tag == RiftSyndicate ? "裂隙商盟" :
            tag == FrontierGuard ? "边境卫队" : "";

        public static string ShortEn(string tag) =>
            tag == RiftSyndicate ? "Syndicate" :
            tag == FrontierGuard ? "Guard" : "";

        public static string ShortZh(string tag) =>
            tag == RiftSyndicate ? "商盟" :
            tag == FrontierGuard ? "卫队" : "";
    }
}

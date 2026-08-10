using System;

namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>Per-player save directory metadata (<c>meta.json</c>).</summary>
    [Serializable]
    public class SaveMeta
    {
        public int saveVersion;
        public long createdAtUtc;
        public long lastSavedAtUtc;
        public int starterSeedTableVersion;
        public string appVersion;
    }
}

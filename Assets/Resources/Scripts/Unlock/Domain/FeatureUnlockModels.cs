using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.Unlock.Domain
{
    /// <summary>One row in the feature-unlock catalog.</summary>
    [Serializable]
    public class FeatureUnlockDef
    {
        public string featureId = "";
        public string navScreen = "";
        public bool startsUnlocked;
        public string requiredStepId = "";
        public int requiredShipLevel;
        public string requiredRegionId = "";
        public string titleEn = "";
        public string titleZh = "";
        public string hintEn = "";
        public string hintZh = "";
        public string bodyEn = "";
        public string bodyZh = "";
        public string notifyEn = "";
        public string notifyZh = "";
    }

    [Serializable]
    public class FeatureUnlockCatalogFile
    {
        public List<FeatureUnlockDef> features = new List<FeatureUnlockDef>();
    }

    /// <summary>Persisted unlock flags. Seeded on first load so existing saves do not spam dialogs.</summary>
    [Serializable]
    public class FeatureUnlockState
    {
        public List<string> unlockedFeatureIds = new List<string>();
        public List<string> announcedFeatureIds = new List<string>();
        public bool seeded;
        public int count;
    }

    /// <summary>Inputs the catalog evaluates against. No Unity types so rules stay testable.</summary>
    public sealed class FeatureUnlockContext
    {
        public bool ChainCompleted;
        public List<string> ClaimedStepIds = new List<string>();
        public int ShipLevel = 1;
        public List<string> ClearedRegionIds = new List<string>();
    }
}

using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;

namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>Deterministic starter inventory rows from <c>Resources/data/StarterSeed</c>.</summary>
    [Serializable]
    public class StarterSeedTable
    {
        public int seedTableVersion = 1;
        public List<ItemEntity> localItems = new();
    }
}

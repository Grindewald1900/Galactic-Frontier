using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;

namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>
    /// Supplies prototype sample data when <see cref="DevDataSettings.Enabled"/> is true.
    /// Implementations must not call <c>DataUtil.Save*</c>; callers decide persistence.
    /// </summary>
    public interface IDevDataProvider
    {
        bool IsActive { get; }

        IReadOnlyList<ItemEntity> CreateSampleInventory(bool isRemote);

        IReadOnlyList<CardEntity> CreateSampleEnemyParty(int count);

        /// <summary>
        /// Fills gacha material lists used by the draw UI. Clears and replaces the target lists.
        /// </summary>
        void FillSampleGachaMaterials(
            List<ItemEntity> providers,
            List<ItemEntity> consumers,
            List<int> quantitiesPerDraw);

        IReadOnlyList<CardEntity> CreateSampleGachaResults(int count);

        PlanetEntity CreateSamplePlanet(int index);

        IReadOnlyList<EventEntity> CreateSampleEvents(int count);
    }
}

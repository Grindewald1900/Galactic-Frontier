using System;

namespace Assets.Resources.Scripts.Economy.Domain
{
    /// <summary>Runtime state of an automated production line (economy/15 §4.10).</summary>
    public enum ProductionLineState
    {
        /// <summary>Not built, disabled, or no valid recipe selected.</summary>
        Idle = 0,
        /// <summary>Built + enabled + recipe valid; producing on cycle.</summary>
        Running = 1,
        /// <summary>Blocked by missing materials, full warehouse, or an unavailable industry fleet.</summary>
        PausedBlock = 2
    }

    /// <summary>
    /// Static definition of an automated production line family (economy/15 §4.10.7 LineBlueprintDef).
    /// A line only serves recipes that share its <see cref="lineFamilyId"/>; the active recipe is
    /// further gated by <see cref="ProductionLineInstance.tier"/> vs <c>RecipeDef.requiredLineTier</c>.
    /// </summary>
    [Serializable]
    public class ProductionLineDef
    {
        public string lineId = "";
        public string lineFamilyId = "";
        public string displayNameEn = "";
        public string displayNameZh = "";
        public string descriptionEn = "";
        public string descriptionZh = "";
        public string icon = "Building";
        public int tierCap = 3;
        /// <summary>Recipes this line may run (must also satisfy tier + skill gates).</summary>
        public string[] allowedRecipeIds = Array.Empty<string>();
        /// <summary>Materials consumed once to build the line.</summary>
        public RecipeInput[] buildCost = Array.Empty<RecipeInput>();
    }

    /// <summary>
    /// Player-owned, persisted line instance (economy/15 §4.10.7 ProductionLineInstance).
    /// Stored inside <see cref="PlayerIdleState.productionLines"/>.
    /// </summary>
    [Serializable]
    public class ProductionLineInstance
    {
        public string lineId = "";
        public int tier = 1;
        public bool built;
        public bool enabled;
        public string activeRecipeId = "";
        /// <summary>Whole-cycle progress anchor; advances by produced cycles only.</summary>
        public long lastSettledAtUtc;
        public ProductionLineState state = ProductionLineState.Idle;
    }
}

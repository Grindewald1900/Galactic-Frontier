using Assets.Resources.Scripts.Progression.Domain;

namespace Assets.Resources.Scripts.Utils
{
    public static class LevelUtil
    {
        public static float GetNextLevelExp(int currentLevel) =>
            ProgressionRules.CombatExpToNext(currentLevel);
    }
}
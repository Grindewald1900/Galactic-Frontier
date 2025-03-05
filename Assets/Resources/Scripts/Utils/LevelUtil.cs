namespace Assets.Scripts.Utils
{
    public static class LevelUtil
    {
        public static float GetNextLevelExp(int currentLevel)
        {
            if (currentLevel < 0) return 0f;
            if (currentLevel <= 20)
            {
                return currentLevel * 100f;
            }
            else if (currentLevel >= 21 && currentLevel <= 40)
            {
                return GetNextLevelExp(20) + (200 * (currentLevel - 20));
            }
            else
            {
                return currentLevel * 1000f;
            }
        }
    }
}
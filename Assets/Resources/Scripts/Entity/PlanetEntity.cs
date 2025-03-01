namespace Assets.Resources.Scripts.Entity
{
    [System.Serializable]
    public class PlanetEntity
    {
        public string backgroundSprite;
        public string planetLevel;
        public string planetName;
        public string planetDescription;

        public PlanetEntity(string bgSprite, string level, string name, string info)
        {
            backgroundSprite = bgSprite;
            planetLevel = level;
            planetName = name;
            planetDescription = info;
        }
    }
}
[System.Serializable]
public class PlanetEntity
{
    public string backgroundSprite;
    public string planetLevel;
    public string planetName;
    public string planetDescription;

    public PlanetEntity(string bgSprite, string level, string name, string info)
    {
        this.backgroundSprite = bgSprite;
        this.planetLevel = level;
        this.planetName = name;
        this.planetDescription = info;
    }
}

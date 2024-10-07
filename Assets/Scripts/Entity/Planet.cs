using UnityEngine;

[System.Serializable]
public class Planet
{
    public Sprite backgroundSprite;
    public string planetLevel;
    public string planetName;

    public string planetDescription;

    public Planet(Sprite bgSprite, string level, string name, string info)
    {
        this.backgroundSprite = bgSprite;
        this.planetLevel = level;
        this.planetName = name;
        this.planetDescription = info;
    }
}

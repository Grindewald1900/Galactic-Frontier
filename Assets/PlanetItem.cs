using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlanetItem : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetPlanet(Planet planet)
    {
        if (planet == null) return;
        SetPlanetItemText(planet.planetLevel, planet.planetName, planet.planetDescription);
        SetPlanetItemImage(planet.backgroundSprite);
    }

    public void SetPlanetItemText(string level, string name, string description)
    {
        TextUtil.SetText(levelText, level);
        TextUtil.SetText(nameText, name);
        TextUtil.SetText(descriptionText, description);
    }

    public void SetPlanetItemImage(Sprite sprite)
    {
        if (sprite == null) return;
        image.sprite = sprite;
    }
}

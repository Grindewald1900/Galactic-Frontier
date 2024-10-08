using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlanetItem : MonoBehaviour, IPointerClickHandler
{
    public Image image;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public bool isFocused = false;
    private Planet currentPlanet;
    private Vector3 defaultScale = Vector3.one;
    private Vector3 focusedScale = new Vector3(1.05f, 1.05f, 1f);


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
        currentPlanet = planet;
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

    public void SetFocus(bool focus)
    {
        isFocused = focus;
        transform.localScale = isFocused ? focusedScale : defaultScale;
        if (focus)
        {
            MapManager.Instance.SetPlanetInfo(currentPlanet);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isFocused == false)
        {
            MapListManager.Instance.SetCurrentFocus(this);
        }
    }
}

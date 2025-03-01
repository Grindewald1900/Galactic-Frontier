using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Planet;

namespace Assets.Resources.Scripts.UI
{
    public class PlanetSlot : MonoBehaviour, IPointerClickHandler
    {
        public Image image;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public bool isFocused = false;
        public PlanetEntity currentPlanet;
        private Vector3 defaultScale = Vector3.one;
        private Vector3 focusedScale = new(1.05f, 1.05f, 1f);

        public void SetPlanet(PlanetEntity planet)
        {
            if (planet == null) return;
            currentPlanet = planet;
            SetPlanetText(planet.planetLevel, planet.planetName, planet.planetDescription);
            SetPlanetImage(planet.backgroundSprite);
        }

        public void SetPlanetText(string level, string name, string description)
        {
            TextUtil.SetText(levelText, level);
            TextUtil.SetText(nameText, name);
            TextUtil.SetText(descriptionText, description);
        }

        public void SetPlanetImage(string imageName)
        {
            Sprite sprite = ImageUtil.GetSpriteByName(ImageUtil.planetImagePath, imageName);
            if (sprite == null) return;
            image.sprite = sprite;
        }

        public void SetFocus(bool focus)
        {
            isFocused = focus;
            transform.localScale = isFocused ? focusedScale : defaultScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isFocused)
            {
                PlanetListManager.Instance.SetCurrentFocus(this);
            }
        }
    }
}